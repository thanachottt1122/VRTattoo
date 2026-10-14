using UnityEngine;

// Paintable skin: owns a RenderTexture used as the base map of this renderer's material.
[RequireComponent(typeof(Renderer))]
public class TattooCanvas : MonoBehaviour
{
    public int textureSize = 1024;
    public Color skinColor = new Color(0.87f, 0.69f, 0.57f);
    public Material brushMaterial;
    public Color inkColor = new Color(0.02f, 0.02f, 0.02f);

    [Tooltip("Line width on the skin surface (m)")]
    public float brushDiameter = 0.005f;

    [Tooltip("Distance between stamps, as a fraction of brush diameter")]
    public float spacing = 0.2f;

    [Tooltip("A stroke segment is treated as crossing a UV seam when its UV length exceeds the real distance by this factor")]
    public float seamRatio = 2f;

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    RenderTexture tattooTexture;
    Material skinMaterial;
    Vector2 metersPerUV = Vector2.one;

    public RenderTexture TattooTexture => tattooTexture;
    public Vector2 MetersPerUV => metersPerUV;

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

        skinMaterial = GetComponent<Renderer>().material;
        skinMaterial.SetTexture(BaseMapId, tattooTexture);
        skinMaterial.SetTexture(MainTexId, tattooTexture);

        metersPerUV = EstimateMetersPerUV();
    }

    void OnDestroy()
    {
        if (tattooTexture != null)
        {
            tattooTexture.Release();
            Destroy(tattooTexture);
        }
        if (skinMaterial != null)
            Destroy(skinMaterial);
    }

    public void Clear()
    {
        var previous = RenderTexture.active;
        RenderTexture.active = tattooTexture;
        GL.Clear(false, true, ClearValue(skinColor));
        RenderTexture.active = previous;
    }

    // GL.Clear takes a linear value in a linear-space project; colours in the Inspector are sRGB.
    // Without the conversion the skin comes out much lighter than skinColor.
    public static Color ClearValue(Color color) =>
        QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;

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

    // Area-weighted average of how many world meters one UV unit spans along U and V,
    // so the brush stays round on the surface even when the UVs are stretched.
    Vector2 EstimateMetersPerUV()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
        if (mesh == null || !mesh.isReadable)
        {
            float size = GetComponent<Renderer>().bounds.size.magnitude;
            return new Vector2(size, size);
        }

        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;
        if (uvs.Length != vertices.Length)
            return Vector2.one;

        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = transform.TransformPoint(vertices[i]);

        double sumU = 0, sumV = 0, sumArea = 0;
        for (int t = 0; t < triangles.Length; t += 3)
        {
            int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
            Vector3 e1 = vertices[b] - vertices[a];
            Vector3 e2 = vertices[c] - vertices[a];
            Vector2 d1 = uvs[b] - uvs[a];
            Vector2 d2 = uvs[c] - uvs[a];

            float det = d1.x * d2.y - d2.x * d1.y;
            if (Mathf.Abs(det) < 1e-10f)
                continue;

            float r = 1f / det;
            Vector3 dPdu = (e1 * d2.y - e2 * d1.y) * r;
            Vector3 dPdv = (e2 * d1.x - e1 * d2.x) * r;
            float area = Vector3.Cross(e1, e2).magnitude * 0.5f;

            sumU += dPdu.magnitude * area;
            sumV += dPdv.magnitude * area;
            sumArea += area;
        }

        if (sumArea <= 0)
            return Vector2.one;

        return new Vector2((float)(sumU / sumArea), (float)(sumV / sumArea));
    }
}
