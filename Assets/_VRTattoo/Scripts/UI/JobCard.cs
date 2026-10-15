using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// The one job card beside the bed (TattooJobManager drives it):
//   waiting    - "Waiting for a customer" + total
//   tattooing  - design name + coloured difficulty tag + time-left bar + pain face/gauge + DONE + total
//   result     - big quality number counting up with a bounce, 1-3 stars, "+points" (+ "FAST +x%"),
//                total counting up; 3 stars sets off a particle burst around the card
//   walked out - "0" + "WALKED OUT" when the pain reached 100 %
// The waiting customers' patience is shown as tiny bars beside the total in every state.
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

    [Header("Time and pain (tattooing)")]
    public NeonBar timeBar;
    public NeonBar painBar;
    public SpriteRenderer painFace;
    public TMP_Text painLabel;
    public Sprite faceCalm;
    public Sprite faceTense;
    public Sprite faceHurt;
    [Tooltip("Pain (%) from which the face looks tense; it looks hurt from the customer's flinch threshold")]
    public float tenseFrom = 30f;
    [Tooltip("Width of the pain face (m)")]
    public float faceSize = 0.034f;
    public Color painLow = new Color(0.3f, 0.95f, 0.45f);
    public Color painMid = new Color(1f, 0.85f, 0.2f);
    public Color painHigh = new Color(1f, 0.2f, 0.25f);
    public Color timeOk = new Color(0.85f, 0.9f, 1f);
    public Color timeLow = new Color(1f, 0.55f, 0.15f);
    public Color timeOver = new Color(1f, 0.2f, 0.25f);
    [Tooltip("The time bar turns orange below this fraction")]
    [Range(0f, 1f)] public float timeLowFraction = 0.25f;

    [Header("Queue")]
    [Tooltip("One tiny patience bar per waiting seat")]
    public NeonBar[] queueBars;
    public TMP_Text queueLabel;

    [Header("Result extras")]
    [Tooltip("FAST +x% under the stars")]
    public TMP_Text bonus;
    [Tooltip("WALKED OUT when the customer left in pain")]
    public TMP_Text walkout;

    [Header("3 stars")]
    [Tooltip("Played once when the third star lights up")]
    public ParticleSystem threeStarBurst;
    public UnityEvent onThreeStars;

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
        SetPain(0f, 100f, false);
        SetTimer(1f, false);
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

    public void ShowResult(int quality, int earned, int totalScore, float bonusFraction = 0f)
    {
        StopResult();
        SetJobParts(false);
        SetResultParts(true);
        status.gameObject.SetActive(false);
        resultRoutine = StartCoroutine(ResultRoutine(Mathf.Clamp(quality, 0, 100), earned, totalScore, bonusFraction));
    }

    // The customer could not take the pain any more: 0, no stars.
    public void ShowWalkout(int totalScore)
    {
        StopResult();
        SetJobParts(false);
        SetResultParts(true);
        status.gameObject.SetActive(false);
        foreach (var s in stars)
            s.gameObject.SetActive(false);
        points.text = "";
        score.text = "0";
        score.transform.localScale = Vector3.one;
        if (walkout != null)
        {
            walkout.gameObject.SetActive(true);
            resultRoutine = StartCoroutine(Pop(walkout.transform, 1f));
        }
        SetTotal(totalScore);
    }

    // Pain 0-100. The face is calm, tense from tenseFrom, hurt from hurtFrom (the flinch threshold);
    // `flinching` makes it jump.
    public void SetPain(float pain, float hurtFrom, bool flinching)
    {
        if (painBar != null)
        {
            float t = pain / 100f;
            Color c = t < 0.5f ? Color.Lerp(painLow, painMid, t * 2f) : Color.Lerp(painMid, painHigh, t * 2f - 1f);
            painBar.Set(t, c);
        }
        if (painLabel != null)
            painLabel.text = $"{Mathf.RoundToInt(pain)}%";
        if (painFace != null)
        {
            painFace.sprite = pain >= hurtFrom ? faceHurt : pain >= tenseFrom ? faceTense : faceCalm;
            float spriteWidth = painFace.sprite != null ? painFace.sprite.bounds.size.x : 1f;
            float scale = faceSize / Mathf.Max(spriteWidth, 0.0001f) * (flinching ? 1.25f : 1f);
            painFace.transform.localScale = Vector3.one * scale;
        }
    }

    // Fraction of the customer's time left; in overtime the empty bar blinks red.
    public void SetTimer(float fraction, bool overtime)
    {
        if (timeBar == null)
            return;
        if (overtime)
            timeBar.Set(Mathf.Repeat(Time.time, 1f) < 0.5f ? 0.04f : 0f, timeOver);
        else
            timeBar.Set(fraction, fraction < timeLowFraction ? timeLow : timeOk);
    }

    // Patience left of the customer on a waiting seat (negative = nobody waiting there).
    public void SetQueue(int seat, float fraction)
    {
        if (queueBars == null || seat >= queueBars.Length || queueBars[seat] == null)
            return;
        bool show = fraction >= 0f;
        SetActive(queueBars[seat], show);
        if (show)
            queueBars[seat].Set(fraction, fraction < timeLowFraction ? timeLow : timeOk);

        if (queueLabel != null)
        {
            bool any = false;
            foreach (var bar in queueBars)
                any |= bar != null && bar.gameObject.activeSelf;
            SetActive(queueLabel, any);
        }
    }

    public int Stars(int quality) => quality >= starThresholds.y ? 3 : quality >= starThresholds.x ? 2 : 1;

    IEnumerator ResultRoutine(int quality, int earned, int totalScore, float bonusFraction)
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
            if (i == 2)
                PlayThreeStars();
            yield return new WaitForSeconds(starInterval);
        }

        points.text = $"+{earned}";
        StartCoroutine(Pop(points.transform, 1f));
        if (bonus != null && bonusFraction > 0.005f)
        {
            bonus.text = $"FAST +{Mathf.RoundToInt(bonusFraction * 100f)}%";
            bonus.gameObject.SetActive(true);
            StartCoroutine(Pop(bonus.transform, 1f));
        }

        int from = shownTotal;
        for (float t = 0f; t < 1f; t += Time.deltaTime / (countUpSeconds * 0.6f))
        {
            SetTotal(Mathf.RoundToInt(Mathf.Lerp(from, totalScore, t)));
            yield return null;
        }
        SetTotal(totalScore);
        resultRoutine = null;
    }

    void PlayThreeStars()
    {
        if (threeStarBurst != null)
        {
            threeStarBurst.Clear(true);
            threeStarBurst.Play(true);
        }
        onThreeStars?.Invoke();
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
        SetActive(timeBar, on);
        SetActive(painBar, on);
        SetActive(painFace, on);
        SetActive(painLabel, on);
        if (!on)
            SetDoneAvailable(false);
    }

    void SetResultParts(bool on)
    {
        score.gameObject.SetActive(on);
        points.gameObject.SetActive(on);
        foreach (var s in stars)
            s.gameObject.SetActive(on);
        // Shown by ShowResult / ShowWalkout when they apply.
        SetActive(bonus, false);
        SetActive(walkout, false);
    }

    static void SetActive(Component part, bool on)
    {
        if (part != null && part.gameObject.activeSelf != on)
            part.gameObject.SetActive(on);
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
