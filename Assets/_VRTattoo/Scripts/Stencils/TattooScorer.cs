using UnityEngine;

public struct TattooScore
{
    public float coverage;  // fraction of the stencil lines that got ink (0..1)
    public float spill;     // ink outside the allowed band, relative to the stencil line area
    public int quality;     // 0..100
}

// Compares the ink on a TattooCanvas with the stencil that was shown on it.
public static class TattooScorer
{
    const float LineAlpha = 0.5f;      // stencil alpha above this is part of the design
    const float SpillWeight = 0.5f;    // how much each unit of spill takes off the coverage
    const float ExtraTolerance = 0.001f; // allowed distance from the line on top of the needle radius (m)
    const float Margin = 0.02f;        // ink this far outside the stencil square still counts as spill (m)

    public static TattooScore Score(TattooCanvas canvas, Texture2D stencil, StencilPlacement placement)
    {
        var result = new TattooScore();
        var rt = canvas.TattooTexture;
        if (rt == null || stencil == null)
            return result;

        int size = rt.width;
        Vector2 metersPerUV = canvas.MetersPerUV;
        Vector2 metersPerPixel = metersPerUV / size;
        int tolX = Mathf.CeilToInt((canvas.brushDiameter * 0.5f + ExtraTolerance) / metersPerPixel.x);
        int tolY = Mathf.CeilToInt((canvas.brushDiameter * 0.5f + ExtraTolerance) / metersPerPixel.y);

        // Pixel region around the stencil.
        Rect b = placement.uvBounds;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(b.xMin * size - Margin / metersPerPixel.x), 0, size - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(b.yMin * size - Margin / metersPerPixel.y), 0, size - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(b.xMax * size + Margin / metersPerPixel.x), x0 + 1, size);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(b.yMax * size + Margin / metersPerPixel.y), y0 + 1, size);
        int w = x1 - x0, h = y1 - y0;

        Color32[] pixels = ReadRegion(rt, x0, y0, w, h);
        float skinLum = Luminance(ReadClearColor(rt, canvas.skinColor));
        float inkLum = Luminance(ReadClearColor(rt, canvas.inkColor));
        float inkThreshold = (skinLum + inkLum) * 0.5f;

        var line = new bool[w * h];
        var ink = new bool[w * h];
        int lineCount = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                ink[i] = Luminance(pixels[i]) < inkThreshold;

                Vector2 uv = new Vector2((x0 + x + 0.5f) / size, (y0 + y + 0.5f) / size);
                Vector2 st = placement.ToStencil(uv);
                if (st.x >= 0f && st.x <= 1f && st.y >= 0f && st.y <= 1f && stencil.GetPixelBilinear(st.x, st.y).a > LineAlpha)
                {
                    line[i] = true;
                    lineCount++;
                }
            }
        }
        if (lineCount == 0)
            return result;

        bool[] allowed = Dilate(line, w, h, tolX, tolY);
        int covered = 0, spilled = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (!ink[i])
                continue;
            if (line[i])
                covered++;
            else if (!allowed[i])
                spilled++;
        }

        result.coverage = covered / (float)lineCount;
        result.spill = spilled / (float)lineCount;
        result.quality = Mathf.RoundToInt(100f * Mathf.Clamp01(result.coverage - SpillWeight * result.spill));
        return result;
    }

    static float Luminance(Color32 c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    static Color32[] ReadRegion(RenderTexture rt, int x, int y, int w, int h)
    {
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(x, y, w, h), 0, 0, false);
        tex.Apply(false);
        RenderTexture.active = previous;
        var pixels = tex.GetPixels32();
        Object.Destroy(tex);
        return pixels;
    }

    // Reads back what a colour looks like after going through the same RenderTexture and
    // read-back path as the canvas, so colour-space conversions cancel out.
    static Color32 ReadClearColor(RenderTexture like, Color color)
    {
        var temp = RenderTexture.GetTemporary(1, 1, 0, like.format);
        var previous = RenderTexture.active;
        RenderTexture.active = temp;
        GL.Clear(false, true, color);
        RenderTexture.active = previous;
        var result = ReadRegion(temp, 0, 0, 1, 1)[0];
        RenderTexture.ReleaseTemporary(temp);
        return result;
    }

    // Rectangular dilation (separable running count).
    static bool[] Dilate(bool[] src, int w, int h, int rx, int ry)
    {
        var tmp = new bool[src.Length];
        for (int y = 0; y < h; y++)
        {
            int count = 0;
            for (int x = -rx; x < w; x++)
            {
                int add = x + rx, remove = x - rx - 1;
                if (add < w && src[y * w + add]) count++;
                if (remove >= 0 && src[y * w + remove]) count--;
                if (x >= 0) tmp[y * w + x] = count > 0;
            }
        }
        var dst = new bool[src.Length];
        for (int x = 0; x < w; x++)
        {
            int count = 0;
            for (int y = -ry; y < h; y++)
            {
                int add = y + ry, remove = y - ry - 1;
                if (add < h && tmp[add * w + x]) count++;
                if (remove >= 0 && tmp[remove * w + x]) count--;
                if (y >= 0) dst[y * w + x] = count > 0;
            }
        }
        return dst;
    }
}
