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
