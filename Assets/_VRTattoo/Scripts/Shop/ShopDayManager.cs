using System.Collections;
using UnityEngine;

// What happened during one day, for the summary panel.
public class DayStats
{
    public int customers;     // everyone who left the shop today
    public int earned;        // tattoo money, tips not included
    public int tips;
    public int qualitySum;    // over the customers who got on the bed (walk-outs count as 0)
    public int jobs;
    public int happy;         // heart eyes or smiling
    public int angry;         // angry emote, walked out in pain, or gave up waiting
    public int stars;

    public int AverageQuality => jobs > 0 ? Mathf.RoundToInt(qualitySum / (float)jobs) : 0;
}

// The shop's working day: a "Day N" banner, then customersPerDay customers come in; when the last
// one has walked out of the door the day summary shows up (NEXT DAY / RESTART).
// Later days pick hard designs more often and customers have less patience.
// The game lasts totalDays days: the last day's summary has FINISH instead of NEXT DAY, which opens
// the Game Complete panel (bank, stars, happy / angry over the whole game, grade); PLAY AGAIN = day 1, $0.
// Day, money and the game totals are saved to a JSON file (ShopSave) at the end of every day.
public class ShopDayManager : MonoBehaviour
{
    public CustomerQueueManager queue;
    public JobCard card;
    public DayBanner banner;
    public DaySummaryPanel summary;
    public GameCompletePanel gameComplete;

    [Tooltip("Days in one game; the Game Complete panel follows the last day's summary")]
    [Min(1)] public int totalDays = 5;
    [Tooltip("Customers who come in each day (served, walked out or gave up waiting)")]
    public int customersPerDay = 5;
    [Tooltip("Seconds between the last customer leaving and the summary")]
    public float summaryDelay = 1f;

    [Header("Harder every day")]
    [Tooltip("Days until the design weights reach the job manager's end weights (day 1 = start weights)")]
    public int rampDays = 5;
    [Tooltip("Customer patience (waiting and on the bed) is multiplied by this once per day")]
    [Range(0.5f, 1f)] public float patiencePerDay = 0.92f;
    [Tooltip("Lowest patience multiplier")]
    [Range(0.1f, 1f)] public float minPatience = 0.6f;

    public int Day { get; private set; } = 1;
    public int Money { get; private set; }
    public DayStats Today { get; private set; } = new DayStats();
    public bool DayRunning { get; private set; }
    public bool IsLastDay => Day >= totalDays;
    public bool GameOver { get; private set; }

    // Over the finished days of this game.
    public int TotalStars { get; private set; }
    public int TotalHappy { get; private set; }
    public int TotalAngry { get; private set; }

    // 0 on day 1 .. 1 after rampDays more days (TattooJobManager.PickStencil).
    public float DifficultyRamp => rampDays > 0 ? Mathf.Clamp01((Day - 1) / (float)rampDays) : 1f;
    public float PatienceScale => Mathf.Max(minPatience, Mathf.Pow(patiencePerDay, Day - 1));

    void Awake()
    {
        // No customers until the banner has been shown.
        if (queue != null)
            queue.BeginDay(0, 1f);
    }

    void OnEnable()
    {
        if (queue != null)
        {
            queue.onCustomerLeft.AddListener(OnCustomerLeft);
            queue.onCustomerGaveUp.AddListener(OnCustomerGaveUp);
        }
        if (summary != null)
        {
            summary.onNextDay.AddListener(NextDay);
            summary.onRestart.AddListener(Restart);
        }
        if (gameComplete != null)
            gameComplete.onPlayAgain.AddListener(Restart);
    }

    void OnDisable()
    {
        if (queue != null)
        {
            queue.onCustomerLeft.RemoveListener(OnCustomerLeft);
            queue.onCustomerGaveUp.RemoveListener(OnCustomerGaveUp);
        }
        if (summary != null)
        {
            summary.onNextDay.RemoveListener(NextDay);
            summary.onRestart.RemoveListener(Restart);
        }
        if (gameComplete != null)
            gameComplete.onPlayAgain.RemoveListener(Restart);
    }

    void Start()
    {
        var save = ShopSave.Load();
        Day = save.day;
        Money = save.money;
        TotalStars = save.stars;
        TotalHappy = save.happy;
        TotalAngry = save.angry;
        Debug.Log($"[ShopDay] Loaded day {Day}, ${Money} from {ShopSave.FilePath}");

        // Quit on the Game Complete panel last time: show it again.
        if (Day > totalDays)
        {
            Day = totalDays;
            ShowGameComplete();
        }
        else
        {
            StartDay();
        }
    }

    void StartDay()
    {
        StopAllCoroutines();
        StartCoroutine(StartDayRoutine());
    }

    IEnumerator StartDayRoutine()
    {
        Today = new DayStats();
        DayRunning = false;
        GameOver = false;
        if (summary != null)
            summary.Hide();
        if (gameComplete != null)
            gameComplete.Hide();
        if (card != null)
        {
            card.gameObject.SetActive(true);
            card.SetDay(Day, totalDays);
            card.ShowWaiting(Money);
        }
        Debug.Log($"[ShopDay] Day {Day}/{totalDays} starts: {customersPerDay} customers, hard-design ramp {DifficultyRamp:0.00}, patience x{PatienceScale:0.00}, bank ${Money}");

        if (banner != null)
            yield return banner.Show($"Day {Day} / {totalDays}");

        DayRunning = true;
        if (queue != null)
            queue.BeginDay(customersPerDay, PatienceScale);
    }

    // TattooJobManager: a customer on the bed was finished (quality -1 = walked out in pain).
    public void RecordJob(int quality, int stars, int pay, int tip, CustomerMood mood)
    {
        Today.jobs++;
        Today.qualitySum += Mathf.Max(0, quality);
        Today.stars += stars;
        Today.earned += pay;
        Today.tips += tip;
        Money += pay + tip;
        if (mood >= CustomerMood.Happy)
            Today.happy++;
        else if (mood == CustomerMood.Angry)
            Today.angry++;
    }

    void OnCustomerGaveUp(Customer customer) => Today.angry++;

    void OnCustomerLeft(Customer customer)
    {
        Today.customers++;
        if (DayRunning && queue.DayDone)
        {
            DayRunning = false;
            StartCoroutine(EndDayRoutine());
        }
    }

    IEnumerator EndDayRoutine()
    {
        var t = Today;
        TotalStars += t.stars;
        TotalHappy += t.happy;
        TotalAngry += t.angry;
        Debug.Log($"[ShopDay] Day {Day}/{totalDays} done: customers {t.customers}, tattoos ${t.earned}, tips ${t.tips}, avg quality {t.AverageQuality} ({t.jobs} jobs), happy {t.happy}, angry {t.angry}, stars {t.stars}, bank ${Money}");
        ShopSave.Save(new ShopSaveData { day = Day + 1, money = Money, stars = TotalStars, happy = TotalHappy, angry = TotalAngry });

        yield return new WaitForSeconds(summaryDelay);

        if (card != null)
            card.gameObject.SetActive(false);
        if (summary != null)
            summary.Show(Day, totalDays, t, Money, IsLastDay);
    }

    // The summary's NEXT DAY button (FINISH on the last day).
    public void NextDay()
    {
        if (DayRunning || GameOver)
            return;
        if (IsLastDay)
        {
            ShowGameComplete();
            return;
        }
        Day++;
        StartDay();
    }

    void ShowGameComplete()
    {
        StopAllCoroutines();
        GameOver = true;
        DayRunning = false;
        if (summary != null)
            summary.Hide();
        if (card != null)
            card.gameObject.SetActive(false);
        string grade = gameComplete != null ? gameComplete.Grade(Money) : "-";
        Debug.Log($"[ShopDay] Game complete after {totalDays} days: bank ${Money}, stars {TotalStars}, happy {TotalHappy}, angry {TotalAngry}, grade {grade}");
        if (gameComplete != null)
            gameComplete.Show(Money, TotalStars, TotalHappy, TotalAngry);
    }

    // Back to day 1 with an empty bank (RESTART on the summary, PLAY AGAIN on Game Complete).
    public void Restart()
    {
        if (DayRunning)
            return;
        Day = 1;
        Money = 0;
        TotalStars = TotalHappy = TotalAngry = 0;
        ShopSave.Save(new ShopSaveData());
        Debug.Log("[ShopDay] Restart: day 1, $0");
        StartDay();
    }
}
