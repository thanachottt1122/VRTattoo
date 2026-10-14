using UnityEngine;

// Root of one character model (same object as its Animator). Holds the per-character tuning that
// lines the model up with the pose pivots of CustomerVisual. While lying on the bed it also
// straightens the pose with IK: the arm on the artist's side (the right arm with the current bed
// and clip) goes inside TattooArm, the other arm lies along the body and the feet come together.
public class CustomerCharacter : MonoBehaviour
{
    public Animator animator;

    [Header("Sitting (relative to the seat pivot)")]
    public Vector3 sitOffset = new Vector3(0f, -0.41f, 0f);
    public Vector3 sitRotation;

    [Header("Lying (relative to the bed pivot, forward = towards the head)")]
    [Tooltip("x moves the body sideways onto the bed's centre line, z moves the head onto the pillow")]
    public Vector3 lieOffset;
    [Tooltip("The lying clip has the head towards -Z, so the model is turned around")]
    public Vector3 lieRotation = new Vector3(0f, 180f, 0f);

    [Header("Tattoo arm")]
    [Tooltip("TattooArm is painted this colour, close to the skin of this character")]
    public Color skinColor = new Color(0.87f, 0.69f, 0.57f);

    [Tooltip("Pull the arm nearest TattooArm along it while lying, so the real arm stays inside it")]
    public bool alignArm = true;

    [Tooltip("How far the hand target sits inside the end of TattooArm (m)")]
    public float handInset = 0.02f;

    [Tooltip("How far below the TattooArm axis the real arm is pulled, so it stays hidden inside it (m)")]
    public float armDrop = 0.06f;

    [Header("Straight pose while lying (the clip spreads arms and legs)")]
    [Tooltip("Lay the other arm along the body instead of spread out")]
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

    Transform tattooArm;
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

    // null stops the arm alignment.
    public void SetTattooArm(Transform arm)
    {
        tattooArm = arm;
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

        bool leftDone = false, rightDone = false;
        if (alignArm && tattooArm != null)
        {
            // Which arm lies on the TattooArm side depends on the clip and the bed, so take the
            // shoulder that is closer to it.
            var left = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
            var right = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
            bool useLeft = (left - tattooArm.position).sqrMagnitude < (right - tattooArm.position).sqrMagnitude;
            AlignToTattooArm(useLeft);
            leftDone = useLeft;
            rightDone = !useLeft;
        }

        if (armsAlongBody)
        {
            if (!leftDone)
                LayAlongBody(bed, true);
            if (!rightDone)
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

    void AlignToTattooArm(bool useLeft)
    {
        var upper = animator.GetBoneTransform(useLeft ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
        var lower = animator.GetBoneTransform(useLeft ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
        var hand = animator.GetBoneTransform(useLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        float armLength = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position);
        var shoulder = upper.position;
        var goalHand = useLeft ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand;
        var goalElbow = useLeft ? AvatarIKHint.LeftElbow : AvatarIKHint.RightElbow;

        // TattooArm is a Unity capsule: 2 units long along its local Y. The end further from the
        // shoulder gets the hand, the elbow is kept along the capsule axis.
        Vector3 axis = tattooArm.up * tattooArm.lossyScale.y;
        Vector3 endA = tattooArm.position + axis, endB = tattooArm.position - axis;
        Vector3 handEnd = (endA - shoulder).sqrMagnitude > (endB - shoulder).sqrMagnitude ? endA : endB;
        Vector3 toShoulder = (shoulder - handEnd).normalized;

        animator.SetIKPositionWeight(goalHand, 1f);
        animator.SetIKHintPositionWeight(goalElbow, 1f);
        Vector3 drop = Vector3.down * armDrop;
        Vector3 handTarget = handEnd + toShoulder * handInset + drop;

        // A short arm that cannot reach the end would be stretched straight and pass over the top
        // of the capsule; stop the hand where it can reach so the arm stays low, inside it.
        Vector3 reach = handTarget - shoulder;
        if (reach.magnitude > armLength * 0.98f)
            handTarget = shoulder + reach.normalized * armLength * 0.98f;

        animator.SetIKPosition(goalHand, handTarget);
        animator.SetIKHintPosition(goalElbow, tattooArm.position + drop);
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
