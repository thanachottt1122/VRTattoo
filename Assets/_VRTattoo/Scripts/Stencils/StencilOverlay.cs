using UnityEngine;

// Where a stencil sits on the skin, as a mapping between the canvas UV and the stencil image (st).
public struct StencilPlacement
{
    public Vector2 centerUV;
    public Vector2 axisX;  // st.x = 0.5 + dot(uv - centerUV, axisX)
    public Vector2 axisY;  // st.y = 0.5 + dot(uv - centerUV, axisY)
    public Rect uvBounds;  // UV rectangle covering the whole stencil

    public Vector2 ToStencil(Vector2 uv)
    {
        Vector2 d = uv - centerUV;
        return new Vector2(0.5f + Vector2.Dot(d, axisX), 0.5f + Vector2.Dot(d, axisY));
    }
}

// Shows the requested design as a faint stencil on the arm (TattooArm). The overlay is a second
// material on the same renderer, so the ink RenderTexture of TattooCanvas is never touched.
[RequireComponent(typeof(TattooCanvas), typeof(MeshCollider))]
public class StencilOverlay : MonoBehaviour
{
    public Material overlayMaterial;

    [Tooltip("Width and height of the stencil on the skin (m)")]
    public float sizeMeters = 0.10f;

    [Tooltip("World direction the top of the stencil points to (away from the tattoo chair)")]
    public Vector3 stencilUp = Vector3.back;

    static readonly int StencilTexId = Shader.PropertyToID("_StencilTex");
    static readonly int CenterId = Shader.PropertyToID("_Center");
    static readonly int AxisXId = Shader.PropertyToID("_AxisX");
    static readonly int AxisYId = Shader.PropertyToID("_AxisY");

    Renderer skinRenderer;
    MeshCollider meshCollider;
    Material overlayInstance;

    public bool IsShowing { get; private set; }
    public StencilPlacement Placement { get; private set; }
    public Texture2D CurrentStencil { get; private set; }

    void Awake()
    {
        skinRenderer = GetComponent<Renderer>();
        meshCollider = GetComponent<MeshCollider>();
        if (overlayMaterial != null)
            overlayInstance = new Material(overlayMaterial);
    }

    void OnDestroy()
    {
        if (overlayInstance != null)
            Destroy(overlayInstance);
    }

    public void Show(Texture2D stencil)
    {
        if (overlayInstance == null || stencil == null || !TryComputePlacement(out var placement))
            return;

        Placement = placement;
        CurrentStencil = stencil;
        overlayInstance.SetTexture(StencilTexId, stencil);
        overlayInstance.SetVector(CenterId, placement.centerUV);
        overlayInstance.SetVector(AxisXId, placement.axisX);
        overlayInstance.SetVector(AxisYId, placement.axisY);

        if (!IsShowing)
        {
            // Material 0 is already the instance TattooCanvas created in Awake.
            var materials = skinRenderer.sharedMaterials;
            skinRenderer.sharedMaterials = new[] { materials[0], overlayInstance };
            IsShowing = true;
        }
    }

    public void Hide()
    {
        if (!IsShowing)
            return;
        skinRenderer.sharedMaterials = new[] { skinRenderer.sharedMaterials[0] };
        IsShowing = false;
        CurrentStencil = null;
    }

    // Centre: the top of the arm (ray straight down). Axes: how UV changes when moving a small
    // distance right/up on the skin, inverted so a UV offset turns into a stencil offset.
    bool TryComputePlacement(out StencilPlacement placement)
    {
        placement = default;
        Vector3 top = skinRenderer.bounds.center;
        if (!RaycastDown(top, out var hit))
            return false;

        Vector3 normal = hit.normal;
        Vector3 up = Vector3.ProjectOnPlane(stencilUp, normal).normalized;
        Vector3 right = Vector3.Cross(normal, up);

        const float step = 0.01f;
        if (!RaycastDown(hit.point + right * step, out var hitRight) || !RaycastDown(hit.point + up * step, out var hitUp))
            return false;

        Vector2 uv0 = hit.textureCoord;
        Vector2 duvRight = (hitRight.textureCoord - uv0) / step;  // UV per meter to the right
        Vector2 duvUp = (hitUp.textureCoord - uv0) / step;        // UV per meter up

        // [duvRight duvUp] maps meters -> UV; its inverse maps UV -> meters.
        float det = duvRight.x * duvUp.y - duvUp.x * duvRight.y;
        if (Mathf.Abs(det) < 1e-6f)
            return false;
        Vector2 metersRightRow = new Vector2(duvUp.y, -duvUp.x) / det;
        Vector2 metersUpRow = new Vector2(-duvRight.y, duvRight.x) / det;

        placement.centerUV = uv0;
        placement.axisX = metersRightRow / sizeMeters;
        placement.axisY = metersUpRow / sizeMeters;

        // Bounds from the four stencil corners.
        float half = sizeMeters * 0.5f;
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        foreach (var corner in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
        {
            Vector2 uv = uv0 + (duvRight * corner.x + duvUp * corner.y) * half;
            min = Vector2.Min(min, uv);
            max = Vector2.Max(max, uv);
        }
        placement.uvBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    bool RaycastDown(Vector3 point, out RaycastHit hit)
    {
        return meshCollider.Raycast(new Ray(point + Vector3.up * 0.3f, Vector3.down), out hit, 1f);
    }
}
