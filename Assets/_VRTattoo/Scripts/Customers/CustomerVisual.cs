using UnityEngine;

public enum CustomerPose { Idle, Walk, Sit, Lie }

// The look of a customer. Customer only talks to this class, so the placeholder capsule can be
// swapped for an animated character by putting a different CustomerVisual on the prefab.
//
// Pose pivots (the customer's transform when SetPose is called):
//   Idle / Walk: feet on the floor, facing forward
//   Sit:         on the seat surface (under the hips), facing away from the backrest
//   Lie:         on the bed surface (under the hips), forward = towards the head
public abstract class CustomerVisual : MonoBehaviour
{
    public abstract void SetPose(CustomerPose pose);

    // Walking speed in m/s (0 when standing still). Useful for blending a walk animation.
    public virtual void SetMoveSpeed(float speed) { }
}
