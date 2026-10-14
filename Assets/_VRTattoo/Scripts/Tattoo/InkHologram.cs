using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Ink level as a glowing hologram ring (Shaders/InkHologram.shader) with the % in the middle.
// It floats beside the tattoo machine in the world (not stuck to the screen, which is uncomfortable in VR),
// always faces the player, and only shows while the machine is held in a hand.
public class InkHologram : MonoBehaviour
{
    public TattooInk ink;

    [Tooltip("Ring quad using the InkHologram material")]
    public Renderer ring;
    public TMP_Text percentLabel;

    [Tooltip("Leave empty to use Camera.main")]
    public Camera viewCamera;

    [Tooltip("Where the ring sits relative to the machine, in the player's view: x = right, y = up (m)")]
    public Vector2 viewOffset = new Vector2(0.06f, 0.035f);

    [Tooltip("When the machine is further right than this in view (0-1), the ring moves to its left side")]
    [Range(0.5f, 1f)] public float switchSideAbove = 0.7f;

    [Tooltip("How quickly the ring catches up with the machine (higher = stiffer)")]
    public float followSharpness = 12f;

    [Tooltip("How fast the shown level moves towards the real one (% per second)")]
    public float levelSpeed = 120f;

    [Tooltip("Very dark inks glow at least this bright so the ring stays visible")]
    [Range(0f, 1f)] public float minBrightness = 0.55f;
    public float glow = 2f;

    [Tooltip("How much the % text is mixed towards white (0 = ink colour)")]
    [Range(0f, 1f)] public float labelWhiten = 0.6f;

    [Tooltip("Below this much ink (%) the ring blinks")]
    [Range(0f, 100f)] public float lowThreshold = 15f;
    public float blinkRate = 3f;

    public float fadeSpeed = 6f;

    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int FillId = Shader.PropertyToID("_Fill");
    static readonly int AlphaId = Shader.PropertyToID("_Alpha");

    XRGrabInteractable grabInteractable;
    MaterialPropertyBlock block;
    float shownLevel = -1f;
    float visibility;
    int shownPercent = -1;
    bool placed;
    float side = 1f;

    void Awake()
    {
        if (ink != null)
            grabInteractable = ink.GetComponent<XRGrabInteractable>();
        block = new MaterialPropertyBlock();
        SetVisible(false);
    }

    bool HeldInHand => grabInteractable != null && grabInteractable.isSelected
        && !(grabInteractable.firstInteractorSelecting is XRSocketInteractor);

    void LateUpdate()
    {
        var cam = viewCamera != null ? viewCamera : Camera.main;
        if (ink == null || cam == null)
            return;

        visibility = Mathf.MoveTowards(visibility, HeldInHand ? 1f : 0f, fadeSpeed * Time.deltaTime);
        if (visibility <= 0f)
        {
            SetVisible(false);
            placed = false;
            return;
        }
        SetVisible(true);

        Follow(cam);

        shownLevel = shownLevel < 0f ? ink.amount : Mathf.MoveTowards(shownLevel, ink.amount, levelSpeed * Time.deltaTime);

        bool low = ink.amount < lowThreshold;
        float blink = low && Mathf.Repeat(Time.time * blinkRate, 1f) > 0.5f ? 0.15f : 1f;

        Color color = ink.color;
        float brightness = color.maxColorComponent;
        if (brightness < minBrightness)
            color = Color.Lerp(color, Color.white, (minBrightness - brightness) / Mathf.Max(1f - brightness, 0.001f));
        color.a = 1f;

        ring.GetPropertyBlock(block);
        block.SetColor(ColorId, color * glow);
        block.SetFloat(FillId, shownLevel / 100f);
        block.SetFloat(AlphaId, visibility * blink);
        ring.SetPropertyBlock(block);

        if (percentLabel != null)
        {
            int percent = Mathf.RoundToInt(shownLevel);
            if (percent != shownPercent)
            {
                shownPercent = percent;
                percentLabel.text = percent + "%";
            }
            // Tinted towards white so the number reads clearly on top of the glowing ring.
            Color labelColor = Color.Lerp(color, Color.white, labelWhiten);
            labelColor.a = visibility * blink;
            percentLabel.color = labelColor;
        }
    }

    // Beside the machine from the player's point of view, smoothed so hand jitter doesn't shake it.
    // Normally on the right; switches to the left when the machine is far right in view (e.g. at the ink caps).
    void Follow(Camera cam)
    {
        Vector3 anchor = ink.transform.position;
        float viewX = cam.WorldToViewportPoint(anchor).x;
        if (viewX > switchSideAbove)
            side = -1f;
        else if (viewX < switchSideAbove - 0.1f)
            side = 1f;

        Transform view = cam.transform;
        Vector3 target = anchor + view.right * (viewOffset.x * side) + view.up * viewOffset.y;
        transform.position = placed ? Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-followSharpness * Time.deltaTime)) : target;
        placed = true;

        // Quads and TMP face their -Z side, so +Z points away from the viewer.
        transform.rotation = Quaternion.LookRotation(transform.position - view.position, view.up);
    }

    void SetVisible(bool visible)
    {
        if (ring != null && ring.enabled != visible)
            ring.enabled = visible;
        if (percentLabel != null && percentLabel.enabled != visible)
            percentLabel.enabled = visible;
        if (!visible)
            shownLevel = -1f;
    }
}
