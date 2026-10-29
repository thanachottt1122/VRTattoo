using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// "Game Complete" after the last day's summary (ShopDayManager), in the job card's neon style, floating
// in front of the player at eye level: bank, stars, happy / angry customers over the whole game, then a
// big grade S/A/B/C by the bank. S sets off the same sparkle + confetti burst as 3 stars on the job card.
// PLAY AGAIN is a NeonButton (controller ray + trigger / grip / poke, mouse click in Desktop Test Mode).
// ShopSounds plays the game-complete jingle as the panel appears.
// The stat rows are laid out here on Show (one row per label/value pair, grade label level with the grade),
// so the panel never depends on hand-placed text positions.
public class GameCompletePanel : MonoBehaviour
{
    public TMP_Text title;
    [Tooltip("Values, in order: bank, stars, happy, angry")]
    public TMP_Text[] values;
    public TMP_Text grade;
    [Tooltip("Row labels, in the same order as values")]
    public TMP_Text[] labels;
    [Tooltip("Shown to the left of the big grade letter")]
    public TMP_Text gradeLabel;
    public NeonButton playAgainButton;
    [Tooltip("Played when the grade is S")]
    public ParticleSystem sBurst;

    public UnityEvent onPlayAgain;

    [Header("Grade (bank at the end of the game, $)")]
    [Tooltip("5 days x 5 customers: excellent play (quality ~90+, tips, fast bonus) ends near $2,000, " +
             "good play (~80) near $1,500, average (~60, some walk-outs) near $1,000")]
    public int sFrom = 1900;
    public int aFrom = 1400;
    public int bFrom = 900;
    public Color sColor = new Color(1f, 0.82f, 0.3f);
    public Color aColor = new Color(1f, 0.45f, 0.9f);
    public Color bColor = new Color(0.45f, 0.75f, 1f);
    public Color cColor = new Color(0.8f, 0.8f, 0.85f);

    [Header("Layout (panel-local, m)")]
    public float firstRowY = 0.115f;
    public float rowSpacing = 0.033f;
    public float gradeY = -0.06f;
    [Tooltip("Centre x of the grade letter, right-aligned with the values")]
    public float gradeX = 0.105f;

    [Header("Placement")]
    [Tooltip("Defaults to the main camera")]
    public Transform viewer;
    [Tooltip("Distance in front of the player (m), at eye level")]
    public float distance = 0.8f;

    [Header("Animation")]
    public float rowInterval = 0.15f;
    public float countUpSeconds = 0.6f;
    public float gradeDelay = 0.3f;
    public float popScale = 1.35f;
    public float popSeconds = 0.3f;

    // Kept inactive in the scene until Show.
    void OnEnable()
    {
        if (playAgainButton != null)
            playAgainButton.onPressed.AddListener(PressPlayAgain);
    }

    void OnDisable()
    {
        if (playAgainButton != null)
            playAgainButton.onPressed.RemoveListener(PressPlayAgain);
    }

    public string Grade(int bank) => bank >= sFrom ? "S" : bank >= aFrom ? "A" : bank >= bFrom ? "B" : "C";

    Color GradeColor(string g) => g switch
    {
        "S" => sColor,
        "A" => aColor,
        "B" => bColor,
        _ => cColor,
    };

    public void Show(int bank, int stars, int happy, int angry)
    {
        PlaceInFrontOfViewer();
        LayoutRows();
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Reveal(bank, stars, happy, angry));
        if (ShopSounds.Instance != null)
            ShopSounds.Instance.PlayGameComplete(Grade(bank));
    }

    public void Hide() => gameObject.SetActive(false);

    void PressPlayAgain() => onPlayAgain?.Invoke();

    void LayoutRows()
    {
        for (int i = 0; i < values.Length; i++)
        {
            float y = firstRowY - i * rowSpacing;
            SetY(values[i], y);
            if (labels != null && i < labels.Length)
                SetY(labels[i], y);
        }
        SetY(gradeLabel, gradeY);
        if (grade != null)
            grade.transform.localPosition = new Vector3(gradeX, gradeY, grade.transform.localPosition.z);
    }

    static void SetY(TMP_Text text, float y)
    {
        if (text == null)
            return;
        var p = text.transform.localPosition;
        text.transform.localPosition = new Vector3(p.x, y, p.z);
    }

    void PlaceInFrontOfViewer()
    {
        var view = viewer != null ? viewer : Camera.main != null ? Camera.main.transform : null;
        if (view == null)
            return;
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(view.up, Vector3.up);
        forward.Normalize();
        transform.position = view.position + forward * distance;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    IEnumerator Reveal(int bank, int stars, int happy, int angry)
    {
        int[] targets = { bank, stars, happy, angry };
        string[] formats = { "${0:N0}", "{0}", "{0}", "{0}" };
        foreach (var v in values)
            v.text = "";
        grade.text = "";

        for (int i = 0; i < values.Length && i < targets.Length; i++)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / countUpSeconds)
            {
                values[i].text = string.Format(formats[i], Mathf.RoundToInt(targets[i] * (1f - (1f - t) * (1f - t))));
                yield return null;
            }
            values[i].text = string.Format(formats[i], targets[i]);
            yield return new WaitForSeconds(rowInterval);
        }

        yield return new WaitForSeconds(gradeDelay);
        string g = Grade(bank);
        grade.text = g;
        grade.color = GradeColor(g);
        if (g == "S" && sBurst != null)
        {
            sBurst.Clear(true);
            sBurst.Play(true);
        }
        for (float t = 0f; t < 1f; t += Time.deltaTime / popSeconds)
        {
            grade.transform.localScale = Vector3.one * (1f + (popScale - 1f) * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        grade.transform.localScale = Vector3.one;
    }
}
