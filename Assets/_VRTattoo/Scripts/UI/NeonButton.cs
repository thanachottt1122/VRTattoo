using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// World-space button in the NeonPanel style (the job card's DONE). Pressed by pointing at it and pulling
// the trigger, by grabbing it (grip), or by poking it. Glows brighter while pointed at, gives a short
// haptic pulse and a small bounce when pressed.
[RequireComponent(typeof(XRSimpleInteractable))]
public class NeonButton : MonoBehaviour
{
    public NeonPanel panel;
    public TMP_Text label;
    public UnityEvent onPressed;

    [Header("Feel")]
    public float hoverScale = 1.06f;
    public float hoverSpeed = 10f;
    [Range(0f, 1f)] public float hapticAmplitude = 0.6f;
    public float hapticDuration = 0.08f;
    [Tooltip("Ignores presses this soon after the last one (s)")]
    public float cooldown = 0.5f;
    public float pressBounce = 0.12f;
    public float bounceSeconds = 0.25f;

    static readonly List<NeonButton> Shown = new List<NeonButton>();

    XRSimpleInteractable interactable;
    readonly List<IXRHoverInteractor> hovering = new List<IXRHoverInteractor>();
    Vector3 baseScale;
    float hoverAmount;
    float lastPressTime = -10f;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        Shown.Add(this);
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDisable()
    {
        Shown.Remove(this);
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        hovering.Clear();
        hoverAmount = 0f;
        transform.localScale = baseScale;
    }

    void OnHoverEntered(HoverEnterEventArgs args) => hovering.Add(args.interactorObject);
    void OnHoverExited(HoverExitEventArgs args) => hovering.Remove(args.interactorObject);
    void OnSelectEntered(SelectEnterEventArgs args) => Press(args.interactorObject);

    void Update()
    {
        // XRI only sends "activated" to selected objects, so the trigger is read from the hovering hand.
        for (int i = hovering.Count - 1; i >= 0; i--)
        {
            if (hovering[i] is XRBaseInputInteractor input && input.activateInput.ReadWasPerformedThisFrame())
            {
                Press(input);
                break;
            }
        }

        hoverAmount = Mathf.MoveTowards(hoverAmount, hovering.Count > 0 ? 1f : 0f, hoverSpeed * Time.deltaTime);
        if (panel != null)
            panel.hover = hoverAmount;

        float t = (Time.time - lastPressTime) / Mathf.Max(bounceSeconds, 0.01f);
        float bounce = t < 1f ? -pressBounce * Mathf.Sin(t * Mathf.PI) : 0f;
        transform.localScale = baseScale * (1f + (hoverScale - 1f) * hoverAmount + bounce);
    }

    void Press(IXRInteractor interactor)
    {
        if (Time.time - lastPressTime < cooldown)
            return;
        lastPressTime = Time.time;

        if (interactor is XRBaseInputInteractor input)
            input.SendHapticImpulse(hapticAmplitude, hapticDuration);
        onPressed.Invoke();
    }

    // Desktop Test Mode: a mouse click on a shown button presses it.
    public static bool TryPressWithRay(Ray ray, float maxDistance = 10f)
    {
        foreach (var button in Shown)
        {
            foreach (var c in button.interactable.colliders)
            {
                if (c != null && c.enabled && c.Raycast(ray, out _, maxDistance))
                {
                    button.Press(null);
                    return true;
                }
            }
        }
        return false;
    }
}
