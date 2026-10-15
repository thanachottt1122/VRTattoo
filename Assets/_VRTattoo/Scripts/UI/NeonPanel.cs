using UnityEngine;

// Shared UI panel (Shaders/NeonPanel.shader): see-through black rounded rectangle with a thin glowing
// pink edge. Sits on a quad; the quad is scaled to `size` plus the glow padding on every side.
// Used by the job card, its DONE button, the difficulty tag and the chair hint.
[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class NeonPanel : MonoBehaviour
{
    public static readonly Color NeonPink = new Color(1.25f, 0.28f, 0.95f, 1f);

    [Tooltip("Size of the panel itself, without the glow (m)")]
    public Vector2 size = new Vector2(0.38f, 0.3f);
    [Tooltip("Room around the panel for the glow to fade out (m)")]
    public float pad = 0.012f;
    public float cornerRadius = 0.025f;

    [ColorUsage(true, true)] public Color edgeColor = NeonPink;
    public Color fillColor = new Color(0.02f, 0.01f, 0.03f, 0.78f);
    public float edgeWidth = 0.0025f;
    [Tooltip("How far the glow fades out around the panel (m)")]
    public float glowWidth = 0.005f;
    [Range(0f, 2f)] public float glow = 0.35f;

    [Tooltip("Brighter edge while pointed at (buttons)")]
    [Range(0f, 1f)] public float hover;
    [Range(0f, 1f)] public float alpha = 1f;

    static readonly int FillId = Shader.PropertyToID("_FillColor");
    static readonly int EdgeId = Shader.PropertyToID("_EdgeColor");
    static readonly int SizeId = Shader.PropertyToID("_Size");
    static readonly int PadId = Shader.PropertyToID("_Pad");
    static readonly int RadiusId = Shader.PropertyToID("_Radius");
    static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
    static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
    static readonly int GlowId = Shader.PropertyToID("_Glow");
    static readonly int HoverId = Shader.PropertyToID("_Hover");
    static readonly int AlphaId = Shader.PropertyToID("_Alpha");

    MeshRenderer meshRenderer;
    MaterialPropertyBlock block;

    void OnEnable() => Apply();
    void OnValidate() => Apply();
    void LateUpdate() => Apply();

    public void Apply()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        block ??= new MaterialPropertyBlock();

        Vector2 quad = size + Vector2.one * (pad * 2f);
        transform.localScale = new Vector3(quad.x, quad.y, 1f);

        meshRenderer.enabled = alpha > 0.001f;
        meshRenderer.GetPropertyBlock(block);
        block.SetColor(FillId, fillColor);
        block.SetColor(EdgeId, edgeColor);
        block.SetVector(SizeId, quad);
        block.SetFloat(PadId, pad);
        block.SetFloat(RadiusId, cornerRadius);
        block.SetFloat(EdgeWidthId, edgeWidth);
        block.SetFloat(GlowWidthId, glowWidth);
        block.SetFloat(GlowId, glow);
        block.SetFloat(HoverId, hover);
        block.SetFloat(AlphaId, alpha);
        meshRenderer.SetPropertyBlock(block);
    }
}
