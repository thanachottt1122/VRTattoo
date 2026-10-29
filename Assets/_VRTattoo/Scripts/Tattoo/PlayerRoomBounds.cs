using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

// Keeps the standing player inside the room (walls, door, big furniture).
// Locomotion already goes through the XR Origin's CharacterController, which collides with the room colliders.
// XR Device Simulator: keyboard moves shift the simulated head, not the origin, so the CharacterController never
//   sees them. Each frame the head's horizontal move is taken back and replayed with CharacterController.Move,
//   so the simulated player stops (and slides) at walls like with locomotion.
// Headset: real head movement is never pushed back (that makes people sick). When the head goes into a blocker,
//   or a blocker is between it and where it last was, the view fades to black instead.
// Does nothing while seated (TattooSeat turns the CharacterController off, SeatHeadLimit takes over).
// Runs in LateUpdate, after SeatHeadLimit's Update, so the shared fade quad gets the larger of the two fades.
public class PlayerRoomBounds : MonoBehaviour
{
    public XROrigin xrOrigin;
    public TattooSeat seat;

    [Tooltip("Only colliders under these count as blockers for the headset fade (walls, door, furniture)")]
    public Transform[] blockerRoots;

    [Header("XR Device Simulator")]
    [Tooltip("Route simulated head moves through the CharacterController (only when no real headset is running)")]
    public bool blockSimulatorHead = true;

    [Tooltip("Simulated head moves longer than this in one frame are resets and are left alone (m)")]
    public float maxStep = 0.5f;

    [Header("Headset")]
    [Tooltip("The view starts fading when the head is this close to a blocker (m)")]
    public float headRadius = 0.1f;

    [Tooltip("Fade change per second (1 = from clear to black in one second)")]
    public float fadeSpeed = 3f;

    [Tooltip("Black quad in front of the camera (ScreenFade material, _Fade property). Shared with SeatHeadLimit")]
    public Renderer fadeRenderer;

    [Tooltip("Its fade is combined with this one on the shared quad")]
    public SeatHeadLimit seatHeadLimit;

    public bool debugLog;

    static readonly int FadeId = Shader.PropertyToID("_Fade");
    static readonly Collider[] Overlaps = new Collider[16];

    CharacterController controller;
    Vector3? lastTrackedHead;
    Vector3? lastSafeHead; // origin space
    float fade;
    float lastLogTime = float.NegativeInfinity;
    MaterialPropertyBlock block;

    public float Fade => fade;

    void Awake()
    {
        if (xrOrigin != null)
            controller = xrOrigin.GetComponent<CharacterController>();
    }

    void OnDisable()
    {
        fade = 0f;
        ApplyFade();
        lastTrackedHead = null;
        lastSafeHead = null;
    }

    void LateUpdate()
    {
        if (xrOrigin == null || xrOrigin.Camera == null)
            return;

        bool seated = seat != null && seat.IsSeated;
        bool active = !seated && controller != null && controller.enabled;

        if (active && blockSimulatorHead && !XRSettings.isDeviceActive)
            BlockSimulatorHead();
        else
            lastTrackedHead = null;

        float target = active && XRSettings.isDeviceActive ? HeadsetFadeTarget() : 0f;
        if (!active)
            lastSafeHead = null;
        fade = Mathf.MoveTowards(fade, target, fadeSpeed * Time.unscaledDeltaTime);
        ApplyFade();
    }

    // The head position in tracking space does not change when the origin moves, so its change is exactly
    // what the simulator did this frame.
    void BlockSimulatorHead()
    {
        Vector3 tracked = xrOrigin.CameraInOriginSpacePos;
        Vector3? previous = lastTrackedHead;
        lastTrackedHead = tracked;
        if (previous == null)
            return;

        Vector3 localDelta = tracked - previous.Value;
        localDelta.y = 0f;
        if (localDelta.sqrMagnitude < 1e-8f || localDelta.magnitude > maxStep)
            return;

        // Undo the head move by shifting the origin back, then redo it with collisions.
        Transform origin = xrOrigin.Origin.transform;
        Vector3 worldDelta = origin.TransformVector(localDelta);
        origin.position -= worldDelta;
        FitControllerUnderHead();
        Vector3 before = origin.position;
        var flags = controller.Move(worldDelta);

        if (debugLog && (flags & CollisionFlags.Sides) != 0 && Time.time - lastLogTime > 0.5f)
        {
            lastLogTime = Time.time;
            Vector3 moved = origin.position - before;
            moved.y = 0f;
            Debug.Log($"[PlayerRoomBounds] simulator head blocked: wanted {worldDelta.magnitude * 100f:0.0} cm, " +
                      $"moved {moved.magnitude * 100f:0.0} cm, head at {xrOrigin.Camera.transform.position}");
        }
    }

    // Same capsule as XRI's CharacterControllerBodyManipulator: from the floor up to the head, under the head.
    void FitControllerUnderHead()
    {
        Vector3 head = xrOrigin.CameraInOriginSpacePos;
        float height = Mathf.Max(xrOrigin.CameraInOriginSpaceHeight, controller.radius * 2f);
        controller.height = height;
        controller.center = new Vector3(head.x, height * 0.5f + controller.skinWidth, head.z);
    }

    float HeadsetFadeTarget()
    {
        Vector3 head = xrOrigin.Camera.transform.position;
        Transform origin = xrOrigin.Origin.transform;

        // Kept in origin space so teleports and turns (which move the origin, never through walls) carry it along;
        // only the player's own walking can take the head through a wall.
        lastSafeHead ??= origin.InverseTransformPoint(head);
        Vector3 safe = origin.TransformPoint(lastSafeHead.Value);

        // Walked through: something solid between the head and the last place it was clear.
        if (IsBlocked(safe, head))
            return 1f;

        float nearest = NearestBlockerDistance(head);
        if (nearest >= headRadius)
            lastSafeHead = origin.InverseTransformPoint(head);
        return headRadius > 0f ? 1f - Mathf.Clamp01(nearest / headRadius) : nearest > 0f ? 0f : 1f;
    }

    // Distance from the head to the closest blocker surface within headRadius (0 = inside one).
    float NearestBlockerDistance(Vector3 head)
    {
        int count = Physics.OverlapSphereNonAlloc(head, headRadius, Overlaps, ~0, QueryTriggerInteraction.Ignore);
        float nearest = headRadius;
        for (int i = 0; i < count; i++)
        {
            var c = Overlaps[i];
            if (!IsBlocker(c))
                continue;
            // ClosestPoint does not support concave meshes (door frame, bed): count them as half way in.
            float distance = c is MeshCollider mesh && !mesh.convex
                ? headRadius * 0.5f
                : Vector3.Distance(head, c.ClosestPoint(head));
            nearest = Mathf.Min(nearest, distance);
        }
        return nearest;
    }

    bool IsBlocked(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        float length = d.magnitude;
        if (length < 0.001f)
            return false;
        foreach (var hit in Physics.RaycastAll(from, d / length, length, ~0, QueryTriggerInteraction.Ignore))
            if (IsBlocker(hit.collider))
                return true;
        // Started inside a blocker (e.g. head already in a wall when the fade began).
        foreach (var hit in Physics.RaycastAll(to, -d / length, length, ~0, QueryTriggerInteraction.Ignore))
            if (IsBlocker(hit.collider))
                return true;
        return false;
    }

    bool IsBlocker(Collider c)
    {
        if (c == null || c.attachedRigidbody != null || blockerRoots == null)
            return false;
        foreach (var root in blockerRoots)
            if (root != null && c.transform.IsChildOf(root))
                return true;
        return false;
    }

    void ApplyFade()
    {
        if (fadeRenderer == null)
            return;

        float shown = Mathf.Max(fade, seatHeadLimit != null && seatHeadLimit.isActiveAndEnabled ? seatHeadLimit.Fade : 0f);
        fadeRenderer.enabled = shown > 0.001f;
        block ??= new MaterialPropertyBlock();
        fadeRenderer.GetPropertyBlock(block);
        block.SetFloat(FadeId, shown);
        fadeRenderer.SetPropertyBlock(block);
    }
}
