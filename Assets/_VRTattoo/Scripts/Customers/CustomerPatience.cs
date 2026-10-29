using System;
using UnityEngine;
using Random = UnityEngine.Random;

// On the Customer prefab: how long the customer is willing to wait.
//   on a waiting seat: waitSeconds (random); CustomerQueueManager sends them home when it runs out
//   on the bed:        TattooJobManager starts a timer for the design (PatienceScoring) and turns
//                      the time used into a bonus or a penalty; it keeps counting into overtime
// Near the end the emote above the head turns annoyed. Walking does not count.
public class CustomerPatience : MonoBehaviour
{
    [Tooltip("Random patience while waiting on a seat (s)")]
    public Vector2 waitSeconds = new Vector2(150f, 220f);

    [Tooltip("The customer looks annoyed once less than this fraction of the time is left")]
    [Range(0f, 1f)] public float annoyedFraction = 0.25f;

    [Tooltip("Multiplies the waiting and bed time (set per day by ShopDayManager through the queue)")]
    public float scale = 1f;

    public bool Running { get; private set; }
    public float Duration { get; private set; }
    // Seconds left; below 0 in overtime.
    public float Remaining { get; private set; }
    public float Elapsed => Duration - Remaining;
    public float Fraction => Duration > 0f ? Mathf.Clamp01(Remaining / Duration) : 1f;
    public bool OutOfPatience => Running && Remaining <= 0f;

    Customer customer;
    CustomerEmote emote;

    void Awake()
    {
        customer = GetComponent<Customer>();
        emote = GetComponent<CustomerEmote>();
    }

    public void StartTimer(float seconds)
    {
        Duration = Remaining = Mathf.Max(seconds, 0.01f);
        Running = true;
        UpdateEmote();
    }

    // Called on reaching a waiting seat. Moving up to the next seat keeps the time already used.
    public void StartWaiting()
    {
        if (!Running)
            StartTimer(Random.Range(waitSeconds.x, waitSeconds.y) * scale);
    }

    public void Stop()
    {
        Running = false;
        if (emote != null)
            emote.SetAnnoyed(false);
    }

    void Update()
    {
        if (!Running || (customer != null && customer.IsWalking))
            return;
        Remaining -= Time.deltaTime;
        UpdateEmote();
    }

    void UpdateEmote()
    {
        if (emote != null)
            emote.SetAnnoyed(Running && Remaining < Duration * annoyedFraction);
    }
}

// Time on the bed per design difficulty, and what finishing fast or slow does to the score.
[Serializable]
public struct PatienceScoring
{
    [Tooltip("Time for an easy / medium / hard design (s)")]
    public float easySeconds;
    public float mediumSeconds;
    public float hardSeconds;

    [Tooltip("Finishing within this fraction of the time earns a bonus (full bonus at 0, none at this fraction)")]
    [Range(0f, 1f)] public float fastFraction;
    [Tooltip("Largest bonus, as a fraction of the points (0.25 = +25%)")]
    public float fastBonus;
    [Tooltip("No bonus below this quality (no rushing an empty arm)")]
    [Range(0, 100)] public int bonusMinQuality;

    [Tooltip("Quality lost per second over the time")]
    public float overtimePenaltyPerSecond;
    [Tooltip("Most quality lost to overtime")]
    public int maxOvertimePenalty;

    public static PatienceScoring Default => new PatienceScoring
    {
        easySeconds = 90f,
        mediumSeconds = 120f,
        hardSeconds = 170f,
        fastFraction = 0.5f,
        fastBonus = 0.25f,
        bonusMinQuality = 40,
        overtimePenaltyPerSecond = 0.5f,
        maxOvertimePenalty = 15,
    };

    public float Seconds(StencilDifficulty difficulty) => difficulty switch
    {
        StencilDifficulty.Medium => mediumSeconds,
        StencilDifficulty.Hard => hardSeconds,
        _ => easySeconds,
    };

    // Fraction of extra points (0 .. fastBonus).
    public float Bonus(float elapsed, float duration, int quality)
    {
        if (quality < bonusMinQuality || duration <= 0f || fastFraction <= 0f)
            return 0f;
        float used = elapsed / duration;
        return fastBonus * Mathf.Clamp01(1f - used / fastFraction);
    }

    // Quality points lost to overtime.
    public int Penalty(float elapsed, float duration)
    {
        float over = elapsed - duration;
        return over > 0f ? Mathf.Min(Mathf.RoundToInt(over * overtimePenaltyPerSecond), maxOvertimePenalty) : 0;
    }
}
