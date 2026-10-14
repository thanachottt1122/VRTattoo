using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// One tattoo job per customer: picks the design when the customer lies down, shows it as a
// stencil on the customer's forearm (CustomerTattooSkin), scores the ink on submit, then sends the
// customer home through CustomerQueueManager.
public class TattooJobManager : MonoBehaviour
{
    public CustomerQueueManager queue;
    public StencilLibrary library;

    [Header("Labels")]
    public TMP_Text orderLabel;
    public TMP_Text scoreLabel;
    public TMP_Text totalLabel;

    [Header("Submit")]
    public XRBaseInteractable submitButton;
    public float scoreDisplaySeconds = 2.5f;

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
            submitButton.selectEntered.AddListener(OnSubmitPressed);
    }

    void OnDisable()
    {
        if (queue != null)
            queue.onCustomerLayDown.RemoveListener(OnCustomerLayDown);
        if (submitButton != null)
            submitButton.selectEntered.RemoveListener(OnSubmitPressed);
    }

    void Start()
    {
        SetText(orderLabel, "");
        SetText(scoreLabel, "");
        UpdateTotal();
    }

    void Update()
    {
#if UNITY_EDITOR
        var keyboard = Keyboard.current;
        if (submitKey != Key.None && keyboard != null && keyboard[submitKey].wasPressedThisFrame)
            Submit();
#endif
    }

    void OnSubmitPressed(SelectEnterEventArgs args) => Submit();

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
        SetText(orderLabel, $"{current.displayName.ToUpperInvariant()}\n<size=75%><color={DifficultyColor(current.difficulty)}>{current.difficulty.ToString().ToUpperInvariant()}</color></size>");
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

        // Why the score is what it is: how much of the design was inked, and what the mess cost.
        string mess = score.messPenalty > 0 ? $"<color=#FF5050>Mess -{score.messPenalty}</color>" : "Mess 0";
        SetText(scoreLabel, $"{score.quality}<size=50%> / 100</size>\n" +
                            $"<size=38%>Accuracy {Mathf.RoundToInt(score.accuracy * 100f)}%  /  {mess}</size>\n" +
                            $"<size=45%>x{multiplier:0.#} = +{points}</size>");
        UpdateTotal();

        yield return new WaitForSeconds(scoreDisplaySeconds);

        SetText(scoreLabel, "");
        SetText(orderLabel, "");
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

    void UpdateTotal() => SetText(totalLabel, $"TOTAL SCORE\n{TotalScore}");

    static void SetText(TMP_Text label, string text)
    {
        if (label != null)
            label.text = text;
    }

    static string DifficultyColor(StencilDifficulty difficulty) => difficulty switch
    {
        StencilDifficulty.Medium => "#FF9A1F",
        StencilDifficulty.Hard => "#FF3B3B",
        _ => "#3CC94A",
    };
}
