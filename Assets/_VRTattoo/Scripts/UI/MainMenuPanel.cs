using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Start menu in the job card's neon style: "VR Tattoo" (Pacifico) with START / SETTINGS / QUIT, and a
// settings page (VolumeRow x4 + BACK). Floats in front of the player at eye level, facing them. If a wall,
// sign, picture or piece of furniture is closer than `distance`, the panel moves in to stay in front of it.
// While IsOpen the day has not started: no customers, the chair cannot be used, the machine stays on its holder.
// The same panel is the pause menu (GamePause): RESUME / SETTINGS / MAIN MENU / QUIT, only its own buttons
// can be pressed while it is up, and BACK on the settings page returns to whichever page opened it.
public class MainMenuPanel : MonoBehaviour
{
    enum Page { Main, Pause, Settings }

    // The start menu is up (the game has not started).
    public static bool IsOpen { get; private set; }
    // The pause menu is up.
    public static bool IsPauseOpen { get; private set; }

    public GameObject mainPage;
    public GameObject settingsPage;
    public NeonButton startButton;
    public NeonButton settingsButton;
    public NeonButton quitButton;
    public NeonButton backButton;
    public VolumeRow[] volumeRows;

    [Header("Pause page")]
    public GameObject pausePage;
    public NeonButton resumeButton;
    public NeonButton pauseSettingsButton;
    public NeonButton mainMenuButton;
    public NeonButton pauseQuitButton;

    [Tooltip("START pressed (ShopDayManager begins the game)")]
    public UnityEvent onStart;
    [Tooltip("RESUME pressed on the pause page")]
    public UnityEvent onResume;
    [Tooltip("MAIN MENU pressed on the pause page")]
    public UnityEvent onMainMenu;

    [Header("Placement")]
    [Tooltip("Defaults to the main camera")]
    public Transform viewer;
    [Tooltip("Distance in front of the player (m), at eye level")]
    public float distance = 0.9f;
    [Tooltip("Closest the panel comes when something is in the way (m). Closer panels are scaled down by" +
             " distance / `distance`, so they look the same size")]
    public float minDistance = 0.35f;
    [Tooltip("Gap kept in front of whatever is in the way (m)")]
    public float clearance = 0.15f;
    [Tooltip("Panel size for the obstacle check (m): the larger settings page with its glow")]
    public Vector2 panelSize = new Vector2(0.47f, 0.43f);
    [Tooltip("Wait for the headset to report the head pose before placing (s)")]
    public float placeDelay = 0.3f;
    [Tooltip("Decor without colliders (signs, pictures, lamps, furniture): the panel moves in until it clears their bounds")]
    public Transform[] avoidRoots;
    [Tooltip("Step towards the player (and upwards) while it still overlaps (m)")]
    public float avoidStep = 0.05f;
    [Tooltip("Highest the panel is raised above eye level to clear something (m)")]
    public float maxLift = 0.3f;
    [Tooltip("Turns tried, in order, when straight ahead is not clear (degrees)")]
    public float[] yawSteps = { 0f, 15f, -15f, 30f, -30f, 45f, -45f };

    Page settingsReturn = Page.Main;
    readonly List<Renderer> avoidRenderers = new List<Renderer>();

    void OnEnable()
    {
        AddListener(startButton, PressStart);
        AddListener(settingsButton, ShowSettings);
        AddListener(quitButton, PressQuit);
        AddListener(backButton, PressBack);
        AddListener(resumeButton, PressResume);
        AddListener(pauseSettingsButton, ShowSettings);
        AddListener(mainMenuButton, PressMainMenu);
        AddListener(pauseQuitButton, PressQuit);
    }

    void OnDisable()
    {
        RemoveListener(startButton, PressStart);
        RemoveListener(settingsButton, ShowSettings);
        RemoveListener(quitButton, PressQuit);
        RemoveListener(backButton, PressBack);
        RemoveListener(resumeButton, PressResume);
        RemoveListener(pauseSettingsButton, ShowSettings);
        RemoveListener(mainMenuButton, PressMainMenu);
        RemoveListener(pauseQuitButton, PressQuit);
        IsOpen = false;
        IsPauseOpen = false;
        if (NeonButton.OnlyUnder == transform)
            NeonButton.OnlyUnder = null;
    }

    static void AddListener(NeonButton button, UnityAction action)
    {
        if (button != null)
            button.onPressed.AddListener(action);
    }

    static void RemoveListener(NeonButton button, UnityAction action)
    {
        if (button != null)
            button.onPressed.RemoveListener(action);
    }

    public void Show() => Open(Page.Main);

    // Pause menu: buttons elsewhere (job card, day summary) are switched off until Hide.
    public void ShowPause() => Open(Page.Pause);

    void Open(Page page)
    {
        IsOpen = page == Page.Main;
        IsPauseOpen = page == Page.Pause;
        NeonButton.OnlyUnder = IsPauseOpen ? transform : null;
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(PlaceWhenReady(page));
    }

    public void Hide()
    {
        IsOpen = false;
        IsPauseOpen = false;
        if (NeonButton.OnlyUnder == transform)
            NeonButton.OnlyUnder = null;
        gameObject.SetActive(false);
    }

    // Pages stay hidden until the panel is in place. Real time: the game may be paused.
    IEnumerator PlaceWhenReady(Page page)
    {
        SetPage(null);
        yield return new WaitForSecondsRealtime(placeDelay);
        PlaceInFrontOfViewer();
        // Physics is not stepped while paused: move the button colliders now so rays hit them.
        Physics.SyncTransforms();
        ShowPage(page);
    }

    void ShowSettings()
    {
        settingsReturn = IsPauseOpen ? Page.Pause : Page.Main;
        ShowPage(Page.Settings);
    }

    void PressBack() => ShowPage(settingsReturn);

    void ShowPage(Page page)
    {
        SetPage(page);
        if (page == Page.Settings && volumeRows != null)
        {
            foreach (var row in volumeRows)
                if (row != null)
                    row.Refresh();
        }
        Debug.Log($"[MainMenu] Page: {page.ToString().ToLowerInvariant()}");
    }

    void SetPage(Page? page)
    {
        if (mainPage != null)
            mainPage.SetActive(page == Page.Main);
        if (pausePage != null)
            pausePage.SetActive(page == Page.Pause);
        if (settingsPage != null)
            settingsPage.SetActive(page == Page.Settings);
    }

    void PressStart()
    {
        Debug.Log("[MainMenu] START");
        if (ShopSounds.Instance != null)
            ShopSounds.Instance.PlayStartBell();
        Hide();
        onStart?.Invoke();
    }

    void PressResume()
    {
        Debug.Log("[MainMenu] RESUME");
        onResume?.Invoke();
    }

    void PressMainMenu()
    {
        Debug.Log("[MainMenu] MAIN MENU");
        onMainMenu?.Invoke();
    }

    void PressQuit()
    {
        Debug.Log("[MainMenu] QUIT");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Straight ahead at eye level if that spot is clear. Otherwise the nearest clear spot: moved in (and scaled
    // down to look the same size), then raised a little, then turned to the side, always facing the eyes.
    void PlaceInFrontOfViewer()
    {
        var view = viewer != null ? viewer : Camera.main != null ? Camera.main.transform : null;
        if (view == null)
            return;
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(view.up, Vector3.up);
        forward.Normalize();

        avoidRenderers.Clear();
        if (avoidRoots != null)
        {
            foreach (var root in avoidRoots)
                if (root != null)
                    avoidRenderers.AddRange(root.GetComponentsInChildren<Renderer>());
        }

        string firstBlocker = null;
        string lastBlocker = null;
        Vector3 best = view.position + forward * minDistance;
        float bestYaw = 0f, bestLift = 0f, bestD = minDistance;
        bool found = false;
        for (int i = 0; i < yawSteps.Length && !found; i++)
        {
            Vector3 dir = Quaternion.AngleAxis(yawSteps[i], Vector3.up) * forward;
            float maxD = ClearDistance(view, dir, out string hitName);
            firstBlocker ??= hitName;
            for (float lift = 0f; lift <= maxLift + 0.001f && !found; lift += avoidStep)
            {
                for (float d = maxD; d >= minDistance - 0.001f; d -= avoidStep)
                {
                    Vector3 c = view.position + dir * d + Vector3.up * lift;
                    lastBlocker = OverlappedDecor(c, Quaternion.LookRotation(c - view.position, Vector3.up), d / distance);
                    if (lastBlocker == null)
                    {
                        best = c;
                        bestD = d;
                        bestYaw = yawSteps[i];
                        bestLift = lift;
                        found = true;
                        break;
                    }
                    firstBlocker ??= lastBlocker;
                }
            }
        }

        transform.position = best;
        transform.rotation = Quaternion.LookRotation(best - view.position, Vector3.up);
        transform.localScale = Vector3.one * (bestD / distance);
        Debug.Log($"[MainMenu] Shown at {best:F2}, distance {bestD:0.00} m (scale {bestD / distance:0.00}), raised {bestLift:0.00} m, turned {bestYaw:0} deg " +
                  $"(in the way: {firstBlocker ?? "none"}{(found ? "" : ", no clear spot, still overlapping " + lastBlocker)})");
    }

    // How far along `dir` the panel can go before something solid (minus the clearance).
    float ClearDistance(Transform view, Vector3 dir, out string blocker)
    {
        blocker = null;
        float d = distance;
        var hits = Physics.BoxCastAll(view.position, new Vector3(panelSize.x, panelSize.y, 0.01f) * 0.5f, dir,
            Quaternion.LookRotation(dir, Vector3.up), distance + clearance, ~0, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.distance <= 0f || hit.collider.transform.IsChildOf(view.root) || hit.collider.transform.IsChildOf(transform))
                continue;
            float limit = Mathf.Max(minDistance, hit.distance - clearance);
            if (limit < d)
            {
                d = limit;
                blocker = hit.collider.name;
            }
        }
        return d;
    }

    // Name of the first avoidRoots renderer whose bounds meet the panel's, or null.
    string OverlappedDecor(Vector3 center, Quaternion rotation, float scale)
    {
        Vector3 right = rotation * Vector3.right * (panelSize.x * 0.5f * scale);
        Vector3 up = rotation * Vector3.up * (panelSize.y * 0.5f * scale);
        var panel = new Bounds(center, Vector3.zero);
        panel.Encapsulate(center + right + up);
        panel.Encapsulate(center - right - up);
        panel.Encapsulate(center + right - up);
        panel.Encapsulate(center - right + up);
        panel.Expand(0.02f);
        foreach (var r in avoidRenderers)
        {
            if (r != null && r.enabled && !(r is ParticleSystemRenderer) && r.bounds.Intersects(panel))
                return r.name;
        }
        return null;
    }
}
