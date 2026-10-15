using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// End-of-day summary in the job card's neon style. Shows up in front of the player:
// title, then each row counts up in turn (tattoo money, tips, day total, average quality,
// happy / angry customers, stars), then the bank. NEXT DAY and RESTART are NeonButtons, so they work
// with a controller ray (trigger or grip), a poke, and a mouse click in Desktop Test Mode.
public class DaySummaryPanel : MonoBehaviour
{
    public TMP_Text title;
    [Tooltip("Values, in order: tattoos, tips, day total, avg quality, happy, angry, stars")]
    public TMP_Text[] values;
    public TMP_Text bank;
    public NeonButton nextDayButton;
    public NeonButton restartButton;

    public UnityEvent onNextDay;
    public UnityEvent onRestart;

    [Header("Placement")]
    [Tooltip("Defaults to the main camera")]
    public Transform viewer;
    [Tooltip("Distance in front of the player (m)")]
    public float distance = 0.75f;
    [Tooltip("Height relative to the eyes (m)")]
    public float heightOffset = -0.12f;
    [Tooltip("Tilt back so the panel faces eyes above it (degrees)")]
    public float tilt = 10f;

    [Header("Animation")]
    public float rowInterval = 0.15f;
    public float countUpSeconds = 0.5f;

    // Kept inactive in the scene until Show.
    void OnEnable()
    {
        if (nextDayButton != null)
            nextDayButton.onPressed.AddListener(PressNextDay);
        if (restartButton != null)
            restartButton.onPressed.AddListener(PressRestart);
    }

    void OnDisable()
    {
        if (nextDayButton != null)
            nextDayButton.onPressed.RemoveListener(PressNextDay);
        if (restartButton != null)
            restartButton.onPressed.RemoveListener(PressRestart);
    }

    public void Show(int day, DayStats stats, int bankMoney)
    {
        PlaceInFrontOfViewer();
        gameObject.SetActive(true);
        title.text = $"Day {day} Complete";
        StopAllCoroutines();
        StartCoroutine(CountUp(stats, bankMoney));
    }

    public void Hide() => gameObject.SetActive(false);

    void PressNextDay() => onNextDay?.Invoke();
    void PressRestart() => onRestart?.Invoke();

    void PlaceInFrontOfViewer()
    {
        var view = viewer != null ? viewer : Camera.main != null ? Camera.main.transform : null;
        if (view == null)
            return;
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(view.up, Vector3.up);
        forward.Normalize();
        transform.position = view.position + forward * distance + Vector3.up * heightOffset;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(tilt, 0f, 0f);
    }

    IEnumerator CountUp(DayStats s, int bankMoney)
    {
        int[] targets = { s.earned, s.tips, s.earned + s.tips, s.AverageQuality, s.happy, s.angry, s.stars };
        string[] formats = { "${0:N0}", "${0:N0}", "${0:N0}", "{0}", "{0}", "{0}", "{0}" };
        for (int i = 0; i < values.Length; i++)
            values[i].text = "";
        bank.text = "";

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
        bank.text = $"BANK  ${bankMoney:N0}";
    }
}
