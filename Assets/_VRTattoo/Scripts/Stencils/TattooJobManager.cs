using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// One tattoo job per customer: picks the design when the customer lies down, shows it as a
// stencil on the customer's forearm (CustomerTattooSkin), scores the ink on submit, then sends the
// customer home through CustomerQueueManager. Everything the player sees is on the JobCard beside the bed.
// While tattooing it also watches the customer's pain (CustomerPain: at 100 % they walk out with 0)
// and the time the design allows (CustomerPatience + PatienceScoring: bonus if fast, less quality if late).
// A finished job pays money (MoneyRules: price by difficulty x quality, a random tip for 3 stars); the
// results go to ShopDayManager for the day summary.
public class TattooJobManager : MonoBehaviour
{
    public CustomerQueueManager queue;
    public StencilLibrary library;
    [Tooltip("Day, bank and day statistics; optional (without it money is only kept for this session)")]
    public ShopDayManager shopDay;

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

    [Header("Money")]
    public MoneyRules money = MoneyRules.Default;

    [Header("Time on the bed")]
    [Tooltip("Time per difficulty and the fast bonus / overtime penalty")]
    public PatienceScoring patience = PatienceScoring.Default;

    [Header("Customer mood")]
    [Tooltip("Quality ranges for the emote above the customer's head and their reaction sound")]
    public CustomerMoodThresholds moods = CustomerMoodThresholds.Default;

    [Header("Difficulty ramp")]
    [Tooltip("Without a ShopDayManager: after this many finished customers the hard designs reach their highest chance (with one, the ramp follows the day)")]
    public int rampCustomers = 10;
    public Vector3 startWeights = new Vector3(0.7f, 0.25f, 0.05f);  // easy, medium, hard
    public Vector3 endWeights = new Vector3(0.2f, 0.4f, 0.4f);

    StencilEntry current;
    StencilEntry previous;
    bool submitting;
    CustomerPain bedPain;
    CustomerPatience bedPatience;
    CustomerTattooSkin resultSkin;  // the arm that glints on 3 stars
    int sessionMoney;  // used when there is no ShopDayManager

    public int CompletedCustomers { get; private set; }
    public int TotalScore { get; private set; }
    public int Money => shopDay != null ? shopDay.Money : sessionMoney;
    public StencilEntry CurrentStencil => current;

    void OnEnable()
    {
        if (queue != null)
            queue.onCustomerLayDown.AddListener(OnCustomerLayDown);
        if (submitButton != null)
            submitButton.onPressed.AddListener(Submit);
        if (card != null)
            card.onThreeStars.AddListener(OnThreeStars);
    }

    void OnDisable()
    {
        if (queue != null)
            queue.onCustomerLayDown.RemoveListener(OnCustomerLayDown);
        if (submitButton != null)
            submitButton.onPressed.RemoveListener(Submit);
        if (card != null)
            card.onThreeStars.RemoveListener(OnThreeStars);
    }

    void Start()
    {
        if (card != null)
            card.ShowWaiting(Money);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (submitKey != Key.None && keyboard != null && keyboard[submitKey].wasPressedThisFrame && !GamePause.IsPaused)
            Submit();
        bool tattooing = !submitting && current != null && queue != null && queue.IsCustomerReady;
        if (tattooing && bedPain != null && bedPain.MaxedOut)
        {
            StartCoroutine(WalkOutRoutine(queue.CustomerOnBed.GetComponent<CustomerTattooSkin>()));
            tattooing = false;
        }

        if (card == null)
            return;
        card.SetDoneAvailable(CanSubmit);
        if (tattooing)
        {
            if (bedPain != null)
                card.SetPain(bedPain.Pain, bedPain.flinchThreshold, bedPain.IsFlinching);
            if (bedPatience != null)
                card.SetTimer(bedPatience.Fraction, bedPatience.Remaining <= 0f);
        }
        if (queue != null && queue.waitSeats != null)
        {
            for (int i = 0; i < queue.waitSeats.Length; i++)
            {
                var waiting = queue.WaitingCustomer(i);
                var p = waiting != null && waiting.IsSettled ? waiting.GetComponent<CustomerPatience>() : null;
                card.SetQueue(i, p != null && p.Running ? p.Fraction : -1f);
            }
        }
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

        // Fresh pain gauge; the clock for this design replaces the waiting-seat one.
        bedPain = customer.GetComponent<CustomerPain>();
        if (bedPain != null)
            bedPain.ResetPain();
        bedPatience = customer.GetComponent<CustomerPatience>();
        if (bedPatience != null)
            bedPatience.StartTimer(patience.Seconds(current.difficulty) * bedPatience.scale);
        if (card != null)
            card.ShowJob(current, Money);
    }

    // Back to the main menu mid-day: the job in progress is forgotten (its customer is gone).
    public void DropJob()
    {
        StopAllCoroutines();
        current = null;
        submitting = false;
        bedPain = null;
        bedPatience = null;
        resultSkin = null;
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

        // Time: late costs quality (so also the mood), fast adds a share of the points.
        float elapsed = 0f, duration = 0f;
        if (bedPatience != null)
        {
            elapsed = bedPatience.Elapsed;
            duration = bedPatience.Duration;
            bedPatience.Stop();
        }
        int overtime = patience.Penalty(elapsed, duration);
        int quality = Mathf.Max(0, score.quality - overtime);
        float bonus = patience.Bonus(elapsed, duration, quality);

        float multiplier = StencilLibrary.Multiplier(current.difficulty);
        int points = Mathf.RoundToInt(quality * multiplier * (1f + bonus));
        TotalScore += points;
        var mood = moods.For(quality);

        int stars = card != null ? card.Stars(quality) : 0;
        int pay = money.Pay(current.difficulty, quality, bonus);
        int tip = stars >= 3 ? money.Tip(current.difficulty) : 0;
        if (shopDay != null)
            shopDay.RecordJob(quality, stars, pay, tip, mood);
        else
            sessionMoney += pay + tip;
        Debug.Log($"[TattooJob] {current.displayName}: accuracy {score.accuracy:P0}, mess area {score.messRatio:0.00}x design (-{score.messPenalty}), quality {score.quality}, time {elapsed:0}/{duration:0}s (-{overtime}, bonus +{bonus:P0}) -> {quality}, +{points} pts, {stars} stars, ${pay} + tip ${tip} (bank ${Money}), mood {mood}");

        // The customer reacts: emote above the head (stays until they leave) and, once the stars are on the
        // card, a voice line by stars: 1 = insult, 2 = compliment, 3 = big compliment.
        if (skin.TryGetComponent(out CustomerEmote emote))
            emote.Show(mood);
        if (skin.TryGetComponent(out CustomerVoice voice))
        {
            if (card != null)
                voice.PlayReaction(LineForStars(stars), card.StarsShownSeconds(quality));
            else
                voice.PlayMood(mood);
        }

        // Only the result is shown; the formula (accuracy, mess, multiplier) stays in the log above.
        resultSkin = skin;
        if (card != null)
            card.ShowResult(quality, pay, tip, Money, bonus);

        yield return new WaitForSeconds(scoreDisplaySeconds);

        resultSkin = null;
        if (card != null)
            card.ShowWaiting(Money);
        FinishJob(skin);
    }

    // The pain reached 100 %: the customer gets up angry straight away and the job earns nothing.
    IEnumerator WalkOutRoutine(CustomerTattooSkin skin)
    {
        submitting = true;
        Debug.Log($"[TattooJob] {current.displayName}: the customer walked out in pain (tolerance {bedPain.Tolerance:0.00}), $0");
        if (shopDay != null)
            shopDay.RecordJob(0, 0, 0, 0, CustomerMood.Angry);

        if (bedPatience != null)
            bedPatience.Stop();
        if (skin.TryGetComponent(out CustomerEmote emote))
            emote.Show(CustomerMood.Angry);
        if (skin.TryGetComponent(out CustomerVoice voice))
            voice.PlayReaction(CustomerVoiceLine.Angry);
        if (card != null)
            card.ShowWalkout(Money);
        FinishJob(skin);

        yield return new WaitForSeconds(scoreDisplaySeconds);

        // Unless the next customer has already lain down and the card shows their job.
        if (card != null && current == null && card.gameObject.activeInHierarchy)
            card.ShowWaiting(Money);
    }

    void FinishJob(CustomerTattooSkin skin)
    {
        if (skin != null)
            skin.HideStencil();
        previous = current;
        current = null;
        bedPain = null;
        bedPatience = null;
        CompletedCustomers++;
        queue.CompleteTattoo();
        submitting = false;
    }

    static CustomerVoiceLine LineForStars(int stars) =>
        stars >= 3 ? CustomerVoiceLine.Ecstatic : stars == 2 ? CustomerVoiceLine.Happy : CustomerVoiceLine.Angry;

    void OnThreeStars()
    {
        if (resultSkin != null)
            resultSkin.PlayGlint();
    }

    // Chooses a difficulty by weight (easy designs early, harder ones on later days),
    // then a design of that difficulty that is not the same as the previous customer's.
    StencilEntry PickStencil()
    {
        if (library == null || library.stencils.Count == 0)
            return null;

        float t = shopDay != null ? shopDay.DifficultyRamp
            : rampCustomers > 0 ? Mathf.Clamp01(CompletedCustomers / (float)rampCustomers) : 1f;
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
