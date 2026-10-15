using System;
using UnityEngine;

// Paintable skin: owns a transparent RenderTexture that holds the ink (alpha = ink, rgb = colour x alpha) in the UV space
// of the collider it sits on. Something else displays it (CustomerTattooSkin's overlay mesh), and
// TattooMachine finds it through the collider it hits.
// A second texture (needle map) remembers the largest needle that put ink on each pixel, so scoring can
// allow a wide needle the edge overflow it naturally makes.
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

    // Needle map value 1 = this needle diameter (m).
    public const float MaxNeedleDiameter = 0.01f;

    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int NeedleId = Shader.PropertyToID("_Needle");

    RenderTexture tattooTexture;
    RenderTexture needleTexture;
    Vector2 metersPerUV = Vector2.one;
    Material brushInstance; // own copy, so changing the ink colour never edits the material asset

    public RenderTexture TattooTexture => tattooTexture;

    // R = diameter of the largest needle that inked the pixel ÷ MaxNeedleDiameter (0 = never inked).
    public RenderTexture NeedleTexture => needleTexture;

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
        // Linear: the values are data (needle sizes), not colours.
        needleTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear)
        {
            name = "TattooNeedleMap",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            useMipMap = false,
        };
        needleTexture.Create();
        Clear();
    }

    void OnDestroy()
    {
        if (tattooTexture != null)
        {
            tattooTexture.Release();
            Destroy(tattooTexture);
        }
        if (needleTexture != null)
        {
            needleTexture.Release();
            Destroy(needleTexture);
        }
        if (brushInstance != null)
            Destroy(brushInstance);
    }

    // Time.time of the last stroke (CustomerVoice uses it to know the customer is being tattooed).
    public float LastDrawTime { get; private set; } = float.NegativeInfinity;

    // Every stroke segment drawn: world end point and needle diameter (CustomerPain listens).
    public event Action<Vector3, float> Stroked;

    // Shifts where ink lands on the skin (m, x = around the arm, y = along it). CustomerPain sets it
    // for a moment when the customer flinches, so the line jumps a little.
    public Vector2 Nudge { get; set; }

    public void Clear()
    {
        var previous = RenderTexture.active;
        RenderTexture.active = tattooTexture;
        GL.Clear(false, true, Color.clear);
        if (needleTexture != null)
        {
            RenderTexture.active = needleTexture;
            GL.Clear(false, true, Color.clear);
        }
        RenderTexture.active = previous;
    }

    // Draws a continuous line between two contact points. Pass the same point twice to start a stroke.
    public void DrawStroke(Vector2 uvFrom, Vector3 worldFrom, Vector2 uvTo, Vector3 worldTo)
        => DrawStroke(uvFrom, worldFrom, uvTo, worldTo, inkColor);

    // Same, in the given ink colour. The texture is premultiplied (rgb = colour x alpha), so strokes keep their own colour.
    public void DrawStroke(Vector2 uvFrom, Vector3 worldFrom, Vector2 uvTo, Vector3 worldTo, Color color)
        => DrawStroke(uvFrom, worldFrom, uvTo, worldTo, color, brushDiameter);

    // Same, with the line width of the needle in use (m).
    public void DrawStroke(Vector2 uvFrom, Vector3 worldFrom, Vector2 uvTo, Vector3 worldTo, Color color, float diameter)
    {
        if (brushMaterial == null || tattooTexture == null)
            return;
        LastDrawTime = Time.time;
        Stroked?.Invoke(worldTo, diameter);
        if (Nudge != Vector2.zero)
        {
            var shift = new Vector2(Nudge.x / metersPerUV.x, Nudge.y / metersPerUV.y);
            uvFrom += shift;
            uvTo += shift;
        }

        Vector2 uvDelta = uvTo - uvFrom;
        float uvMeters = new Vector2(uvDelta.x * metersPerUV.x, uvDelta.y * metersPerUV.y).magnitude;
        float worldMeters = Vector3.Distance(worldFrom, worldTo);
        if (uvMeters > worldMeters * seamRatio + diameter)
        {
            // Crossing a UV seam: interpolating in UV would smear a line across the texture.
            uvFrom = uvTo;
            uvMeters = 0f;
        }

        float step = Mathf.Max(diameter * spacing, 0.0001f);
        int stamps = Mathf.Clamp(Mathf.CeilToInt(uvMeters / step), 1, 2048);
        float halfU = diameter * 0.5f / metersPerUV.x;
        float halfV = diameter * 0.5f / metersPerUV.y;

        if (brushInstance == null || brushInstance.shader != brushMaterial.shader)
        {
            if (brushInstance != null)
                Destroy(brushInstance);
            brushInstance = new Material(brushMaterial);
        }
        brushInstance.SetColor(ColorId, color);
        brushInstance.SetFloat(NeedleId, Mathf.Clamp01(diameter / MaxNeedleDiameter));

        var previous = RenderTexture.active;
        Stamp(tattooTexture, 0, uvFrom, uvTo, stamps, halfU, halfV);
        if (needleTexture != null && brushInstance.passCount > 1)
            Stamp(needleTexture, 1, uvFrom, uvTo, stamps, halfU, halfV);
        RenderTexture.active = previous;
    }

    void Stamp(RenderTexture target, int pass, Vector2 uvFrom, Vector2 uvTo, int stamps, float halfU, float halfV)
    {
        RenderTexture.active = target;
        GL.PushMatrix();
        GL.LoadOrtho();
        brushInstance.SetPass(pass);
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
    }
}
