using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu VRTattoo/Simulate Tattoo Scores: scores made-up ink for every design in the library with every
// needle size, so the scoring rules (TattooJobManager.scoring in the open scene) can be tuned without playing.
// The canvas and stencil placement match CustomerTattooSkin (20 cm canvas, 14 cm design, 1024 px).
// The cases are a first-time player (mouse or headset) taking more or less care, each with the stars it
// should get (JobCard.starThresholds in the scene). A last table re-scores real jobs from a play log with
// the current rules and bed times.
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

    enum Case { Careful, Fair, Sloppy, Smear, SmearOutside }

    static readonly string[] CaseNames = { "Careful", "Fair", "Sloppy", "Smear", "Smear outside" };
    static readonly string[] CaseTargets = { "3* 80+", "2* 55-79", "1* <55", "1* <20", "1* 0" };
    static readonly int[] CaseStars = { 3, 2, 1, 1, 1 };
    const int CaseCount = 5;

    // How the hand moves in the traced cases: wobble (±m), share of 5 mm patches skipped, stray strokes off
    // the lines (× the design's area). Public so they can be tried out without recompiling.
    public static (float jitter, float gaps, float stray)[] TracedCases =
    {
        (0.0015f, 0.30f, 0.10f),  // Careful
        (0.003f, 0.60f, 0.40f),   // Fair: the old rules measure about the typical real job
        (0.006f, 0.80f, 1.0f),    // Sloppy: about the messiest real job that was not a smear
    };

    // For trying cases from outside (execute_code cannot build these tuples itself).
    public static void SetTracedCase(int index, float jitter, float gaps, float stray) => TracedCases[index] = (jitter, gaps, stray);

    // The rules the real jobs below were played with; the cases are calibrated so that these rules measure
    // about the accuracy and mess of the real jobs (Fair = a typical real job, Sloppy = the messy ones).
    static readonly TattooScoringRules OldRules = new TattooScoringRules
    {
        coverTolerance = 0.0015f, messTolerance = 0f, needleSlack = 1.1f, messFalloff = 0.0032f, messLimit = 1f, messPower = 0.58f,
    };

    // Real jobs from a play log (first plays with the old rules, 70/90 stars): accuracy, mess ratio and the
    // time used. Accuracy and mess are what the old tolerances measured, so the new scores here are a floor
    // (wider tolerances also raise the accuracy and lower the mess).
    static readonly (string name, StencilDifficulty difficulty, float accuracy, float mess, float elapsed, int oldQuality)[] RealJobs =
    {
        ("crown skull",    StencilDifficulty.Easy,   0.74f, 0.12f, 67f,  45),
        ("karate kick",    StencilDifficulty.Easy,   0.90f, 0.20f, 71f,  44),
        ("royal crown",    StencilDifficulty.Hard,   0.73f, 0.02f, 40f,  66),
        ("crown skull",    StencilDifficulty.Easy,   0.57f, 0.00f, 45f,  55),
        ("cloud",          StencilDifficulty.Easy,   0.67f, 0.14f, 28f,  46),
        ("karate kick",    StencilDifficulty.Easy,   0.82f, 0.15f, 28f,  55),
        ("cloud",          StencilDifficulty.Easy,   0.74f, 1.70f, 190f, 0),
        ("crown skull",    StencilDifficulty.Easy,   0.70f, 0.18f, 35f,  44),
        ("cloud",          StencilDifficulty.Easy,   0.61f, 0.11f, 21f,  44),
        ("biohazard",      StencilDifficulty.Medium, 0.74f, 0.39f, 20f,  31),
        ("melting smiley", StencilDifficulty.Easy,   0.91f, 2.90f, 20f,  0),
    };

    [MenuItem("VRTattoo/Simulate Tattoo Scores")]
    static void Menu() => Debug.Log(Run());

    // Batch mode: -executeMethod TattooScoreSimulator.RunBatch (opens the main scene for its rules).
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/_VRTattoo/Scenes/TattooRoom.unity");
        Debug.Log(Run());
    }

    public static string Run() => Run(null);

    // rulesOverride: score with these rules instead of the scene's (for tuning).
    public static string Run(TattooScoringRules? rulesOverride)
    {
        var library = AssetDatabase.LoadAssetAtPath<StencilLibrary>("Assets/_VRTattoo/Data/StencilLibrary.asset");
        if (library == null)
            return "[TattooScoreSimulator] StencilLibrary.asset not found";

        var manager = Object.FindFirstObjectByType<TattooJobManager>();
        var card = Object.FindFirstObjectByType<JobCard>();
        var rules = rulesOverride ?? (manager != null ? manager.scoring : TattooScoringRules.Default);
        var patience = manager != null ? manager.patience : PatienceScoring.Default;
        Vector2Int starThresholds = card != null ? card.starThresholds : new Vector2Int(55, 80);
        int Stars(int quality) => quality >= starThresholds.y ? 3 : quality >= starThresholds.x ? 2 : 1;

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

        // results[(difficulty, needle)][case] = scores; fitting[difficulty][case] = scores with the needle that suits each design
        var results = new Dictionary<(StencilDifficulty, int), List<int>[]>();
        var fitting = new Dictionary<StencilDifficulty, List<int>[]>();
        // Calibration with the fitting needle: summed accuracy and mess per case, old and current rules.
        var oldAccuracy = new float[CaseCount]; var oldMess = new float[CaseCount];
        var newAccuracy = new float[CaseCount]; var newMess = new float[CaseCount];
        int designs = 0;

        var report = new StringBuilder();
        report.AppendLine($"[TattooScoreSimulator] rules: cover {rules.coverTolerance * 1000f:0.#} mm, mess tolerance {rules.messTolerance * 1000f:0.#} mm " +
                          $"+ {rules.needleSlack:0.##} x needle radius, falloff {rules.messFalloff * 1000f:0.#} mm, messLimit {rules.messLimit:0.##}, " +
                          $"messPower {rules.messPower:0.##}; 2 stars from {starThresholds.x}, 3 from {starThresholds.y}");
        report.AppendLine($"  per design (quality LINER MEDIUM SHADER, * = the fitting needle): {string.Join(" | ", CaseNames)}");

        foreach (var entry in library.stencils)
        {
            if (entry == null || entry.texture == null)
                continue;

            bool[] line = TattooScorer.StencilMask(entry.texture, placement, Size);
            float halfWidth = HalfWidth(line, metersPerPixel);
            float[] lineDistance = TattooScorer.DistanceToMask(line, Size, Size, mpp);
            int fit = FittingNeedle(halfWidth);
            report.Append($"  {entry.difficulty,-6} {entry.displayName,-18} line {halfWidth * 2000f,4:0.0}mm{(halfWidth >= SolidHalfWidth ? " solid" : "      ")}");

            if (!fitting.TryGetValue(entry.difficulty, out var fitCases))
                fitting[entry.difficulty] = fitCases = NewCases();
            designs++;

            for (int c = 0; c < CaseCount; c++)
            {
                report.Append(" |");
                for (int n = 0; n < NeedleNames.Length; n++)
                {
                    if (!results.TryGetValue((entry.difficulty, n), out var perCase))
                        results[(entry.difficulty, n)] = perCase = NewCases();

                    float diameter = NeedleDiameters[n];
                    bool[] ink = MakeInk((Case)c, line, placement, metersPerPixel, diameter, halfWidth);
                    var needle = new byte[ink.Length];
                    byte value = TattooScorer.NeedleValue(diameter);
                    for (int i = 0; i < ink.Length; i++)
                        if (ink[i])
                            needle[i] = value;

                    var score = TattooScorer.ScoreMasks(ink, line, Size, Size, mpp, rules, needle, lineDistance);
                    perCase[c].Add(score.quality);
                    if (n == fit)
                    {
                        fitCases[c].Add(score.quality);
                        var old = TattooScorer.ScoreMasks(ink, line, Size, Size, mpp, OldRules, needle, lineDistance);
                        oldAccuracy[c] += old.accuracy;
                        oldMess[c] += old.messRatio;
                        newAccuracy[c] += score.accuracy;
                        newMess[c] += score.messRatio;
                    }
                    report.Append($" {score.quality,3}{(n == fit ? "*" : " ")}");
                }
            }
            report.AppendLine();
        }

        // Calibration: what the old rules measure on each case, next to the real jobs.
        var table = new StringBuilder();
        float realAccuracy = 0f, realMess = 0f;
        int realCount = 0;
        foreach (var job in RealJobs)
        {
            if (job.mess > 1f)
                continue;  // the two smears
            realAccuracy += job.accuracy;
            realMess += job.mess;
            realCount++;
        }
        table.AppendLine($"Calibration (fitting needle, average over designs): real jobs measured accuracy {realAccuracy / realCount:P0}, mess {realMess / realCount:0.00} (without the 2 smears)");
        for (int c = 0; c < CaseCount; c++)
            table.AppendLine($"  {CaseNames[c],-14} old rules: accuracy {oldAccuracy[c] / designs:P0}, mess {oldMess[c] / designs:0.00} | " +
                             $"current rules: accuracy {newAccuracy[c] / designs:P0}, mess {newMess[c] / designs:0.00}");
        table.AppendLine();

        // Main table: the needle that suits each design, with stars.
        table.AppendLine("Quality min / avg / max [stars min-max] with the fitting needle; OK = every design gets the target stars");
        table.Append($"{"",-8}");
        for (int c = 0; c < CaseCount; c++)
            table.Append($"| {CaseNames[c] + " (" + CaseTargets[c] + ")",-30}");
        table.AppendLine();
        foreach (StencilDifficulty difficulty in System.Enum.GetValues(typeof(StencilDifficulty)))
        {
            if (!fitting.TryGetValue(difficulty, out var perCase))
                continue;
            table.Append($"{difficulty,-8}");
            for (int c = 0; c < CaseCount; c++)
                table.Append($"| {Summary(perCase[c], Stars, CaseStars[c]),-30}");
            table.AppendLine();
        }

        table.AppendLine();
        table.AppendLine("Every needle, quality min / avg / max");
        table.Append($"{"",-15}");
        for (int c = 0; c < CaseCount; c++)
            table.Append($"| {CaseNames[c],-20}");
        table.AppendLine();
        foreach (StencilDifficulty difficulty in System.Enum.GetValues(typeof(StencilDifficulty)))
        {
            for (int n = 0; n < NeedleNames.Length; n++)
            {
                if (!results.TryGetValue((difficulty, n), out var perCase))
                    continue;
                table.Append($"{difficulty + " " + NeedleNames[n],-15}");
                for (int c = 0; c < CaseCount; c++)
                    table.Append($"| {Summary(perCase[c], null, 0),-20}");
                table.AppendLine();
            }
        }

        // Real jobs again: same accuracy and mess, the current mess formula and bed times.
        table.AppendLine();
        table.AppendLine($"Real jobs re-scored (bed time easy/medium/hard {patience.easySeconds:0}/{patience.mediumSeconds:0}/{patience.hardSeconds:0}s, " +
                         $"overtime -{patience.overtimePenaltyPerSecond:0.##}/s, at most -{patience.maxOvertimePenalty})");
        table.AppendLine($"  {"design",-15} {"acc",4} {"mess",5} {"time",9} | old quality stars | new quality (time) stars");
        foreach (var job in RealJobs)
        {
            float messFactor = 1f - Mathf.Pow(Mathf.Clamp01(job.mess / Mathf.Max(rules.messLimit, 0.0001f)), rules.messPower);
            int scored = Mathf.RoundToInt(100f * Mathf.Clamp01(job.accuracy * messFactor));
            float duration = patience.Seconds(job.difficulty);
            int overtime = patience.Penalty(job.elapsed, duration);
            int quality = Mathf.Max(0, scored - overtime);
            table.AppendLine($"  {job.name,-15} {job.accuracy * 100f,3:0}% {job.mess,5:0.00} {job.elapsed,4:0}/{duration,3:0}s " +
                             $"| {job.oldQuality,3}         1     | {quality,3}     ({-overtime,3})  {Stars(quality)}");
        }

        return report + "\n" + table;
    }

    static List<int>[] NewCases()
    {
        var cases = new List<int>[CaseCount];
        for (int i = 0; i < CaseCount; i++)
            cases[i] = new List<int>();
        return cases;
    }

    // The thinnest needle about as wide as the line (solid designs end up with the shader).
    static int FittingNeedle(float halfWidth)
    {
        for (int n = 0; n < NeedleDiameters.Length; n++)
            if (NeedleDiameters[n] >= halfWidth * 2f * 0.8f)
                return n;
        return NeedleDiameters.Length - 1;
    }

    // stars (optional): adds the star range and OK / NO against targetStars.
    static string Summary(List<int> scores, System.Func<int, int> stars, int targetStars)
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
        string text = $"{min,3} / {sum / scores.Count,5:0.0} / {max,3}";
        if (stars != null)
        {
            int low = stars(min), high = stars(max);
            text += $" [{low}-{high}] {(low == targetStars && high == targetStars ? "OK" : "NO")}";
        }
        return text;
    }

    static bool[] MakeInk(Case c, bool[] line, StencilPlacement placement, float metersPerPixel, float needle, float halfWidth)
    {
        var random = new System.Random(Seed);

        switch (c)
        {
            case Case.Careful:
            case Case.Fair:
            case Case.Sloppy:
            {
                // Careful: close to the lines, few skips and slips. Fair: follows the lines roughly. Sloppy: rushed,
                // big wobbles, many skips and strokes off the design.
                var hand = TracedCases[(int)c];
                bool[] ink = Trace(line, metersPerPixel, needle, halfWidth, hand.jitter, hand.gaps, random);
                AddStrayStrokes(ink, line, metersPerPixel, needle, hand.stray, random);
                return ink;
            }
            case Case.Smear:
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
