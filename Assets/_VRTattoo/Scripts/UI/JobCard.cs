using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

// The one job card beside the bed (TattooJobManager drives it):
//   waiting   - "Waiting for a customer" + total
//   tattooing - design name + coloured difficulty tag + DONE button + total
//   result    - big quality number counting up with a bounce, 1-3 stars, "+points", total counting up
// No formula is shown to the player (accuracy / mess / multiplier only go to the console).
public class JobCard : MonoBehaviour
{
    [Header("Parts")]
    public NeonPanel panel;
    public TMP_Text title;
    [Tooltip("\"Waiting for a customer\" line")]
    public TMP_Text status;
    public NeonPanel difficultyTag;
    public TMP_Text difficultyLabel;
    public NeonButton doneButton;
    public TMP_Text score;
    public SpriteRenderer[] stars;
    public TMP_Text points;
    public TMP_Text total;

    [Header("Stars")]
    [Tooltip("Quality needed for 2 and 3 stars (1 star below the first)")]
    public Vector2Int starThresholds = new Vector2Int(70, 90);
    public Color starOn = new Color(1f, 0.82f, 0.3f, 1f);
    public Color starOff = new Color(1f, 1f, 1f, 0.18f);

    [Header("Animation")]
    public float countUpSeconds = 0.8f;
    public float popScale = 1.18f;
    public float popSeconds = 0.22f;
    public float starInterval = 0.12f;
    [Tooltip("Width of one star (m)")]
    public float starSize = 0.05f;

    [Header("Difficulty colours")]
    public Color easyColor = new Color(0.24f, 0.79f, 0.29f);
    public Color mediumColor = new Color(1f, 0.6f, 0.12f);
    public Color hardColor = new Color(1f, 0.23f, 0.23f);

    int shownTotal;
    Coroutine resultRoutine;

    void Awake() => ShowWaiting(0);

    public void ShowWaiting(int totalScore)
    {
        StopResult();
        SetJobParts(false);
        SetResultParts(false);
        status.gameObject.SetActive(true);
        SetTotal(totalScore);
    }

    public void ShowJob(StencilEntry job, int totalScore)
    {
        StopResult();
        SetResultParts(false);
        SetJobParts(true);
        status.gameObject.SetActive(false);
        title.text = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(job.displayName);

        Color c = DifficultyColor(job.difficulty);
        difficultyLabel.text = job.difficulty.ToString().ToUpperInvariant();
        difficultyLabel.color = Color.Lerp(c, Color.white, 0.25f);
        difficultyTag.edgeColor = c * 1.1f;
        difficultyTag.fillColor = new Color(c.r * 0.15f, c.g * 0.15f, c.b * 0.15f, 0.85f);
        SetTotal(totalScore);
    }

    // The DONE button only shows while the customer is lying down and the job can be submitted.
    public void SetDoneAvailable(bool available)
    {
        if (doneButton != null && doneButton.gameObject.activeSelf != available)
            doneButton.gameObject.SetActive(available);
    }

    public void ShowResult(int quality, int earned, int totalScore)
    {
        StopResult();
        SetJobParts(false);
        SetResultParts(true);
        status.gameObject.SetActive(false);
        resultRoutine = StartCoroutine(ResultRoutine(Mathf.Clamp(quality, 0, 100), earned, totalScore));
    }

    public int Stars(int quality) => quality >= starThresholds.y ? 3 : quality >= starThresholds.x ? 2 : 1;

    IEnumerator ResultRoutine(int quality, int earned, int totalScore)
    {
        foreach (var s in stars)
        {
            s.color = starOff;
            s.transform.localScale = Vector3.one * starSize;
        }
        points.text = "";
        score.text = "0";

        // Fast count-up (ease-out), then a small bounce.
        for (float t = 0f; t < 1f; t += Time.deltaTime / countUpSeconds)
        {
            float e = 1f - (1f - t) * (1f - t);
            score.text = Mathf.RoundToInt(quality * e).ToString();
            yield return null;
        }
        score.text = quality.ToString();
        yield return Pop(score.transform, 1f);

        int count = Stars(quality);
        for (int i = 0; i < stars.Length && i < count; i++)
        {
            stars[i].color = starOn;
            StartCoroutine(Pop(stars[i].transform, starSize));
            yield return new WaitForSeconds(starInterval);
        }

        points.text = $"+{earned}";
        StartCoroutine(Pop(points.transform, 1f));

        int from = shownTotal;
        for (float t = 0f; t < 1f; t += Time.deltaTime / (countUpSeconds * 0.6f))
        {
            SetTotal(Mathf.RoundToInt(Mathf.Lerp(from, totalScore, t)));
            yield return null;
        }
        SetTotal(totalScore);
        resultRoutine = null;
    }

    IEnumerator Pop(Transform target, float restScale)
    {
        Vector3 rest = Vector3.one * restScale;
        for (float t = 0f; t < 1f; t += Time.deltaTime / popSeconds)
        {
            target.localScale = rest * (1f + (popScale - 1f) * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        target.localScale = rest;
    }

    void SetJobParts(bool on)
    {
        title.gameObject.SetActive(on);
        difficultyTag.gameObject.SetActive(on);
        difficultyLabel.gameObject.SetActive(on);
        if (!on)
            SetDoneAvailable(false);
    }

    void SetResultParts(bool on)
    {
        score.gameObject.SetActive(on);
        points.gameObject.SetActive(on);
        foreach (var s in stars)
            s.gameObject.SetActive(on);
    }

    void SetTotal(int value)
    {
        shownTotal = value;
        total.text = $"TOTAL  {value:N0}";
    }

    void StopResult()
    {
        if (resultRoutine != null)
            StopCoroutine(resultRoutine);
        resultRoutine = null;
    }

    Color DifficultyColor(StencilDifficulty difficulty) => difficulty switch
    {
        StencilDifficulty.Medium => mediumColor,
        StencilDifficulty.Hard => hardColor,
        _ => easyColor,
    };
}
