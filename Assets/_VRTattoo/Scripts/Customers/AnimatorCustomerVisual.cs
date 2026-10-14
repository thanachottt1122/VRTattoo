using UnityEngine;

// For a real character model: drives an Animator instead of moving a capsule.
// The model's root should line up with the pose pivots described in CustomerVisual
// (feet when standing, hips on the seat/bed when sitting or lying); per-character corrections
// live on CustomerCharacter.
//
// While lying on the bed the forearm on the artist's side becomes the tattoo surface
// (CustomerTattooSkin on the same object); getting up removes it together with its ink.
public class AnimatorCustomerVisual : CustomerVisual
{
    public Animator animator;

    [Tooltip("Set by CustomerCharacterPicker, or assign by hand for a single fixed model")]
    public CustomerCharacter character;

    [Tooltip("Float parameter, walking speed in m/s")]
    public string speedParameter = "Speed";

    [Tooltip("Bool parameter, true while sitting")]
    public string sitParameter = "Sit";

    [Tooltip("Bool parameter, true while lying down")]
    public string lieParameter = "Lie";

    CustomerTattooSkin tattooSkin;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        character = GetComponentInChildren<CustomerCharacter>();
    }

    void Awake()
    {
        if (character != null && animator == null)
            animator = character.animator;
        tattooSkin = GetComponent<CustomerTattooSkin>();
    }

    public void SetCharacter(CustomerCharacter newCharacter)
    {
        character = newCharacter;
        animator = newCharacter != null ? newCharacter.animator : null;
    }

    public override void SetPose(CustomerPose pose)
    {
        if (character != null)
            character.ApplyPose(pose);

        if (tattooSkin != null)
        {
            if (pose == CustomerPose.Lie)
                tattooSkin.Build(animator);
            else
                tattooSkin.Release();
        }

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
