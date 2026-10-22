using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Sends this grabbable back into its socket when it is let go, or if it falls out of the map.
[RequireComponent(typeof(XRGrabInteractable))]
public class ReturnToSocket : MonoBehaviour
{
    public XRSocketInteractor socket;

    [Tooltip("Return to the socket if the object falls below this world height")]
    public float fallResetHeight = -3f;

    XRGrabInteractable grabInteractable;
    bool returnPending;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        grabInteractable.selectExited.RemoveListener(OnSelectExited);
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        // Selecting again inside a select-exit callback is not allowed, so return on the next Update.
        if (socket != null && args.interactorObject != (IXRSelectInteractor)socket)
            returnPending = true;
    }

    void Update()
    {
        if (socket == null)
            return;

        if (grabInteractable.isSelected)
        {
            // Picked up again (e.g. passed to the other hand) before the return happened.
            returnPending = false;
            return;
        }

        if (returnPending || transform.position.y < fallResetHeight)
        {
            returnPending = false;
            Return();
        }
    }

    public void Return()
    {
        if (socket == null || socket.hasSelection)
            return;

        var manager = socket.interactionManager != null ? socket.interactionManager : grabInteractable.interactionManager;
        if (manager == null)
            return;

        manager.SelectEnter((IXRSelectInteractor)socket, (IXRSelectInteractable)grabInteractable);
    }
}
