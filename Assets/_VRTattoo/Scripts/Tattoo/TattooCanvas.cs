using UnityEngine;

// Paintable skin: owns a transparent RenderTexture that holds the ink (alpha = ink) in the UV space
// of the collider it sits on. Something else displays it (CustomerTattooSkin's overlay mesh), and
// TattooMachine finds it through the collider it hits.
public class TattooCanvas : MonoBehaviour
{
    public int textureSize = 1024;
    public Material brushMaterial;
    public Color inkColor = new Color(0.02f, 0.02f, 0.02f);

    [Tooltip("Line width on the skin surface (m)")]
    public float brushDiameter = 0.005f;

    [Tooltip("Distance between stamps, as a fraction of brush diameter")]
    public float spacing = 0.2f;

    [Tooltip("A stroke segment is treated as crossing a UV seam when its UV length exceeds the real distance by this factor")]
    public float seamRatio = 2f;

    static readonly int ColorId = Shader.PropertyToID("_Color");

    RenderTexture tattooTexture;
    Vector2 metersPerUV = Vector2.one;

    public RenderTexture TattooTexture => tattooTexture;

    // How many metres on the skin one UV unit spans along U and V (set by whoever made the UVs).
    public Vector2 MetersPerUV
    {
        get => metersPerUV;
        set => metersPerUV = value;
    }

    void Awake()
    {
        tattooTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32)
        {
            name = "TattooTexture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            useMipMap = false,
        };
        tattooTexture.Create();
        Clear();
    }

    void OnDestroy()
    {
        if (tattooTexture != null)
        {
            tattooTexture.Release();
            Destroy(tattooTexture);
        }
    }

    public void Clear()
    {
        var previous = RenderTexture.active;
        RenderTexture.active = tattooTexture;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = previous;
    }

    // Draws a continuous line between two contact points. Pass the same point twice to start a stroke.
    public void DrawStroke(Vector2 uvFrom, Vector3 worldFrom, Vector2 uvTo, Vector3 worldTo)
    {
        if (brushMaterial == null || tattooTexture == null)
            return;

        Vector2 uvDelta = uvTo - uvFrom;
        float uvMeters = new Vector2(uvDelta.x * metersPerUV.x, uvDelta.y * metersPerUV.y).magnitude;
        float worldMeters = Vector3.Distance(worldFrom, worldTo);
        if (uvMeters > worldMeters * seamRatio + brushDiameter)
        {
            // Crossing a UV seam: interpolating in UV would smear a line across the texture.
            uvFrom = uvTo;
            uvMeters = 0f;
        }

        float step = Mathf.Max(brushDiameter * spacing, 0.0001f);
        int stamps = Mathf.Clamp(Mathf.CeilToInt(uvMeters / step), 1, 2048);
        float halfU = brushDiameter * 0.5f / metersPerUV.x;
        float halfV = brushDiameter * 0.5f / metersPerUV.y;

        brushMaterial.SetColor(ColorId, inkColor);

        var previous = RenderTexture.active;
        RenderTexture.active = tattooTexture;
        GL.PushMatrix();
        GL.LoadOrtho();
        brushMaterial.SetPass(0);
        GL.Begin(GL.QUADS);
        for (int i = 1; i <= stamps; i++)
        {
            Vector2 p = Vector2.Lerp(uvFrom, uvTo, i / (float)stamps);
            GL.TexCoord2(0f, 0f); GL.Vertex3(p.x - halfU, p.y - halfV, 0f);
            GL.TexCoord2(0f, 1f); GL.Vertex3(p.x - halfU, p.y + halfV, 0f);
            GL.TexCoord2(1f, 1f); GL.Vertex3(p.x + halfU, p.y + halfV, 0f);
            GL.TexCoord2(1f, 0f); GL.Vertex3(p.x + halfU, p.y - halfV, 0f);
        }
        GL.End();
        GL.PopMatrix();
        RenderTexture.active = previous;
    }
}
