using UnityEngine;

// On the Customer prefab: a mood emote that pops up above the customer's head after the tattoo is
// submitted. It always faces the player and stays until the customer is destroyed at the door.
public class CustomerEmote : MonoBehaviour
{
    [Header("Sprites (UI/Emotes)")]
    public Sprite ecstatic;
    public Sprite happy;
    public Sprite neutral;
    public Sprite angry;

    [Header("Look")]
    [Tooltip("Width of the emote (m)")]
    public float size = 0.28f;

    [Tooltip("Height of the emote's centre above the head bone (world up, m)")]
    public float heightAboveHead = 0.32f;

    [Tooltip("Gentle up/down float (m)")]
    public float bobHeight = 0.02f;
    public float bobSpeed = 2f;

    [Header("Pop-in")]
    public float popDuration = 0.4f;

    [Tooltip("How far below its resting height the emote starts (m)")]
    public float popRise = 0.12f;

    [Tooltip("Leave empty to use Camera.main")]
    public Transform viewer;

    public CustomerMood? Mood { get; private set; }

    AnimatorCustomerVisual visual;
    SpriteRenderer sprite;
    Transform head;
    float shownTime;

    void Awake()
    {
        visual = GetComponent<AnimatorCustomerVisual>();

        var go = new GameObject("Emote");
        go.transform.SetParent(transform, false);
        sprite = go.AddComponent<SpriteRenderer>();
        sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sprite.receiveShadows = false;
        go.SetActive(false);
    }

    public void Show(CustomerMood mood)
    {
        var picked = SpriteFor(mood);
        Mood = mood;
        shownTime = Time.time;
        if (picked == null)
        {
            Debug.LogWarning($"[CustomerEmote] {name}: no sprite for {mood}");
            return;
        }

        sprite.sprite = picked;
        sprite.gameObject.SetActive(true);
        LateUpdate();
    }

    public void Hide()
    {
        Mood = null;
        sprite.gameObject.SetActive(false);
    }

    Sprite SpriteFor(CustomerMood mood) => mood switch
    {
        CustomerMood.Ecstatic => ecstatic,
        CustomerMood.Happy => happy,
        CustomerMood.Neutral => neutral,
        _ => angry,
    };

    void LateUpdate()
    {
        if (!sprite.gameObject.activeSelf)
            return;

        var t = sprite.transform;
        float age = Time.time - shownTime;
        float pop = popDuration > 0f ? Mathf.Clamp01(age / popDuration) : 1f;

        Vector3 anchor = HeadPosition() + Vector3.up * heightAboveHead;
        float bob = Mathf.Sin(age * bobSpeed * Mathf.PI) * bobHeight * pop;
        t.position = anchor + Vector3.up * (bob - (1f - EaseOutCubic(pop)) * popRise);

        // Sprites face -Z towards the viewer when forward points away from it.
        var cam = viewer != null ? viewer : (Camera.main != null ? Camera.main.transform : null);
        if (cam != null)
        {
            Vector3 away = t.position - cam.position;
            if (away.sqrMagnitude > 0.0001f)
                t.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        float spriteWidth = sprite.sprite != null ? sprite.sprite.bounds.size.x : 1f;
        t.localScale = Vector3.one * (size / Mathf.Max(spriteWidth, 0.0001f) * EaseOutBack(pop));
    }

    Vector3 HeadPosition()
    {
        if (head == null && visual != null && visual.animator != null && visual.animator.isHuman)
            head = visual.animator.GetBoneTransform(HumanBodyBones.Head);
        return head != null ? head.position : transform.position + Vector3.up * 1.7f;
    }

    static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

    // Overshoots to ~1.1 before settling at 1.
    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
