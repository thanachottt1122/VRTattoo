using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// Menu VRTattoo/Simulate Tattoo Scores: scores made-up ink for every design in the library, so the
// scoring rules (TattooJobManager.scoring in the open scene) can be tuned without playing.
// The canvas and stencil placement match CustomerTattooSkin (20 cm canvas, 14 cm design, 1024 px).
public static class TattooScoreSimulator
{
    const int Size = 1024;
    const float CanvasSize = 0.20f;
    const float StencilSize = 0.14f;
    const int Seed = 12345;
    const float NeedleDiameter = 0.005f;  // CustomerTattooSkin.brushDiameter

    enum Case { Clean, Shaky, SmearDesign, SmearOutside }

    static readonly string[] CaseNames = { "Clean lines", "Shaky hand", "Smear over design", "Smear outside" };

    [MenuItem("VRTattoo/Simulate Tattoo Scores")]
    static void Menu() => Debug.Log(Run());

    public static string Run()
    {
        var library = AssetDatabase.LoadAssetAtPath<StencilLibrary>("Assets/_VRTattoo/Data/StencilLibrary.asset");
        if (library == null)
            return "[TattooScoreSimulator] StencilLibrary.asset not found";

        var manager = Object.FindFirstObjectByType<TattooJobManager>();
        var rules = manager != null ? manager.scoring : TattooScoringRules.Default;

        float k = CanvasSize / StencilSize;
        var placement = new StencilPlacement
        {
            centerUV = new Vector2(0.5f, 0.5f),
            axisX = new Vector2(k, 0f),
            axisY = new Vector2(0f, -k),
            uvBounds = new Rect(0.5f - 0.5f / k, 0.5f - 0.5f / k, 1f / k, 1f / k),
        };
        float metersPerPixel = CanvasSize / Size;
        Vector2 mpp = new Vector2(metersPerPixel, metersPerPixel);

        // results[difficulty][case] = list of scores
        var results = new Dictionary<StencilDifficulty, List<TattooScore>[]>();
        var report = new StringBuilder();
        report.AppendLine($"[TattooScoreSimulator] rules: cover {rules.coverTolerance * 1000f:0.#} mm, mess tolerance {rules.messTolerance * 1000f:0.#} mm, " +
                          $"messLimit {rules.messLimit:0.##}, messPower {rules.messPower:0.##}");

        foreach (var entry in library.stencils)
        {
            if (entry == null || entry.texture == null)
                continue;

            bool[] line = TattooScorer.StencilMask(entry.texture, placement, Size);
            if (!results.TryGetValue(entry.difficulty, out var perCase))
            {
                perCase = new List<TattooScore>[4];
                for (int c = 0; c < 4; c++)
                    perCase[c] = new List<TattooScore>();
                results[entry.difficulty] = perCase;
            }

            report.Append($"  {entry.difficulty,-6} {entry.displayName,-20}");
            for (int c = 0; c < 4; c++)
            {
                bool[] ink = MakeInk((Case)c, line, placement, metersPerPixel);
                var score = TattooScorer.ScoreMasks(ink, line, Size, Size, mpp, rules);
                perCase[c].Add(score);
                report.Append($" | {score.quality,3} (acc {score.accuracy:P0}, mess {score.messRatio:0.00}x)");
            }
            report.AppendLine();
        }

        var table = new StringBuilder();
        table.AppendLine("Quality min / avg / max");
        table.AppendLine($"{"",-8}| {CaseNames[0],-17}| {CaseNames[1],-17}| {CaseNames[2],-17}| {CaseNames[3],-17}");
        foreach (StencilDifficulty difficulty in System.Enum.GetValues(typeof(StencilDifficulty)))
        {
            if (!results.TryGetValue(difficulty, out var perCase))
                continue;
            table.Append($"{difficulty,-8}");
            for (int c = 0; c < 4; c++)
            {
                int min = int.MaxValue, max = int.MinValue;
                float sum = 0f;
                foreach (var s in perCase[c])
                {
                    min = Mathf.Min(min, s.quality);
                    max = Mathf.Max(max, s.quality);
                    sum += s.quality;
                }
                table.Append($"| {min,3} / {sum / perCase[c].Count,5:0.0} / {max,3}  ");
            }
            table.AppendLine();
        }

        return report + "\n" + table;
    }

    static bool[] MakeInk(Case c, bool[] line, StencilPlacement placement, float metersPerPixel)
    {
        var random = new System.Random(Seed);

        switch (c)
        {
            case Case.Clean:
                // A 5 mm needle run along the lines with a steady hand (±0.5 mm), a few short gaps.
                return Trace(line, metersPerPixel, 0.0005f, 0.02f, random);
            case Case.Shaky:
                // The same, but the hand wobbles up to ±3 mm and skips more.
                return Trace(line, metersPerPixel, 0.003f, 0.10f, random);
            case Case.SmearDesign:
            {
                // Ink rubbed over the whole design square.
                var ink = new bool[line.Length];
                Rect b = placement.uvBounds;
                for (int y = Mathf.FloorToInt(b.yMin * Size); y < Mathf.CeilToInt(b.yMax * Size); y++)
                    for (int x = Mathf.FloorToInt(b.xMin * Size); x < Mathf.CeilToInt(b.xMax * Size); x++)
                        ink[y * Size + x] = true;
                return ink;
            }
            default:
            {
                // As much ink as the design has, but in the margin beside the design square.
                int lineCount = 0;
                foreach (bool l in line)
                    if (l) lineCount++;
                var ink = new bool[line.Length];
                int width = Mathf.Max(1, Mathf.FloorToInt(placement.uvBounds.xMin * Size) - 4);
                int rows = Mathf.Min(Size, Mathf.CeilToInt(lineCount / (float)width));
                for (int y = 0; y < rows; y++)
                    for (int x = 0; x < width; x++)
                        ink[y * Size + x] = true;
                return ink;
            }
        }
    }

    // Ink from a needle of NeedleDiameter run along the design lines: each line widened to the needle's
    // width (thin lines get overflow, wide areas none), shifted by smooth noise of up to ±jitter,
    // with gapChance of 5 mm patches left out.
    static bool[] Trace(bool[] line, float metersPerPixel, float jitter, float gapChance, System.Random random)
    {
        int area = 0, edge = 0;
        for (int y = 1; y < Size - 1; y++)
        {
            for (int x = 1; x < Size - 1; x++)
            {
                int i = y * Size + x;
                if (!line[i])
                    continue;
                area++;
                if (!line[i - 1] || !line[i + 1] || !line[i - Size] || !line[i + Size])
                    edge++;
            }
        }
        // Half the typical line width: area ÷ outline length for strokes.
        float halfWidth = edge > 0 ? area / (float)edge * metersPerPixel : 0f;
        int overflow = Mathf.RoundToInt(Mathf.Max(0f, NeedleDiameter * 0.5f - halfWidth) / metersPerPixel);
        bool[] traced = TattooScorer.Dilate(line, Size, Size, overflow, overflow);

        var ink = new bool[traced.Length];
        float amplitude = jitter / metersPerPixel;
        float frequency = metersPerPixel / 0.015f;  // one wobble per ~1.5 cm
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = Wobble(x * frequency, y * frequency) * amplitude;
                float dy = Wobble(x * frequency + 37.1f, y * frequency + 11.7f) * amplitude;
                int sx = Mathf.Clamp(Mathf.RoundToInt(x + dx), 0, Size - 1);
                int sy = Mathf.Clamp(Mathf.RoundToInt(y + dy), 0, Size - 1);
                ink[y * Size + x] = traced[sy * Size + sx];
            }
        }
        DropCells(ink, Mathf.RoundToInt(0.005f / metersPerPixel), gapChance, random);
        return ink;
    }

    // Smooth noise stretched to use the whole −1..1 range (Perlin noise rarely leaves 0.2..0.8).
    static float Wobble(float x, float y) => Mathf.Clamp((Mathf.PerlinNoise(x, y) - 0.5f) / 0.3f, -1f, 1f);

    // Removes ink in cell × cell squares with the given chance (gaps where the needle skipped).
    static void DropCells(bool[] ink, int cell, float chance, System.Random random)
    {
        cell = Mathf.Max(1, cell);
        for (int cy = 0; cy < Size; cy += cell)
            for (int cx = 0; cx < Size; cx += cell)
                if (random.NextDouble() < chance)
                    for (int y = cy; y < Mathf.Min(cy + cell, Size); y++)
                        for (int x = cx; x < Mathf.Min(cx + cell, Size); x++)
                            ink[y * Size + x] = false;
    }
}
