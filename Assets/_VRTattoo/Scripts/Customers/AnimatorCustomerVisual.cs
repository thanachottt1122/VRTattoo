using UnityEngine;

// For a real character model: drives an Animator instead of moving a capsule.
// The model's root should line up with the pose pivots described in CustomerVisual
// (feet when standing, hips on the seat/bed when sitting or lying); per-character corrections
// live on CustomerCharacter.
//
// While lying on the bed the scene's TattooArm stands in for the customer's arm on the artist's
// side: it is shown, painted in the character's skin colour, and the real arm is pulled inside it.
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

    [Tooltip("Hide TattooArm while nobody is lying on the bed")]
    public bool hideTattooArmWhenEmpty = true;

    // The visual currently lying on the bed (owner of TattooArm).
    static AnimatorCustomerVisual lyingVisual;
    static TattooCanvas tattooCanvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        lyingVisual = null;
        tattooCanvas = null;
    }

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        character = GetComponentInChildren<CustomerCharacter>();
    }

    void Awake()
    {
        if (character != null && animator == null)
            animator = character.animator;
        if (lyingVisual == null)
            ShowTattooArm(false);
    }

    void OnDestroy()
    {
        if (lyingVisual == this)
            ReleaseTattooArm();
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

        if (pose == CustomerPose.Lie)
            TakeTattooArm();
        else if (lyingVisual == this)
            ReleaseTattooArm();

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

    // Called before CustomerQueueManager clears the canvas, so the new skin colour is used.
    void TakeTattooArm()
    {
        lyingVisual = this;
        var canvas = FindCanvas();
        if (canvas == null)
            return;

        if (character != null)
        {
            canvas.skinColor = character.skinColor;
            character.SetTattooArm(canvas.transform);
        }
        ShowTattooArm(true);
    }

    void ReleaseTattooArm()
    {
        lyingVisual = null;
        if (character != null)
            character.SetTattooArm(null);
        ShowTattooArm(false);
    }

    void ShowTattooArm(bool show)
    {
        if (!show && !hideTattooArmWhenEmpty)
            return;
        var canvas = FindCanvas();
        if (canvas != null && canvas.TryGetComponent<Renderer>(out var armRenderer))
            armRenderer.enabled = show;
    }

    static TattooCanvas FindCanvas()
    {
        if (tattooCanvas == null)
            tattooCanvas = FindFirstObjectByType<TattooCanvas>();
        return tattooCanvas;
    }
}
