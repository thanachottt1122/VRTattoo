using UnityEngine;

// For a real character model: drives an Animator instead of moving a capsule.
// The model's root should line up with the pose pivots described in CustomerVisual
// (feet when standing, hips on the seat/bed when sitting or lying).
public class AnimatorCustomerVisual : CustomerVisual
{
    public Animator animator;

    [Tooltip("Float parameter, walking speed in m/s")]
    public string speedParameter = "Speed";

    [Tooltip("Bool parameter, true while sitting")]
    public string sitParameter = "Sit";

    [Tooltip("Bool parameter, true while lying down")]
    public string lieParameter = "Lie";

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public override void SetPose(CustomerPose pose)
    {
        if (animator == null)
            return;

        SetBool(sitParameter, pose == CustomerPose.Sit);
        SetBool(lieParameter, pose == CustomerPose.Lie);
        if (pose != CustomerPose.Walk)
            SetMoveSpeed(0f);
    }

    public override void SetMoveSpeed(float speed)
    {
        if (animator != null && !string.IsNullOrEmpty(speedParameter))
            animator.SetFloat(speedParameter, speed);
    }

    void SetBool(string parameter, bool value)
    {
        if (!string.IsNullOrEmpty(parameter))
            animator.SetBool(parameter, value);
    }
}
