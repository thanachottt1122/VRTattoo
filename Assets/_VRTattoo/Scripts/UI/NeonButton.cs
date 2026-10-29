using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// World-space button in the NeonPanel style (the job card's DONE). Pressed by pointing at it and pulling
// the trigger, by grabbing it (grip), or by poking it. Glows brighter while pointed at, gives a short
// haptic pulse and a small bounce when pressed. Starting to point at a pressable button raises AnyHoverStarted
// once (not again while the pointer stays on it), from any hand or from the mouse in Desktop Test Mode.
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

    // Any button was pressed (ShopSounds plays the click).
    public static event Action<NeonButton> AnyPressed;
    // Something started pointing at a shown, pressable button (ShopSounds plays the hover tick).
    public static event Action<NeonButton> AnyHoverStarted;

    static readonly List<NeonButton> Shown = new List<NeonButton>();

    XRSimpleInteractable interactable;
    readonly List<IXRHoverInteractor> hovering = new List<IXRHoverInteractor>();
    bool mouseHover;
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
        mouseHover = false;
        hoverAmount = 0f;
        transform.localScale = baseScale;
    }

    bool IsHovered => hovering.Count > 0 || mouseHover;

    // Hidden, switched off or still cooling down from the last press: pointing at it does nothing.
    bool CanPress => isActiveAndEnabled && interactable.enabled && Time.time - lastPressTime >= cooldown;

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        bool wasHovered = IsHovered;
        hovering.Add(args.interactorObject);
        if (!wasHovered)
            OnHoverStarted();
    }

    void OnHoverStarted()
    {
        if (CanPress)
            AnyHoverStarted?.Invoke(this);
    }

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

        hoverAmount = Mathf.MoveTowards(hoverAmount, IsHovered ? 1f : 0f, hoverSpeed * Time.deltaTime);
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
        AnyPressed?.Invoke(this);
        onPressed.Invoke();
    }

    // Desktop Test Mode: the mouse ray hovers the button under it (null = mouse not pointing at buttons).
    public static void HoverWithRay(Ray? ray, float maxDistance = 10f)
    {
        for (int i = Shown.Count - 1; i >= 0; i--)
        {
            var button = Shown[i];
            bool hit = ray.HasValue && button.HitBy(ray.Value, maxDistance);
            if (hit == button.mouseHover)
                continue;
            bool wasHovered = button.IsHovered;
            button.mouseHover = hit;
            if (hit && !wasHovered)
                button.OnHoverStarted();
        }
    }

    bool HitBy(Ray ray, float maxDistance)
    {
        foreach (var c in interactable.colliders)
        {
            if (c != null && c.enabled && c.Raycast(ray, out _, maxDistance))
                return true;
        }
        return false;
    }

    // Desktop Test Mode: a mouse click on a shown button presses it.
    public static bool TryPressWithRay(Ray ray, float maxDistance = 10f)
    {
        foreach (var button in Shown)
        {
            if (button.HitBy(ray, maxDistance))
            {
                button.Press(null);
                return true;
            }
        }
        return false;
    }
}
