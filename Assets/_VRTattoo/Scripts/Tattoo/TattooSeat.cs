using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Tattoo artist chair: toggles the XR Origin between seated (facing the look target, locomotion off) and standing.
public class TattooSeat : MonoBehaviour
{
    public XROrigin xrOrigin;

    [Tooltip("What the player faces when seated (the arm)")]
    public Transform lookTarget;

    [Tooltip("Eye height above the chair's floor position while seated (m). Not used when leanOverTarget is on")]
    public float seatedEyeHeight = 1.15f;

    [Header("Working position")]
    [Tooltip("Put the eyes close to the look target, like a tattoo artist leaning over the arm, " +
             "instead of straight above the chair. The view is never zoomed.")]
    public bool leanOverTarget = true;

    [Tooltip("Horizontal distance from the look target towards the chair (m)")]
    public float workHorizontalDistance = 0.25f;

    [Tooltip("Height of the eyes above the look target's centre (m)")]
    public float workEyeHeight = 0.33f;

    [Tooltip("How far from the chair, away from the look target, the player stands up (m)")]
    public float standDistance = 0.6f;

    [Tooltip("Controller button only works when the head is this close to the chair (m)")]
    public float useRange = 1.5f;

    public InputActionProperty toggleAction;

    [Tooltip("Locomotion providers etc. turned off while seated")]
    public Behaviour[] disableWhileSeated;

    [Tooltip("Turned off while seated so it does not collide with the chair or fall")]
    public CharacterController characterController;

    [Header("Tool")]
    [Tooltip("Put into the hand automatically while seated (the tattoo machine)")]
    public XRGrabInteractable heldTool;

    [Tooltip("Interactor that holds the tool while seated (right hand Near-Far Interactor)")]
    public XRBaseInteractor handInteractor;

    XRSimpleInteractable chairInteractable;
    readonly HashSet<Behaviour> wasEnabled = new HashSet<Behaviour>();

    public bool IsSeated { get; private set; }

    void Awake()
    {
        chairInteractable = GetComponent<XRSimpleInteractable>();
    }

    void OnEnable()
    {
        var action = toggleAction.action;
        if (action != null)
        {
            action.performed += OnTogglePerformed;
            if (toggleAction.reference == null)
                action.Enable();
        }

        if (chairInteractable != null)
            chairInteractable.selectEntered.AddListener(OnChairSelected);
    }

    void OnDisable()
    {
        var action = toggleAction.action;
        if (action != null)
        {
            action.performed -= OnTogglePerformed;
            if (toggleAction.reference == null)
                action.Disable();
        }

        if (chairInteractable != null)
            chairInteractable.selectEntered.RemoveListener(OnChairSelected);
    }

    void OnTogglePerformed(InputAction.CallbackContext context)
    {
        if (IsSeated || IsPlayerInRange())
            Toggle();
    }

    void OnChairSelected(SelectEnterEventArgs args) => Toggle();

    bool IsPlayerInRange()
    {
        if (xrOrigin == null || xrOrigin.Camera == null)
            return false;

        Vector3 offset = xrOrigin.Camera.transform.position - transform.position;
        offset.y = 0f;
        return offset.magnitude <= useRange;
    }

    public void Toggle()
    {
        if (IsSeated)
            StandUp();
        else
            SitDown();
    }

    public void SitDown()
    {
        if (IsSeated || xrOrigin == null)
            return;

        SetBehavioursEnabled(false);

        Vector3 eyePosition = SeatedEyePosition();
        if (lookTarget != null)
        {
            Vector3 forward = lookTarget.position - eyePosition;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
                xrOrigin.MatchOriginUpCameraForward(Vector3.up, forward.normalized);
        }
        xrOrigin.MoveCameraToWorldLocation(eyePosition);

        if (heldTool != null && handInteractor != null && !handInteractor.isPerformingManualInteraction)
            handInteractor.StartManualInteraction((IXRSelectInteractable)heldTool);

        IsSeated = true;
    }

    public Vector3 SeatedEyePosition()
    {
        if (!leanOverTarget || lookTarget == null)
            return transform.position + Vector3.up * seatedEyeHeight;

        Vector3 towardsChair = transform.position - lookTarget.position;
        towardsChair.y = 0f;
        towardsChair = towardsChair.sqrMagnitude > 0.0001f ? towardsChair.normalized : -transform.forward;
        return lookTarget.position + towardsChair * workHorizontalDistance + Vector3.up * workEyeHeight;
    }

    public void StandUp()
    {
        if (!IsSeated || xrOrigin == null)
            return;

        // Releasing hands the tool to ReturnToSocket, which puts it back on its stand.
        if (handInteractor != null && handInteractor.isPerformingManualInteraction)
            handInteractor.EndManualInteraction();

        Vector3 away = lookTarget != null ? transform.position - lookTarget.position : -transform.forward;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : -transform.forward;

        Vector3 standPosition = transform.position + away * standDistance;
        Transform origin = xrOrigin.Origin.transform;
        Vector3 cameraOffset = xrOrigin.Camera.transform.position - origin.position;
        cameraOffset.y = 0f;
        origin.position = new Vector3(standPosition.x - cameraOffset.x, transform.position.y, standPosition.z - cameraOffset.z);

        SetBehavioursEnabled(true);

        IsSeated = false;
    }

    void SetBehavioursEnabled(bool enabled)
    {
        if (!enabled)
            wasEnabled.Clear();

        foreach (var behaviour in disableWhileSeated)
        {
            if (behaviour == null)
                continue;

            if (!enabled)
            {
                if (behaviour.enabled)
                    wasEnabled.Add(behaviour);
                behaviour.enabled = false;
            }
            else if (wasEnabled.Contains(behaviour))
            {
                behaviour.enabled = true;
            }
        }

        if (characterController != null)
            characterController.enabled = enabled;
    }
}
