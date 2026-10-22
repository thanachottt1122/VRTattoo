using UnityEngine;

public struct TattooScore
{
    public float accuracy;   // fraction of the stencil lines that got ink on or near them (0..1)
    public float messRatio;  // weighted area of ink outside the design ÷ area of the design (0..)
    public int messPenalty;  // points taken off for the mess (shown to the player)
    public int quality;      // 0..100
}

[System.Serializable]
public struct TattooScoringRules
{
    [Tooltip("A line pixel counts as inked when ink is this close to it (m)")]
    public float coverTolerance;

    [Tooltip("Ink further than this (plus the needle slack) outside the edge of a design line is mess (m)")]
    public float messTolerance;

    [Tooltip("Mess counts more the further it is: from nothing at the tolerance to full at this much further out (m). " +
             "A slightly wobbly edge costs little, ink far from the design costs fully.")]
    public float messFalloff;

    [Tooltip("Extra mess tolerance per metre of radius of the needle that inked the pixel: a wide needle " +
             "naturally overflows the edge, so it gets more room (0 = same for every needle)")]
    public float needleSlack;

    [Tooltip("Mess area (as a multiple of the design's area) that brings the score to 0")]
    public float messLimit;

    [Tooltip("quality = accuracy × (1 − min(1, messRatio / messLimit)^messPower); below 1 = small messes already cost noticeably, " +
             "large ones level off before reaching messLimit")]
    public float messPower;

    // Tuned with VRTattoo/Simulate Tattoo Scores.
    public static TattooScoringRules Default => new TattooScoringRules
    {
        coverTolerance = 0.0015f,
        messTolerance = 0f,
        needleSlack = 1.1f,
        messFalloff = 0.0032f,
        messLimit = 1f,
        messPower = 0.58f,
    };
}

// Compares the ink on a TattooCanvas with the stencil that was shown on it.
//   accuracy  = share of the stencil lines with ink on or within coverTolerance of them
//   messRatio = area of ink outside the lines ÷ area of the lines, counted over the whole canvas so smearing
//               outside the design is never free. Each ink pixel weighs clamp01((distance to the lines − tolerance) / messFalloff),
//               tolerance = messTolerance + needleSlack × radius of the needle that inked it
//   quality   = 100 × accuracy × (1 − min(1, messRatio / messLimit)^messPower)
// All areas are pixel counts on the same canvas (every pixel covers the same skin area).
public static class TattooScorer
{
    const float LineAlpha = 0.5f;  // stencil alpha above this is part of the design
    const byte InkAlpha = 127;     // canvas alpha above this is ink

    public static TattooScore Score(TattooCanvas canvas, Texture2D stencil, StencilPlacement placement) =>
        Score(canvas, stencil, placement, TattooScoringRules.Default);

    public static TattooScore Score(TattooCanvas canvas, Texture2D stencil, StencilPlacement placement, TattooScoringRules rules)
    {
        var rt = canvas.TattooTexture;
        if (rt == null || stencil == null)
            return new TattooScore();

        int size = rt.width;
        Vector2 metersPerPixel = canvas.MetersPerUV / size;

        // The canvas is transparent where there is no ink. The whole of it is read: ink anywhere counts.
        Color32[] pixels = ReadRegion(rt, 0, 0, size, size);
        var ink = new bool[size * size];
        for (int i = 0; i < ink.Length; i++)
            ink[i] = pixels[i].a > InkAlpha;

        // Needle map: R = needle diameter ÷ TattooCanvas.MaxNeedleDiameter.
        byte[] needle = null;
        var needleRT = canvas.NeedleTexture;
        if (needleRT != null && needleRT.width == size && needleRT.height == size)
        {
            Color32[] needlePixels = ReadRegion(needleRT, 0, 0, size, size);
            needle = new byte[size * size];
            for (int i = 0; i < needle.Length; i++)
                needle[i] = needlePixels[i].r;
        }

        bool[] line = StencilMask(stencil, placement, size);
        return ScoreMasks(ink, line, size, size, metersPerPixel, rules, needle);
    }

    // Stencil lines drawn into a size × size canvas grid (only the stencil's own square is sampled).
    public static bool[] StencilMask(Texture2D stencil, StencilPlacement placement, int size)
    {
        var line = new bool[size * size];
        Rect b = placement.uvBounds;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(b.xMin * size), 0, size - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(b.yMin * size), 0, size - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(b.xMax * size), x0 + 1, size);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(b.yMax * size), y0 + 1, size);

        // Nearest-pixel lookups into one copy of the image (GetPixelBilinear per pixel is ~5x slower).
        Color32[] image = stencil.GetPixels32();
        int sw = stencil.width, sh = stencil.height;
        byte lineAlpha = (byte)(LineAlpha * 255f);
        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                Vector2 st = placement.ToStencil(new Vector2((x + 0.5f) / size, (y + 0.5f) / size));
                if (st.x < 0f || st.x >= 1f || st.y < 0f || st.y >= 1f)
                    continue;
                if (image[(int)(st.y * sh) * sw + (int)(st.x * sw)].a > lineAlpha)
                    line[y * size + x] = true;
            }
        }
        return line;
    }

    // Needle map value (0..255) for a needle of this diameter (m).
    public static byte NeedleValue(float diameter) =>
        (byte)Mathf.RoundToInt(Mathf.Clamp01(diameter / TattooCanvas.MaxNeedleDiameter) * 255f);

    // The scoring itself, on ink / line masks of the same w × h grid. Also used by the score simulator.
    // needle (optional) = per-pixel NeedleValue of the needle that inked it; null = no needle slack.
    // lineDistance (optional) = DistanceToMask(line), passed in when scoring many inks against one design.
    public static TattooScore ScoreMasks(bool[] ink, bool[] line, int w, int h, Vector2 metersPerPixel, TattooScoringRules rules,
        byte[] needle = null, float[] lineDistance = null)
    {
        var result = new TattooScore();
        int lineCount = 0;
        for (int i = 0; i < line.Length; i++)
            if (line[i])
                lineCount++;
        if (lineCount == 0)
            return result;

        int coverX = Mathf.CeilToInt(rules.coverTolerance / metersPerPixel.x);
        int coverY = Mathf.CeilToInt(rules.coverTolerance / metersPerPixel.y);

        // Mess is weighed by how far the ink is from the lines, past a tolerance that grows with the needle that put it there.
        float[] distance = lineDistance ?? DistanceToMask(line, w, h, metersPerPixel);
        bool[] nearInk = Dilate(ink, w, h, coverX, coverY);
        float falloff = Mathf.Max(rules.messFalloff, 0.0001f);
        int covered = 0;
        float mess = 0f;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] && nearInk[i])
                covered++;
            if (ink[i] && !line[i])
            {
                float needleRadius = needle != null ? needle[i] / 255f * TattooCanvas.MaxNeedleDiameter * 0.5f : 0f;
                float tolerance = rules.messTolerance + rules.needleSlack * needleRadius;
                mess += Mathf.Clamp01((distance[i] - tolerance) / falloff);
            }
        }

        result.accuracy = covered / (float)lineCount;
        result.messRatio = mess / (float)lineCount;
        float messFactor = 1f - Mathf.Pow(Mathf.Clamp01(result.messRatio / Mathf.Max(rules.messLimit, 0.0001f)), rules.messPower);
        result.quality = Mathf.RoundToInt(100f * Mathf.Clamp01(result.accuracy * messFactor));
        result.messPenalty = Mathf.Max(0, Mathf.RoundToInt(100f * result.accuracy) - result.quality);
        return result;
    }

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

    // Distance (m) from every pixel to the nearest true pixel of the mask: two-pass chamfer (8 neighbours),
    // within a few % of the true distance.
    public static float[] DistanceToMask(bool[] mask, int w, int h, Vector2 metersPerPixel)
    {
        float dx = metersPerPixel.x, dy = metersPerPixel.y, dd = Mathf.Sqrt(dx * dx + dy * dy);
        var d = new float[mask.Length];
        for (int i = 0; i < d.Length; i++)
            d[i] = mask[i] ? 0f : float.MaxValue;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                float v = d[i];
                if (x > 0) v = Mathf.Min(v, d[i - 1] + dx);
                if (y > 0)
                {
                    v = Mathf.Min(v, d[i - w] + dy);
                    if (x > 0) v = Mathf.Min(v, d[i - w - 1] + dd);
                    if (x < w - 1) v = Mathf.Min(v, d[i - w + 1] + dd);
                }
                d[i] = v;
            }
        }
        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = w - 1; x >= 0; x--)
            {
                int i = y * w + x;
                float v = d[i];
                if (x < w - 1) v = Mathf.Min(v, d[i + 1] + dx);
                if (y < h - 1)
                {
                    v = Mathf.Min(v, d[i + w] + dy);
                    if (x < w - 1) v = Mathf.Min(v, d[i + w + 1] + dd);
                    if (x > 0) v = Mathf.Min(v, d[i + w - 1] + dd);
                }
                d[i] = v;
            }
        }
        return d;
    }

    // Rectangular dilation (separable running count).
    public static bool[] Dilate(bool[] src, int w, int h, int rx, int ry)
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
