using UnityEngine;

// Colours some renderers with an ink colour (MaterialPropertyBlock, so all ink objects share one material).
// Used by ink caps and decorative ink bottles; visible in the Editor too.
[ExecuteAlways]
public class InkTint : MonoBehaviour
{
    public Color color = new Color(0.02f, 0.02f, 0.02f);

    [Tooltip("Renderers that show the ink colour (the ink surface, the bottle body...)")]
    public Renderer[] renderers;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    MaterialPropertyBlock block;

    void OnEnable() => Apply();

    void OnValidate() => Apply();

    public void Apply()
    {
        if (renderers == null)
            return;

        block ??= new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            if (r == null)
                continue;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            r.SetPropertyBlock(block);
        }
    }
}
