using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Desktop-only testing aid: while seated with the machine in hand, the mouse places the needle
// on the skin and holding the left mouse button draws. R refills the ink, 1-4 switch to the ink caps' colours.
// Does nothing in headset builds.
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

    [Header("Simulator camera")]
    [Tooltip("When the mode becomes active, tilt the XR Device Simulator head to look at the skin. " +
             "Only the simulated head is moved; a real headset is never rotated.")]
    public bool autoLookAtSkin = true;

    [Tooltip("Leave empty to find it in the scene")]
    public XRDeviceSimulator deviceSimulator;

    [Header("Ink (while the mode is on)")]
    public Key refillKey = Key.R;

    [Tooltip("Number keys 1, 2, 3... switch to these caps' colours (and refill)")]
    public InkCap[] inkCaps;

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
    float? pitchBeforeLook;

    // Zoom state: the camera's own FOV is the zoomed-out limit and is put back when the mode ends.
    Camera zoomCamera;
    float? fovBeforeZoom;
    InputAction simulatorScrollAction;
    readonly List<InputAction> pausedSimulatorKeys = new List<InputAction>();
    static readonly Key[] InkKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };

    bool IsAllowed => !editorOnly || Application.isEditor;

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
        PauseSimulatorKeys(false);
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

        PauseSimulatorKeys(enabledMode);
        if (enabledMode && keyboard != null)
            HandleInkKeys(keyboard);

        bool heldInHand = grabInteractable != null && grabInteractable.isSelected
            && !(grabInteractable.firstInteractorSelecting is XRSocketInteractor);
        var skin = SkinCollider;
        bool active = enabledMode && seat != null && seat.IsSeated && heldInHand && skin != null;

        SetOverride(active);
        if (!active)
            return;

        var cam = viewCamera != null ? viewCamera : Camera.main;
        var mouse = Mouse.current;
        if (cam == null || mouse == null)
        {
            machine.ExternalTrigger = false;
            return;
        }

        UpdateZoom(cam, mouse);

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (skin.Raycast(ray, out RaycastHit hit, 10f))
        {
            Vector3 needleDirection = Vector3.Slerp(-hit.normal, ray.direction, tiltTowardView).normalized;
            PlaceTip(hit.point + hit.normal * hoverOffset, needleDirection);
            machine.ExternalTrigger = mouse.leftButton.isPressed;
        }
        else
        {
            machine.ExternalTrigger = false;
        }
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

    void HandleInkKeys(Keyboard keyboard)
    {
        if (keyboard[refillKey].wasPressedThisFrame)
            RefillInk();

        int count = inkCaps != null ? Mathf.Min(inkCaps.Length, InkKeys.Length) : 0;
        for (int i = 0; i < count; i++)
        {
            if (keyboard[InkKeys[i]].wasPressedThisFrame)
                SelectInk(i);
        }
    }

    TattooInk Ink => machine != null ? machine.ink : null;

    // Fills the machine with its current colour.
    public void RefillInk()
    {
        if (Ink == null)
            return;
        Ink.Refill();
        Debug.Log("[TattooDesktopTestMode] Ink refilled");
    }

    // Same as dipping into inkCaps[index].
    public void SelectInk(int index)
    {
        if (Ink == null || inkCaps == null || index < 0 || index >= inkCaps.Length || inkCaps[index] == null)
            return;
        Ink.Refill(inkCaps[index].Color);
        Debug.Log($"[TattooDesktopTestMode] Ink: {inkCaps[index].inkName}");
    }

    // The simulator also uses R and 1-4, so those actions are paused while the mode is on.
    void PauseSimulatorKeys(bool pause)
    {
        if (pause == pausedSimulatorKeys.Count > 0)
            return;

        if (!pause)
        {
            foreach (var action in pausedSimulatorKeys)
                action.Enable();
            pausedSimulatorKeys.Clear();
            return;
        }

        if (deviceSimulator == null)
            deviceSimulator = FindFirstObjectByType<XRDeviceSimulator>();
        if (deviceSimulator == null)
            return;

        foreach (var reference in new[]
        {
            deviceSimulator.toggleMouseTransformationModeAction,  // R
            deviceSimulator.togglePrimary2DAxisTargetAction,      // 1
            deviceSimulator.toggleSecondary2DAxisTargetAction,    // 2
            deviceSimulator.toggleDevicePositionTargetAction,     // 3
            deviceSimulator.primary2DAxisClickAction,             // 4
        })
        {
            var action = reference != null ? reference.action : null;
            if (action != null && action.enabled)
            {
                action.Disable();
                pausedSimulatorKeys.Add(action);
            }
        }
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
