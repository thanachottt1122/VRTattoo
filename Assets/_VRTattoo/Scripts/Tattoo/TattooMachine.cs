using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Draws on a TattooCanvas while held, trigger pressed, and the needle tip touches the skin.
[RequireComponent(typeof(XRGrabInteractable))]
public class TattooMachine : MonoBehaviour
{
    [Tooltip("Needle tip. Its forward (+Z) axis points out of the needle.")]
    public Transform tip;

    [Tooltip("How far past the tip the skin still counts as touched (m)")]
    public float contactDistance = 0.01f;

    [Tooltip("Ray starts this far behind the tip so pressing into the skin still hits (m)")]
    public float rayBackOffset = 0.03f;

    public LayerMask skinLayers = ~0;

    [Header("Haptics")]
    [Range(0f, 1f)] public float hapticAmplitude = 0.4f;
    public float hapticDuration = 0.1f;
    public float hapticInterval = 0.08f;

    [Tooltip("Losing contact for less than this long keeps the stroke connected (s)")]
    public float strokeGapTolerance = 0.1f;

    public bool debugLog;

    XRGrabInteractable grabInteractable;
    XRBaseInputInteractor activeInteractor;
    bool triggerHeld;
    TattooCanvas lastCanvas;
    Vector2 lastUV;
    Vector3 lastPoint;
    float lastContactTime;
    float nextHapticTime;

    public bool IsDrawing { get; private set; }

    // Lets non-XR input (Desktop Test Mode) press the trigger.
    public bool ExternalTrigger { get; set; }

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        grabInteractable.activated.AddListener(OnActivated);
        grabInteractable.deactivated.AddListener(OnDeactivated);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        grabInteractable.activated.RemoveListener(OnActivated);
        grabInteractable.deactivated.RemoveListener(OnDeactivated);
        grabInteractable.selectExited.RemoveListener(OnReleased);
        StopDrawing();
    }

    void OnActivated(ActivateEventArgs args)
    {
        triggerHeld = true;
        activeInteractor = args.interactorObject as XRBaseInputInteractor;
    }

    void OnDeactivated(DeactivateEventArgs args) => StopDrawing();

    void OnReleased(SelectExitEventArgs args) => StopDrawing();

    void StopDrawing()
    {
        triggerHeld = false;
        activeInteractor = null;
        lastCanvas = null;
        IsDrawing = false;
    }

    void Update()
    {
        if ((!triggerHeld && !ExternalTrigger) || tip == null)
        {
            lastCanvas = null;
            IsDrawing = false;
            return;
        }

        Vector3 direction = tip.forward;
        Vector3 origin = tip.position - direction * rayBackOffset;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, rayBackOffset + contactDistance, skinLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.TryGetComponent(out TattooCanvas canvas))
        {
            Vector2 uv = hit.textureCoord;
            bool continueStroke = canvas == lastCanvas && Time.time - lastContactTime <= strokeGapTolerance;
            if (continueStroke)
                canvas.DrawStroke(lastUV, lastPoint, uv, hit.point);
            else
                canvas.DrawStroke(uv, hit.point, uv, hit.point);

            lastCanvas = canvas;
            lastUV = uv;
            lastPoint = hit.point;
            lastContactTime = Time.time;
            IsDrawing = true;

            if (Time.time >= nextHapticTime)
            {
                bool sent = activeInteractor != null && activeInteractor.SendHapticImpulse(hapticAmplitude, hapticDuration);
                nextHapticTime = Time.time + hapticInterval;
                if (debugLog)
                    Debug.Log($"[TattooMachine] Drawing at UV {uv}, haptic sent: {sent}");
            }
        }
        else
        {
            // Keep lastCanvas so a brief loss of contact (fast moves) still joins up within strokeGapTolerance.
            IsDrawing = false;
        }
    }
}
