using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// One tattoo job per customer: picks the design when the customer lies down, shows it as a
// stencil on the customer's forearm (CustomerTattooSkin), scores the ink on submit, then sends the
// customer home through CustomerQueueManager. Everything the player sees is on the JobCard beside the bed.
public class TattooJobManager : MonoBehaviour
{
    public CustomerQueueManager queue;
    public StencilLibrary library;

    [Header("UI")]
    public JobCard card;
    [Tooltip("The card's DONE button")]
    public NeonButton submitButton;
    [Tooltip("How long the score stays on the card before the customer gets up")]
    public float scoreDisplaySeconds = 3.5f;

    [Tooltip("Submit key for desktop testing (Unity Editor only)")]
    public Key submitKey = Key.J;

    [Header("Scoring")]
    public TattooScoringRules scoring = TattooScoringRules.Default;

    [Header("Customer mood")]
    [Tooltip("Quality ranges for the emote above the customer's head and their reaction sound")]
    public CustomerMoodThresholds moods = CustomerMoodThresholds.Default;

    [Header("Difficulty ramp")]
    [Tooltip("After this many finished customers the hard designs reach their highest chance")]
    public int rampCustomers = 10;
    public Vector3 startWeights = new Vector3(0.7f, 0.25f, 0.05f);  // easy, medium, hard
    public Vector3 endWeights = new Vector3(0.2f, 0.4f, 0.4f);

    StencilEntry current;
    StencilEntry previous;
    bool submitting;

    public int CompletedCustomers { get; private set; }
    public int TotalScore { get; private set; }
    public StencilEntry CurrentStencil => current;

    void OnEnable()
    {
        if (queue != null)
            queue.onCustomerLayDown.AddListener(OnCustomerLayDown);
        if (submitButton != null)
            submitButton.onPressed.AddListener(Submit);
    }

    void OnDisable()
    {
        if (queue != null)
            queue.onCustomerLayDown.RemoveListener(OnCustomerLayDown);
        if (submitButton != null)
            submitButton.onPressed.RemoveListener(Submit);
    }

    void Start()
    {
        if (card != null)
            card.ShowWaiting(TotalScore);
    }

    void Update()
    {
#if UNITY_EDITOR
        var keyboard = Keyboard.current;
        if (submitKey != Key.None && keyboard != null && keyboard[submitKey].wasPressedThisFrame)
            Submit();
#endif
        if (card != null)
            card.SetDoneAvailable(CanSubmit);
    }

    // A design is on the customer lying on the bed and it has not been submitted yet.
    public bool CanSubmit
    {
        get
        {
            if (submitting || current == null || queue == null || !queue.IsCustomerReady)
                return false;
            var skin = queue.CustomerOnBed.GetComponent<CustomerTattooSkin>();
            return skin != null && skin.IsReady;
        }
    }

    void OnCustomerLayDown(Customer customer)
    {
        current = PickStencil();
        if (current == null)
        {
            Debug.LogWarning("[TattooJob] Stencil library is empty");
            return;
        }

        // Shown as soon as the forearm surface is built (it waits for the pose to settle).
        if (customer.TryGetComponent(out CustomerTattooSkin skin))
            skin.ShowStencil(current.texture);
        if (card != null)
            card.ShowJob(current, TotalScore);
    }

    public void Submit()
    {
        if (submitting || current == null || queue == null || !queue.IsCustomerReady)
            return;
        var skin = queue.CustomerOnBed.GetComponent<CustomerTattooSkin>();
        if (skin == null || !skin.IsReady)
            return;
        StartCoroutine(SubmitRoutine(skin, -1));
    }

#if UNITY_EDITOR
    // Testing: submits like the real button, but with the given quality instead of the scored one.
    public void SubmitForTest(int quality)
    {
        if (submitting || current == null || queue == null || !queue.IsCustomerReady)
            return;
        var skin = queue.CustomerOnBed.GetComponent<CustomerTattooSkin>();
        if (skin != null && skin.IsReady)
            StartCoroutine(SubmitRoutine(skin, quality));
    }
#endif

    IEnumerator SubmitRoutine(CustomerTattooSkin skin, int forcedQuality)
    {
        submitting = true;

        var score = TattooScorer.Score(skin.Canvas, current.texture, skin.Placement, scoring);
        if (forcedQuality >= 0)
            score.quality = forcedQuality;
        float multiplier = StencilLibrary.Multiplier(current.difficulty);
        int points = Mathf.RoundToInt(score.quality * multiplier);
        TotalScore += points;
        var mood = moods.For(score.quality);
        Debug.Log($"[TattooJob] {current.displayName}: accuracy {score.accuracy:P0}, mess area {score.messRatio:0.00}x design (-{score.messPenalty}), quality {score.quality}, +{points}, mood {mood}");

        // The customer reacts: emote above the head (stays until they leave) and a voice line.
        if (skin.TryGetComponent(out CustomerEmote emote))
            emote.Show(mood);
        if (skin.TryGetComponent(out CustomerVoice voice))
            voice.PlayMood(mood);

        // Only the result is shown; the formula (accuracy, mess, multiplier) stays in the log above.
        if (card != null)
            card.ShowResult(score.quality, points, TotalScore);

        yield return new WaitForSeconds(scoreDisplaySeconds);

        if (card != null)
            card.ShowWaiting(TotalScore);
        if (skin != null)
            skin.HideStencil();
        previous = current;
        current = null;
        CompletedCustomers++;
        queue.CompleteTattoo();
        submitting = false;
    }

    // Chooses a difficulty by weight (easy designs early, harder ones the longer the shift goes),
    // then a design of that difficulty that is not the same as the previous customer's.
    StencilEntry PickStencil()
    {
        if (library == null || library.stencils.Count == 0)
            return null;

        float t = rampCustomers > 0 ? Mathf.Clamp01(CompletedCustomers / (float)rampCustomers) : 1f;
        Vector3 weights = Vector3.Lerp(startWeights, endWeights, t);

        var byLevel = new List<StencilEntry>[3];
        for (int i = 0; i < 3; i++)
            byLevel[i] = new List<StencilEntry>();
        foreach (var entry in library.stencils)
            if (entry != null && entry.texture != null && entry != previous)
                byLevel[(int)entry.difficulty].Add(entry);

        float total = 0f;
        for (int i = 0; i < 3; i++)
            if (byLevel[i].Count > 0)
                total += weights[i];

        if (total <= 0f)
            return previous;  // only one design in the library

        float r = Random.value * total;
        List<StencilEntry> chosen = null;
        for (int i = 0; i < 3; i++)
        {
            if (byLevel[i].Count == 0)
                continue;
            chosen = byLevel[i];  // falls through to the last non-empty level on rounding error
            r -= weights[i];
            if (r <= 0f)
                break;
        }
        return chosen[Random.Range(0, chosen.Count)];
    }
}
