using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// Menu VRTattoo/Simulate Tattoo Scores: scores made-up ink for every design in the library with every
// needle size, so the scoring rules (TattooJobManager.scoring in the open scene) can be tuned without playing.
// The canvas and stencil placement match CustomerTattooSkin (20 cm canvas, 14 cm design, 1024 px).
public static class TattooScoreSimulator
{
    const int Size = 1024;
    const float CanvasSize = 0.20f;
    const float StencilSize = 0.14f;
    const int Seed = 12345;

    // The needle cartridges on the trolley (Prefabs/Needles).
    static readonly string[] NeedleNames = { "LINER", "MEDIUM", "SHADER" };
    static readonly float[] NeedleDiameters = { 0.003f, 0.005f, 0.009f };

    // Designs whose typical line is at least this wide are "solid" (filled areas, a job for the shader needle).
    const float SolidHalfWidth = 0.003f;

    enum Case { Clean, SlightShake, Messy, SmearDesign, SmearOutside }

    static readonly string[] CaseNames = { "Clean", "Slight shake", "Messy", "Smear design", "Smear outside" };
    static readonly string[] CaseTargets = { "90-100", "75-90", "45-65", "< 25", "0" };
    const int CaseCount = 5;

    [MenuItem("VRTattoo/Simulate Tattoo Scores")]
    static void Menu() => Debug.Log(Run());

    public static string Run() => Run(null);

    // rulesOverride: score with these rules instead of the scene's (for tuning).
    public static string Run(TattooScoringRules? rulesOverride)
    {
        var library = AssetDatabase.LoadAssetAtPath<StencilLibrary>("Assets/_VRTattoo/Data/StencilLibrary.asset");
        if (library == null)
            return "[TattooScoreSimulator] StencilLibrary.asset not found";

        var manager = Object.FindFirstObjectByType<TattooJobManager>();
        var rules = rulesOverride ?? (manager != null ? manager.scoring : TattooScoringRules.Default);

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

        // results[(difficulty, needle)][case] = scores; solid[needle, case] = scores of solid designs only
        var results = new Dictionary<(StencilDifficulty, int), List<int>[]>();
        var solid = new List<int>[NeedleNames.Length, CaseCount];
        for (int n = 0; n < NeedleNames.Length; n++)
            for (int c = 0; c < CaseCount; c++)
                solid[n, c] = new List<int>();

        var report = new StringBuilder();
        report.AppendLine($"[TattooScoreSimulator] rules: cover {rules.coverTolerance * 1000f:0.#} mm, mess tolerance {rules.messTolerance * 1000f:0.#} mm " +
                          $"+ {rules.needleSlack:0.##} x needle radius, falloff {rules.messFalloff * 1000f:0.#} mm, messLimit {rules.messLimit:0.##}, messPower {rules.messPower:0.##}");
        report.AppendLine($"  per design (quality LINER MEDIUM SHADER): {string.Join(" | ", CaseNames)}");

        foreach (var entry in library.stencils)
        {
            if (entry == null || entry.texture == null)
                continue;

            bool[] line = TattooScorer.StencilMask(entry.texture, placement, Size);
            float halfWidth = HalfWidth(line, metersPerPixel);
            float[] lineDistance = TattooScorer.DistanceToMask(line, Size, Size, mpp);
            bool isSolid = halfWidth >= SolidHalfWidth;
            report.Append($"  {entry.difficulty,-6} {entry.displayName,-18} line {halfWidth * 2000f,4:0.0}mm{(isSolid ? " solid" : "      ")}");

            for (int c = 0; c < CaseCount; c++)
            {
                report.Append(" |");
                for (int n = 0; n < NeedleNames.Length; n++)
                {
                    if (!results.TryGetValue((entry.difficulty, n), out var perCase))
                    {
                        perCase = new List<int>[CaseCount];
                        for (int i = 0; i < CaseCount; i++)
                            perCase[i] = new List<int>();
                        results[(entry.difficulty, n)] = perCase;
                    }

                    float diameter = NeedleDiameters[n];
                    bool[] ink = MakeInk((Case)c, line, placement, metersPerPixel, diameter, halfWidth);
                    var needle = new byte[ink.Length];
                    byte value = TattooScorer.NeedleValue(diameter);
                    for (int i = 0; i < ink.Length; i++)
                        if (ink[i])
                            needle[i] = value;

                    var score = TattooScorer.ScoreMasks(ink, line, Size, Size, mpp, rules, needle, lineDistance);
                    perCase[c].Add(score.quality);
                    if (isSolid)
                        solid[n, c].Add(score.quality);
                    report.Append($" {score.quality,3}");
                }
            }
            report.AppendLine();
        }

        var table = new StringBuilder();
        table.AppendLine("Quality min / avg / max");
        table.Append($"{"",-15}");
        for (int c = 0; c < CaseCount; c++)
            table.Append($"| {CaseNames[c] + " (" + CaseTargets[c] + ")",-22}");
        table.AppendLine();
        foreach (StencilDifficulty difficulty in System.Enum.GetValues(typeof(StencilDifficulty)))
        {
            for (int n = 0; n < NeedleNames.Length; n++)
            {
                if (!results.TryGetValue((difficulty, n), out var perCase))
                    continue;
                table.Append($"{difficulty + " " + NeedleNames[n],-15}");
                for (int c = 0; c < CaseCount; c++)
                    table.Append($"| {Summary(perCase[c]),-22}");
                table.AppendLine();
            }
        }
        for (int n = 0; n < NeedleNames.Length; n++)
        {
            table.Append($"{"solid " + NeedleNames[n],-15}");
            for (int c = 0; c < CaseCount; c++)
                table.Append($"| {Summary(solid[n, c]),-22}");
            table.AppendLine();
        }

        return report + "\n" + table;
    }

    static string Summary(List<int> scores)
    {
        if (scores.Count == 0)
            return "-";
        int min = int.MaxValue, max = int.MinValue;
        float sum = 0f;
        foreach (int s in scores)
        {
            min = Mathf.Min(min, s);
            max = Mathf.Max(max, s);
            sum += s;
        }
        return $"{min,3} / {sum / scores.Count,5:0.0} / {max,3}";
    }

    static bool[] MakeInk(Case c, bool[] line, StencilPlacement placement, float metersPerPixel, float needle, float halfWidth)
    {
        var random = new System.Random(Seed);

        switch (c)
        {
            case Case.Clean:
                // The needle run along the lines with a steady hand (±0.5 mm), a few short gaps.
                return Trace(line, metersPerPixel, needle, halfWidth, 0.0005f, 0.02f, random);
            case Case.SlightShake:
                // An unsteady hand: wobbles up to ±3 mm, 10% skipped.
                return Trace(line, metersPerPixel, needle, halfWidth, 0.003f, 0.10f, random);
            case Case.Messy:
            {
                // A very shaky hand (±4.5 mm, 15% skipped) plus stray strokes off the lines (slips of the needle)
                // adding 40% of the design's area.
                bool[] ink = Trace(line, metersPerPixel, needle, halfWidth, 0.0045f, 0.15f, random);
                AddStrayStrokes(ink, line, metersPerPixel, needle, 0.4f, random);
                return ink;
            }
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
                int lineCount = Count(line);
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

    static int Count(bool[] mask)
    {
        int count = 0;
        foreach (bool b in mask)
            if (b) count++;
        return count;
    }

    // Half the typical line width of a design: area ÷ outline length.
    static float HalfWidth(bool[] line, float metersPerPixel)
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
        return edge > 0 ? area / (float)edge * metersPerPixel : 0f;
    }

    // Ink from a needle run along the design lines: each line widened to the needle's width (thin lines
    // get overflow, wide areas none), shifted by smooth noise of up to ±jitter, with gapChance of 5 mm patches left out.
    static bool[] Trace(bool[] line, float metersPerPixel, float needle, float halfWidth, float jitter, float gapChance, System.Random random)
    {
        int overflow = Mathf.RoundToInt(Mathf.Max(0f, needle * 0.5f - halfWidth) / metersPerPixel);
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

    // Short straight strokes (1.5 cm, needle wide) starting on the design and heading off in a random
    // direction, until they add share × the design's area of ink.
    static void AddStrayStrokes(bool[] ink, bool[] line, float metersPerPixel, float needle, float share, System.Random random)
    {
        var linePixels = new List<int>();
        for (int i = 0; i < line.Length; i++)
            if (line[i])
                linePixels.Add(i);
        if (linePixels.Count == 0)
            return;

        int target = Mathf.RoundToInt(linePixels.Count * share);
        int radius = Mathf.Max(1, Mathf.RoundToInt(needle * 0.5f / metersPerPixel));
        int length = Mathf.RoundToInt(0.015f / metersPerPixel);
        int added = 0;
        for (int stroke = 0; stroke < 2000 && added < target; stroke++)
        {
            int start = linePixels[random.Next(linePixels.Count)];
            float angle = (float)(random.NextDouble() * Mathf.PI * 2f);
            float sx = start % Size, sy = start / Size;
            for (int s = 0; s <= length && added < target; s++)
            {
                int cx = Mathf.RoundToInt(sx + Mathf.Cos(angle) * s);
                int cy = Mathf.RoundToInt(sy + Mathf.Sin(angle) * s);
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    for (int x = cx - radius; x <= cx + radius; x++)
                    {
                        if (x < 0 || y < 0 || x >= Size || y >= Size || (x - cx) * (x - cx) + (y - cy) * (y - cy) > radius * radius)
                            continue;
                        int i = y * Size + x;
                        if (!ink[i] && !line[i])
                        {
                            ink[i] = true;
                            added++;
                        }
                    }
                }
            }
        }
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
