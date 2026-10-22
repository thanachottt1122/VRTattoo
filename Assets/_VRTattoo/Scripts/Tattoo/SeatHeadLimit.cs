using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

// Keeps the player's head near the seat while seated.
// Headset: the head moves freely within freeRadius, beyond it the view fades to black (the camera is never moved).
// XR Device Simulator: the simulated head position is locked (keyboard/scroll moves do nothing), looking around still works.
[DefaultExecutionOrder(XRInteractionUpdateOrder.k_DeviceSimulator + 1)] // right after the simulator pushes its HMD state
public class SeatHeadLimit : MonoBehaviour
{
    public TattooSeat seat;

    [Tooltip("Leave empty to use the XR Origin's camera")]
    public Camera headCamera;

    [Header("Headset")]
    [Tooltip("The head can move this far from where it was when sitting down without any fade (m)")]
    public float freeRadius = 0.5f;

    [Tooltip("Distance past freeRadius over which the view goes fully black (m)")]
    public float fadeDistance = 0.15f;

    [Tooltip("Fade change per second (1 = from clear to black in one second)")]
    public float fadeSpeed = 3f;

    [Tooltip("Black quad in front of the camera (ScreenFade material, _Fade property)")]
    public Renderer fadeRenderer;

    [Header("XR Device Simulator")]
    [Tooltip("Lock the simulated head position while seated (only when no real headset is running)")]
    public bool lockSimulatorHead = true;

    [Tooltip("Leave empty to find it in the scene")]
    public XRDeviceSimulator deviceSimulator;

    // The simulator keeps the head pose in a private field, so it is set by reflection.
    static readonly FieldInfo SimulatorHMDStateField =
        typeof(XRDeviceSimulator).GetField("m_HMDState", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly int FadeId = Shader.PropertyToID("_Fade");

    bool wasSeated;
    Vector3 seatedEyePosition;
    Vector3? lockedSimulatorPosition;
    float fade;
    MaterialPropertyBlock block;

    public float Fade => fade;

    Camera HeadCamera => headCamera != null ? headCamera
        : seat != null && seat.xrOrigin != null ? seat.xrOrigin.Camera : Camera.main;

    void OnDisable()
    {
        fade = 0f;
        ApplyFade();
        lockedSimulatorPosition = null;
        wasSeated = false;
    }

    void Update()
    {
        var cam = HeadCamera;
        bool seated = seat != null && seat.IsSeated && cam != null;

        if (seated && !wasSeated)
        {
            seatedEyePosition = cam.transform.position;
            lockedSimulatorPosition = null;
        }
        wasSeated = seated;

        if (seated && lockSimulatorHead && !XRSettings.isDeviceActive)
            LockSimulatorHead();
        else
            lockedSimulatorPosition = null;

        float target = 0f;
        if (seated)
        {
            float distance = Vector3.Distance(cam.transform.position, seatedEyePosition);
            target = fadeDistance > 0f
                ? Mathf.InverseLerp(freeRadius, freeRadius + fadeDistance, distance)
                : distance > freeRadius ? 1f : 0f;
        }
        fade = Mathf.MoveTowards(fade, target, fadeSpeed * Time.unscaledDeltaTime);
        ApplyFade();
    }

    void ApplyFade()
    {
        if (fadeRenderer == null)
            return;

        fadeRenderer.enabled = fade > 0.001f;
        block ??= new MaterialPropertyBlock();
        fadeRenderer.GetPropertyBlock(block);
        block.SetFloat(FadeId, fade);
        fadeRenderer.SetPropertyBlock(block);
    }

    // Puts the simulated head back where it was when sitting down, after the simulator applied this frame's input.
    void LockSimulatorHead()
    {
        if (deviceSimulator == null)
            deviceSimulator = FindFirstObjectByType<XRDeviceSimulator>();
        if (deviceSimulator == null || !deviceSimulator.isActiveAndEnabled || SimulatorHMDStateField == null)
            return;

        var hmd = InputSystem.GetDevice<XRSimulatedHMD>();
        if (hmd == null || !hmd.added)
            return;

        var state = (XRSimulatedHMDState)SimulatorHMDStateField.GetValue(deviceSimulator);
        if (lockedSimulatorPosition == null)
        {
            lockedSimulatorPosition = state.centerEyePosition;
            return;
        }

        if (state.centerEyePosition == lockedSimulatorPosition.Value)
            return;

        state.centerEyePosition = lockedSimulatorPosition.Value;
        state.devicePosition = state.centerEyePosition;
        SimulatorHMDStateField.SetValue(deviceSimulator, state);
        InputState.Change(hmd, state);
    }
}
