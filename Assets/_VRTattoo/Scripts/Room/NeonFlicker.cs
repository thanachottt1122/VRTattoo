using System.Collections;
using TMPro;
using UnityEngine;

// Old-neon flicker: every few seconds the sign dims for a few quick blinks, then returns to full.
// Works on runtime material instances (TMP face/glow + additive halo), so assets are untouched.
// Each sign rolls its own random timings, so two signs never stay in sync.
// Brightness is read by LightBuzz so the sign's hum dips with each blink.
public class NeonFlicker : MonoBehaviour
{
    public TextMeshPro text;
    public Renderer halo;

    [Header("Timing (seconds)")]
    [Tooltip("Random wait between flicker bursts.")]
    public float minInterval = 6f;
    public float maxInterval = 14f;
    [Tooltip("Extra random delay before the first burst (0..this).")]
    public float maxStartDelay = 6f;

    [Header("Burst")]
    public int minFlickers = 1;
    public int maxFlickers = 3;
    [Tooltip("Brightness while dimmed (0 = off, 1 = no change). Kept above 0 so it is not harsh in VR.")]
    [Range(0f, 1f)] public float dimLevel = 0.35f;
    public float minDimTime = 0.04f;
    public float maxDimTime = 0.12f;
    public float minGapTime = 0.05f;
    public float maxGapTime = 0.15f;

    [Tooltip("Log each burst to the console (for tuning).")]
    public bool logFlickers;

    static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");
    static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    // 1 = full, dimLevel while blinking.
    public float Brightness { get; private set; } = 1f;

    Material textMaterial;
    Material haloMaterial;
    Color faceColor, glowColor, haloColor;

    void Start()
    {
        if (text != null)
        {
            textMaterial = text.fontMaterial; // instance
            faceColor = textMaterial.GetColor(FaceColorId);
            glowColor = textMaterial.GetColor(GlowColorId);
        }
        if (halo != null)
        {
            haloMaterial = halo.material; // instance
            haloColor = haloMaterial.GetColor(BaseColorId);
        }
        StartCoroutine(FlickerLoop());
    }

    IEnumerator FlickerLoop()
    {
        yield return new WaitForSeconds(Random.Range(0f, maxStartDelay));
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            int count = Random.Range(minFlickers, maxFlickers + 1);
            if (logFlickers)
                Debug.Log($"[NeonFlicker] {name} t={Time.time:F2} x{count}");
            for (int i = 0; i < count; i++)
            {
                SetBrightness(dimLevel);
                yield return new WaitForSeconds(Random.Range(minDimTime, maxDimTime));
                SetBrightness(1f);
                yield return new WaitForSeconds(Random.Range(minGapTime, maxGapTime));
            }
        }
    }

    void SetBrightness(float k)
    {
        Brightness = k;
        if (textMaterial != null)
        {
            textMaterial.SetColor(FaceColorId, Scale(faceColor, k));
            textMaterial.SetColor(GlowColorId, Scale(glowColor, k));
        }
        if (haloMaterial != null)
            haloMaterial.SetColor(BaseColorId, Scale(haloColor, k));
    }

    static Color Scale(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

    void OnDisable() => SetBrightness(1f);

    void OnDestroy()
    {
        if (textMaterial != null) Destroy(textMaterial);
        if (haloMaterial != null) Destroy(haloMaterial);
    }
}
