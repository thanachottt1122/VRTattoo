using UnityEngine;

public enum CustomerGender { Male, Female }

// Root of one character model (same object as its Animator). Holds the per-character tuning that
// lines the model up with the pose pivots of CustomerVisual. While lying on the bed it also
// straightens the pose with IK (the clip spreads arms and legs): both arms lie along the body and
// the feet come together. The forearm on the artist's side is tattooed (CustomerTattooSkin).
public class CustomerCharacter : MonoBehaviour
{
    public Animator animator;

    [Header("Voice (CustomerVoice)")]
    [Tooltip("Picks the male or female voice set of CustomerVoice")]
    public CustomerGender gender = CustomerGender.Male;

    [Tooltip("Own voice set for this character, used instead of the gender's set")]
    public CustomerVoiceSet voiceOverride;

    [Header("Sitting (relative to the seat pivot)")]
    public Vector3 sitOffset = new Vector3(0f, -0.41f, 0f);
    public Vector3 sitRotation;

    [Header("Lying (relative to the bed pivot, forward = towards the head)")]
    [Tooltip("x moves the body sideways onto the bed's centre line, z moves the head onto the pillow")]
    public Vector3 lieOffset;
    [Tooltip("The lying clip has the head towards -Z, so the model is turned around")]
    public Vector3 lieRotation = new Vector3(0f, 180f, 0f);

    [Header("Straight pose while lying (the clip spreads arms and legs)")]
    [Tooltip("Lay the arms along the body instead of spread out")]
    public bool armsAlongBody = true;

    [Tooltip("How far outside the shoulder the free hand rests (m)")]
    public float handOutward = 0.05f;

    [Tooltip("Height of the resting arm above the bed surface (m)")]
    public float armRestHeight = 0.04f;

    [Tooltip("Put the feet down on the bed, this far from the body's centre line")]
    public bool feetOnBed = true;
    public float footSpread = 0.11f;

    [Tooltip("Height of the ankle above the bed surface (m)")]
    public float ankleHeight = 0.08f;

    bool lying;

    void Reset()
    {
        animator = GetComponent<Animator>();
    }

    public void ApplyPose(CustomerPose pose)
    {
        lying = pose == CustomerPose.Lie;
        switch (pose)
        {
            case CustomerPose.Sit:
                transform.SetLocalPositionAndRotation(sitOffset, Quaternion.Euler(sitRotation));
                break;
            case CustomerPose.Lie:
                transform.SetLocalPositionAndRotation(lieOffset, Quaternion.Euler(lieRotation));
                break;
            default:
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                break;
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null)
            return;

        foreach (var goal in new[] { AvatarIKGoal.LeftHand, AvatarIKGoal.RightHand, AvatarIKGoal.LeftFoot, AvatarIKGoal.RightFoot })
            animator.SetIKPositionWeight(goal, 0f);
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);

        // The parent (the customer) is the bed pivot while lying: on the mattress surface,
        // forward towards the head, +X = the customer's left.
        var bed = transform.parent;
        if (!lying || bed == null)
            return;

        if (feetOnBed)
        {
            PlaceFoot(bed, AvatarIKGoal.LeftFoot, 1f);
            PlaceFoot(bed, AvatarIKGoal.RightFoot, -1f);
        }

        if (armsAlongBody)
        {
            LayAlongBody(bed, true);
            LayAlongBody(bed, false);
        }
    }

    void PlaceFoot(Transform bed, AvatarIKGoal foot, float side)
    {
        var position = bed.InverseTransformPoint(animator.GetIKPosition(foot));
        position.x = transform.localPosition.x + side * footSpread;
        position.y = ankleHeight;
        animator.SetIKPositionWeight(foot, 1f);
        animator.SetIKPosition(foot, bed.TransformPoint(position));
    }

    // Straight arm resting on the bed beside the body, hand towards the feet.
    void LayAlongBody(Transform bed, bool left)
    {
        var upper = animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
        var lower = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
        var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        float length = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position);

        Vector3 shoulder = bed.InverseTransformPoint(upper.position);
        float side = left ? 1f : -1f;
        float drop = shoulder.y - armRestHeight;
        float reach = Mathf.Sqrt(Mathf.Max(0f, length * length - drop * drop)) * 0.97f;

        var handTarget = new Vector3(shoulder.x + side * handOutward, armRestHeight, shoulder.z - reach);
        var elbowHint = new Vector3(shoulder.x + side * (handOutward + 0.05f), armRestHeight, shoulder.z - reach * 0.5f);

        var goalHand = left ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand;
        var goalElbow = left ? AvatarIKHint.LeftElbow : AvatarIKHint.RightElbow;
        animator.SetIKPositionWeight(goalHand, 1f);
        animator.SetIKHintPositionWeight(goalElbow, 1f);
        animator.SetIKPosition(goalHand, bed.TransformPoint(handTarget));
        animator.SetIKHintPosition(goalElbow, bed.TransformPoint(elbowHint));
    }
}
