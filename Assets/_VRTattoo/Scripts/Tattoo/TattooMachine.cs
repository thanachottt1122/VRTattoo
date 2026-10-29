using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Draws on a TattooCanvas while held, trigger pressed, and the needle tip touches the skin.
// With a TattooInk it draws in the ink's colour, uses ink by area inked and stops (with a warning buzz) when empty.
// With a TattooNeedle the line is as wide as the fitted needle (otherwise the canvas' brushDiameter).
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

    [Tooltip("Ink supply (leave empty to use the one on this object; none = unlimited ink in the canvas colour)")]
    public TattooInk ink;

    [Tooltip("Fitted needle (leave empty to use the one on this object; none = the canvas' brush width)")]
    public TattooNeedle needle;

    [Header("Haptics")]
    [Range(0f, 1f)] public float hapticAmplitude = 0.4f;
    public float hapticDuration = 0.1f;
    public float hapticInterval = 0.08f;

    [Tooltip("Short buzz when the trigger is pressed (or the ink runs out) with no ink")]
    [Range(0f, 1f)] public float emptyHapticAmplitude = 0.8f;
    public float emptyHapticDuration = 0.05f;

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
    bool emptyWarned;

    public bool IsDrawing { get; private set; }

    // Lets non-XR input (Desktop Test Mode) press the trigger.
    public bool ExternalTrigger { get; set; }

    // Trigger held by the hand or by ExternalTrigger (TattooInk refills while this is held in a cap).
    public bool TriggerPressed => triggerHeld || ExternalTrigger;

    // Haptics on the hand pressing the trigger (nothing without one, e.g. Desktop Test Mode).
    public bool SendHaptic(float amplitude, float duration) =>
        activeInteractor != null && activeInteractor.SendHapticImpulse(amplitude, duration);

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (ink == null)
            ink = GetComponent<TattooInk>();
        if (needle == null)
            needle = GetComponent<TattooNeedle>();
    }

    // The trigger is pressed for a refill or a needle swap here, not to draw.
    public bool AtTrolley => (ink != null && ink.DippedCap != null) || (needle != null && needle.TouchedCartridge != null);

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
        emptyWarned = false;
        if (ink != null && ink.IsEmpty && !AtTrolley)
            WarnEmpty();
    }

    void OnDeactivated(DeactivateEventArgs args) => StopDrawing();

    void OnReleased(SelectExitEventArgs args) => StopDrawing();

    void StopDrawing()
    {
        triggerHeld = false;
        activeInteractor = null;
        lastCanvas = null;
        IsDrawing = false;
        emptyWarned = false;
    }

    // One short buzz per trigger press: no ink comes out.
    void WarnEmpty()
    {
        if (emptyWarned)
            return;
        emptyWarned = true;
        SendHaptic(emptyHapticAmplitude, emptyHapticDuration);
        if (debugLog)
            Debug.Log("[TattooMachine] Out of ink");
    }

    void Update()
    {
        // No tattooing while the game is paused.
        if ((!triggerHeld && !ExternalTrigger) || tip == null || GamePause.IsPaused)
        {
            lastCanvas = null;
            IsDrawing = false;
            if (!triggerHeld)
                emptyWarned = false;
            return;
        }

        if (ink != null && ink.IsEmpty)
        {
            lastCanvas = null;
            IsDrawing = false;
            // In a cap (or on a needle cartridge) the trigger refills / swaps instead, so no empty buzz there.
            if (!AtTrolley)
                WarnEmpty();
            return;
        }

        Vector3 direction = tip.forward;
        Vector3 origin = tip.position - direction * rayBackOffset;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, rayBackOffset + contactDistance, skinLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.TryGetComponent(out TattooCanvas canvas))
        {
            Vector2 uv = hit.textureCoord;
            bool continueStroke = canvas == lastCanvas && Time.time - lastContactTime <= strokeGapTolerance;
            Color color = ink != null ? ink.color : canvas.inkColor;
            float diameter = needle != null ? needle.diameter : canvas.brushDiameter;
            if (continueStroke)
                canvas.DrawStroke(lastUV, lastPoint, uv, hit.point, color, diameter);
            else
                canvas.DrawStroke(uv, hit.point, uv, hit.point, color, diameter);

            // Ink goes by the area inked: holding still uses none, a new dot costs one needle width.
            if (ink != null)
                ink.Consume(continueStroke ? Vector3.Distance(lastPoint, hit.point) : diameter, diameter);

            lastCanvas = canvas;
            lastUV = uv;
            lastPoint = hit.point;
            lastContactTime = Time.time;
            IsDrawing = true;

            if (Time.time >= nextHapticTime)
            {
                bool sent = SendHaptic(hapticAmplitude, hapticDuration);
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
