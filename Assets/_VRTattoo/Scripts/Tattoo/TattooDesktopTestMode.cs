using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Desktop-only testing aid. While seated: click the machine on its holder to pick it up, P puts it back.
// With the machine in hand the mouse places the needle on the skin (or in an ink cap) and holding the
// left mouse button presses the trigger: draws on skin, refills / changes colour in a cap, just like in
// the headset. Does nothing in headset builds.
[DefaultExecutionOrder(-10)] // place the needle before TattooMachine raycasts this frame
public class TattooDesktopTestMode : MonoBehaviour
{
    [Tooltip("Can also be toggled with the toggle key in Play mode")]
    public bool enabledMode;

    [Tooltip("Only works inside the Unity Editor, never in builds")]
    public bool editorOnly = true;

    public Key toggleKey = Key.K;

    public TattooSeat seat;
    public TattooMachine machine;
    [Tooltip("Leave empty to use the forearm of the customer on the bed (CustomerTattooSkin)")]
    public Collider skinCollider;

    [Tooltip("Leave empty to use Camera.main")]
    public Camera viewCamera;

    [Tooltip("0 = needle perpendicular to the skin, 1 = needle along the mouse ray")]
    [Range(0f, 1f)] public float tiltTowardView = 0.5f;

    [Tooltip("Gap between the needle tip and the skin (m)")]
    public float hoverOffset = 0.001f;

    [Header("Pick up / put back")]
    [Tooltip("Hand that holds the machine when it is clicked on its holder (right Near-Far Interactor)")]
    public XRBaseInteractor handInteractor;

    [Tooltip("Puts the machine back on its holder. Not G: the XR Device Simulator uses G for grip.")]
    public Key releaseKey = Key.P;

    [Header("Simulator camera")]
    [Tooltip("When the mode becomes active, tilt the XR Device Simulator head to look at the skin. " +
             "Only the simulated head is moved; a real headset is never rotated.")]
    public bool autoLookAtSkin = true;

    [Tooltip("Leave empty to find it in the scene")]
    public XRDeviceSimulator deviceSimulator;

    [Tooltip("Needle direction in an ink cap or on a needle cartridge: 0 = straight down, 1 = along the mouse ray")]
    [Range(0f, 1f)] public float capTiltTowardView = 0.3f;

    [Header("Zoom (desktop only)")]
    [Tooltip("Mouse wheel changes the camera's field of view while the mode is active. " +
             "Never applied when a headset is running: zooming in VR causes motion sickness.")]
    public bool scrollZoom = true;

    [Tooltip("Narrowest field of view when zoomed in (degrees)")]
    public float minFieldOfView = 20f;

    [Tooltip("Degrees per mouse wheel notch")]
    public float zoomStep = 4f;

    // The simulator keeps its head rotation in private fields, so it is set by reflection.
    static readonly FieldInfo SimulatorEulerField =
        typeof(XRDeviceSimulator).GetField("m_CenterEyeEuler", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo SimulatorHMDStateField =
        typeof(XRDeviceSimulator).GetField("m_HMDState", BindingFlags.NonPublic | BindingFlags.Instance);
    const float MaxSimulatorPitch = 80f; // matches the simulator's own clamp

    XRGrabInteractable grabInteractable;
    bool overriding;
    bool waitForButtonUp;  // the click that picked the machine up is not a trigger press
    float? pitchBeforeLook;

    // Zoom state: the camera's own FOV is the zoomed-out limit and is put back when the mode ends.
    Camera zoomCamera;
    float? fovBeforeZoom;
    InputAction simulatorScrollAction;
    readonly RaycastHit[] capHits = new RaycastHit[16];

    bool IsAllowed => !editorOnly || Application.isEditor;

    // Mouse mode is on (SeatHintPanel shows mouse/keyboard hints).
    public bool IsActive => IsAllowed && enabledMode;

    Collider SkinCollider => skinCollider != null ? skinCollider
        : CustomerTattooSkin.Active != null ? CustomerTattooSkin.Active.SkinCollider : null;

    void Awake()
    {
        if (machine != null)
            grabInteractable = machine.GetComponent<XRGrabInteractable>();
    }

    void OnDisable()
    {
        SetOverride(false);
        NeonButton.HoverWithRay(null);
    }

    void Update()
    {
        if (!IsAllowed)
            return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
        {
            enabledMode = !enabledMode;
            Debug.Log($"[TattooDesktopTestMode] {(enabledMode ? "ON" : "OFF")}");
        }

        bool seated = enabledMode && seat != null && seat.IsSeated;
        HoverButtons(seated);
        if (seated && ClickedButton())
            return;
        if (seated)
            HandlePickUp(keyboard);

        // No customer needed: the ink caps and needle cartridges can still be used.
        bool active = seated && HeldInHand;

        SetOverride(active);
        if (!active)
            return;

        // The mode can start before a customer lies down; look at the skin once it appears.
        if (!pitchBeforeLook.HasValue && SkinCollider != null)
            LookAtSkin();

        var cam = viewCamera != null ? viewCamera : Camera.main;
        var mouse = Mouse.current;
        if (cam == null || mouse == null)
        {
            machine.ExternalTrigger = false;
            return;
        }

        UpdateZoom(cam, mouse);

        if (waitForButtonUp && !mouse.leftButton.isPressed)
            waitForButtonUp = false;
        bool triggerPressed = mouse.leftButton.isPressed && !waitForButtonUp;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        var skin = SkinCollider;
        RaycastHit skinHit = default;
        bool onSkin = skin != null && skin.Raycast(ray, out skinHit, 10f);
        var capTrigger = RaycastTrolleyTrigger(ray, out float capDistance);

        if (capTrigger != null && (!onSkin || capDistance < skinHit.distance))
        {
            // Needle into the middle of the cap's / cartridge's trigger, pointing down: the trigger refills or swaps.
            Vector3 needleDirection = Vector3.Slerp(Vector3.down, ray.direction, capTiltTowardView).normalized;
            PlaceTip(capTrigger.bounds.center, needleDirection);
            machine.ExternalTrigger = triggerPressed;
        }
        else if (onSkin)
        {
            Vector3 needleDirection = Vector3.Slerp(-skinHit.normal, ray.direction, tiltTowardView).normalized;
            PlaceTip(skinHit.point + skinHit.normal * hoverOffset, needleDirection);
            machine.ExternalTrigger = triggerPressed;
        }
        else
        {
            machine.ExternalTrigger = false;
        }
    }

    // The mouse points at NeonButtons like a controller ray does (glow + hover sound), only where it can click them.
    void HoverButtons(bool seated)
    {
        var cam = viewCamera != null ? viewCamera : Camera.main;
        var mouse = Mouse.current;
        NeonButton.HoverWithRay(seated && cam != null && mouse != null
            ? cam.ScreenPointToRay(mouse.position.ReadValue())
            : (Ray?)null);
    }

    // A click on a shown NeonButton (the job card's DONE) presses it instead of tattooing.
    bool ClickedButton()
    {
        var cam = viewCamera != null ? viewCamera : Camera.main;
        var mouse = Mouse.current;
        if (cam == null || mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return false;
        if (!NeonButton.TryPressWithRay(cam.ScreenPointToRay(mouse.position.ReadValue())))
            return false;
        waitForButtonUp = true;
        if (machine != null)
            machine.ExternalTrigger = false;
        return true;
    }

    bool HeldInHand => grabInteractable != null && grabInteractable.isSelected
        && !(grabInteractable.firstInteractorSelecting is XRSocketInteractor);

    // Click the machine (on its holder) to take it in hand; the release key puts it back.
    void HandlePickUp(Keyboard keyboard)
    {
        if (grabInteractable == null || handInteractor == null)
            return;

        if (HeldInHand)
        {
            if (keyboard != null && keyboard[releaseKey].wasPressedThisFrame)
                PutBack();
            return;
        }

        var cam = viewCamera != null ? viewCamera : Camera.main;
        var mouse = Mouse.current;
        if (cam == null || mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        foreach (var c in grabInteractable.colliders)
        {
            if (c != null && c.enabled && c.Raycast(ray, out _, 10f))
            {
                handInteractor.StartManualInteraction((IXRSelectInteractable)grabInteractable);
                waitForButtonUp = true;
                return;
            }
        }
    }

    // ReturnToSocket puts the released machine back on its holder.
    void PutBack()
    {
        var holder = grabInteractable.firstInteractorSelecting;
        if (holder is XRBaseInteractor interactor && interactor.isPerformingManualInteraction)
            interactor.EndManualInteraction();
        else if (grabInteractable.interactionManager != null)
            grabInteractable.interactionManager.SelectExit(holder, (IXRSelectInteractable)grabInteractable);
    }

    // Nearest ink cap or needle cartridge trigger under the mouse.
    Collider RaycastTrolleyTrigger(Ray ray, out float distance)
    {
        Collider trigger = null;
        distance = float.MaxValue;
        int count = Physics.RaycastNonAlloc(ray, capHits, 10f, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var hit = capHits[i];
            if (!hit.collider.isTrigger || hit.distance >= distance)
                continue;
            if (hit.collider.GetComponentInParent<InkCap>() == null && hit.collider.GetComponentInParent<NeedleCartridge>() == null)
                continue;
            trigger = hit.collider;
            distance = hit.distance;
        }
        return trigger;
    }

    void PlaceTip(Vector3 tipPosition, Vector3 needleDirection)
    {
        Transform body = machine.transform;
        Transform tip = machine.tip;
        body.rotation = Quaternion.FromToRotation(tip.forward, needleDirection) * body.rotation;
        body.position += tipPosition - tip.position;
    }

    // While active the hand stops driving the machine so the mouse can.
    void SetOverride(bool on)
    {
        if (on == overriding || grabInteractable == null)
            return;

        grabInteractable.trackPosition = !on;
        grabInteractable.trackRotation = !on;
        if (!on && machine != null)
            machine.ExternalTrigger = false;

        if (on)
            LookAtSkin();
        else
        {
            RestoreSimulatorPitch();
            EndZoom();
        }

        overriding = on;
    }

    // Mouse wheel = field of view. The simulator also listens to the wheel (moves the simulated
    // device), so its scroll action is paused while zooming is possible.
    void UpdateZoom(Camera cam, Mouse mouse)
    {
        if (!scrollZoom || XRSettings.isDeviceActive)
            return;

        if (fovBeforeZoom == null)
        {
            zoomCamera = cam;
            fovBeforeZoom = cam.fieldOfView;
            PauseSimulatorScroll(true);
        }

        Zoom(mouse.scroll.ReadValue().y);
    }

    // Positive = wheel forward = zoom in. Only between minFieldOfView and the camera's own FOV.
    public void Zoom(float wheel)
    {
        if (zoomCamera == null || fovBeforeZoom == null || Mathf.Abs(wheel) < 0.01f)
            return;

        float maxFov = Mathf.Max(fovBeforeZoom.Value, minFieldOfView);
        zoomCamera.fieldOfView = Mathf.Clamp(zoomCamera.fieldOfView - Mathf.Sign(wheel) * zoomStep, minFieldOfView, maxFov);
    }

    void EndZoom()
    {
        if (fovBeforeZoom.HasValue && zoomCamera != null)
            zoomCamera.fieldOfView = fovBeforeZoom.Value;
        fovBeforeZoom = null;
        zoomCamera = null;
        PauseSimulatorScroll(false);
    }

    void PauseSimulatorScroll(bool pause)
    {
        if (pause)
        {
            if (deviceSimulator == null)
                deviceSimulator = FindFirstObjectByType<XRDeviceSimulator>();
            var action = deviceSimulator != null && deviceSimulator.mouseScrollAction != null
                ? deviceSimulator.mouseScrollAction.action : null;
            if (action != null && action.enabled)
            {
                action.Disable();
                simulatorScrollAction = action;
            }
        }
        else if (simulatorScrollAction != null)
        {
            simulatorScrollAction.Enable();
            simulatorScrollAction = null;
        }
    }

    void LookAtSkin()
    {
        var skin = SkinCollider;
        if (!autoLookAtSkin || skin == null)
            return;

        var cam = viewCamera != null ? viewCamera : Camera.main;
        if (cam == null)
            return;

        Vector3 toSkin = skin.bounds.center - cam.transform.position;
        float horizontal = new Vector2(toSkin.x, toSkin.z).magnitude;
        float pitch = Mathf.Clamp(Mathf.Atan2(-toSkin.y, horizontal) * Mathf.Rad2Deg, -MaxSimulatorPitch, MaxSimulatorPitch);

        if (TrySetSimulatorPitch(pitch, out float previous))
            pitchBeforeLook = previous;
    }

    void RestoreSimulatorPitch()
    {
        if (pitchBeforeLook.HasValue)
            TrySetSimulatorPitch(pitchBeforeLook.Value, out _);
        pitchBeforeLook = null;
    }

    // Only ever touches the XR Device Simulator's simulated head; without a simulator this does nothing.
    bool TrySetSimulatorPitch(float pitch, out float previousPitch)
    {
        previousPitch = 0f;
        if (deviceSimulator == null)
            deviceSimulator = FindFirstObjectByType<XRDeviceSimulator>();
        if (deviceSimulator == null || !deviceSimulator.isActiveAndEnabled
            || SimulatorEulerField == null || SimulatorHMDStateField == null)
            return false;

        var euler = (Vector3)SimulatorEulerField.GetValue(deviceSimulator);
        previousPitch = euler.x;
        euler.x = pitch;
        SimulatorEulerField.SetValue(deviceSimulator, euler);

        var hmdState = (XRSimulatedHMDState)SimulatorHMDStateField.GetValue(deviceSimulator);
        hmdState.centerEyeRotation = Quaternion.Euler(euler);
        hmdState.deviceRotation = hmdState.centerEyeRotation;
        SimulatorHMDStateField.SetValue(deviceSimulator, hmdState);
        return true;
    }
}
