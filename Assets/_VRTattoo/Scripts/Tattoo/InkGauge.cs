using UnityEngine;

// Ink level shown as rings around the tattoo machine: lit rings glow in the current ink colour,
// empty rings are dark. The last ring blinks when the ink is nearly gone.
public class InkGauge : MonoBehaviour
{
    public TattooInk ink;

    [Tooltip("Rings from the bottom (first to empty) to the top")]
    public Renderer[] rings;

    public Color emptyColor = new Color(0.08f, 0.08f, 0.08f);

    [Tooltip("Glow strength of lit rings")]
    public float emission = 1.5f;

    [Tooltip("Below this much ink (%) the last ring blinks")]
    [Range(0f, 100f)] public float lowThreshold = 10f;

    public float blinkRate = 3f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock block;

    void Awake()
    {
        if (ink == null)
            ink = GetComponentInParent<TattooInk>();
    }

    void LateUpdate()
    {
        if (ink == null || rings == null || rings.Length == 0)
            return;

        block ??= new MaterialPropertyBlock();
        int lit = Mathf.CeilToInt(ink.amount / 100f * rings.Length - 0.0001f);
        bool blinkOff = ink.amount > 0f && ink.amount < lowThreshold && Mathf.Repeat(Time.time * blinkRate, 1f) > 0.5f;

        for (int i = 0; i < rings.Length; i++)
        {
            var ring = rings[i];
            if (ring == null)
                continue;

            bool on = i < lit && !(blinkOff && i == 0);
            Color inkColor = ink.color;
            // Very dark inks (black) still need to read as "lit", so they glow grey.
            Color glow = Color.Lerp(inkColor, Color.white, inkColor.maxColorComponent < 0.1f ? 0.35f : 0f);

            ring.GetPropertyBlock(block);
            block.SetColor(BaseColorId, on ? inkColor : emptyColor);
            block.SetColor(EmissionColorId, on ? glow * emission : Color.black);
            ring.SetPropertyBlock(block);
        }
    }
}
