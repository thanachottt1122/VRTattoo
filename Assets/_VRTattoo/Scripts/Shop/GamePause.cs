using UnityEngine;
using UnityEngine.InputSystem;

// Pause during play: the left controller's Menu button (Esc in Desktop Test Mode) opens the pause page of
// MainMenuPanel in front of the player; pressing it again resumes. While paused Time.timeScale is 0 (job
// timer, customers, pain and patience stop), game audio is paused, and the machine cannot tattoo.
// Not available on the start menu or the Game Complete panel.
public class GamePause : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    public ShopDayManager day;
    public MainMenuPanel menu;
    [Tooltip("Optional: Esc only works while its mouse mode is on")]
    public TattooDesktopTestMode desktopTestMode;

    [Tooltip("Leave empty for the left controller's Menu button")]
    public InputActionProperty pauseAction;
    public Key desktopKey = Key.Escape;

    InputAction ownAction;

    InputAction Action => pauseAction.action != null && pauseAction.action.bindings.Count > 0 ? pauseAction.action : ownAction;

    void Awake()
    {
        ownAction = new InputAction("Pause", InputActionType.Button);
        ownAction.AddBinding("<XRController>{LeftHand}/{MenuButton}");
        ownAction.AddBinding("<XRController>{LeftHand}/menu");
    }

    void OnEnable()
    {
        if (Action == ownAction || pauseAction.reference == null)
            Action.Enable();
        if (menu != null)
        {
            menu.onResume.AddListener(Resume);
            menu.onMainMenu.AddListener(QuitToMainMenu);
        }
    }

    void OnDisable()
    {
        if (Action == ownAction || pauseAction.reference == null)
            Action.Disable();
        if (menu != null)
        {
            menu.onResume.RemoveListener(Resume);
            menu.onMainMenu.RemoveListener(QuitToMainMenu);
        }
        SetPaused(false);
    }

    void OnDestroy() => ownAction?.Dispose();

    void Update()
    {
        bool pressed = Action.WasPressedThisFrame();
        var keyboard = Keyboard.current;
        if (desktopKey != Key.None && keyboard != null && keyboard[desktopKey].wasPressedThisFrame
            && (desktopTestMode == null || desktopTestMode.IsActive))
            pressed = true;
        if (!pressed)
            return;

        if (IsPaused)
            Resume();
        else
            Pause();
    }

    // Only during a game: not on the start menu, not on Game Complete.
    public bool CanPause => !IsPaused && menu != null && !MainMenuPanel.IsOpen && day != null && day.InGame && !day.GameOver;

    public void Pause()
    {
        if (!CanPause)
        {
            Debug.Log("[GamePause] Pause ignored (start menu or Game Complete)");
            return;
        }
        SetPaused(true);
        menu.ShowPause();
        Debug.Log($"[GamePause] Paused (day {day.Day})");
    }

    public void Resume()
    {
        if (!IsPaused)
            return;
        menu.Hide();
        SetPaused(false);
        Debug.Log("[GamePause] Resumed");
    }

    // MAIN MENU: the day so far is dropped (the save still holds the start of this day).
    void QuitToMainMenu()
    {
        if (!IsPaused)
            return;
        SetPaused(false);
        menu.Hide();
        Debug.Log("[GamePause] Main menu");
        day.QuitToMenu();
    }

    static void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
    }
}
