using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

// Button hints at the artist chair, in the NeonPanel style, matching what the player is using:
// headset, XR Device Simulator on the computer, or Desktop Test Mode (mouse).
//   standing near the chair: one line ("[B] Sit"), floating above the chair and facing the player
//   seated: two short lines beside the work area for showSeconds, then slowly fades away over fadeOutSeconds
//           (shown again when standing up and sitting down again, or when the input mode changes)
public class SeatHintPanel : MonoBehaviour
{
    public TattooSeat seat;
    [Tooltip("Optional: when its mouse mode is on, mouse/keyboard hints are shown")]
    public TattooDesktopTestMode desktopTestMode;
    [Tooltip("Leave empty to find it in the scene. When it is running, keyboard hints are shown")]
    public XRDeviceSimulator deviceSimulator;

    public NeonPanel panel;
    public TMP_Text label;
    [Tooltip("Space between the text and the panel edge (m)")]
    public Vector2 padding = new Vector2(0.03f, 0.02f);

    [Header("Placement")]
    [Tooltip("Where the one-line hint floats while standing (turns to face the player)")]
    public Transform standingAnchor;
    [Tooltip("Where the hint sits while seated (fixed, faces the seated eye position)")]
    public Transform seatedAnchor;

    [Header("Timing")]
    [Tooltip("How long the seated hint stays fully visible after sitting down (s)")]
    public float showSeconds = 12f;
    [Tooltip("How long the seated hint takes to fade away after showSeconds (s)")]
    public float fadeOutSeconds = 3f;
    [Tooltip("Fade change per second when appearing, and for the standing hint")]
    public float fadeSpeed = 3f;

    [Header("Look")]
    public string keyColor = "#FF7ADB";

    public bool debugLog;

    enum InputMode { Headset, Simulator, Desktop }

    float alpha;
    float seatedShownTime = -100f;
    bool wasSeated;
    InputMode lastMode;
    string shownText;

    void Start()
    {
        if (deviceSimulator == null)
            deviceSimulator = FindFirstObjectByType<XRDeviceSimulator>();
        lastMode = Mode;
        Apply(0f);
    }

    InputMode Mode =>
        desktopTestMode != null && desktopTestMode.IsActive ? InputMode.Desktop
        : deviceSimulator != null && deviceSimulator.isActiveAndEnabled ? InputMode.Simulator
        : InputMode.Headset;

    void LateUpdate()
    {
        if (seat == null)
            return;

        bool seated = seat.IsSeated;
        var mode = Mode;
        if ((seated && !wasSeated) || mode != lastMode)
            seatedShownTime = Time.time;
        wasSeated = seated;
        lastMode = mode;

        // Nothing to sit for while the main menu is up.
        bool show = seated ? Time.time - seatedShownTime < showSeconds : seat.IsPlayerInRange() && !MainMenuPanel.IsOpen;
        float speed = seated && !show && fadeOutSeconds > 0f ? 1f / fadeOutSeconds : fadeSpeed;
        float before = alpha;
        alpha = Mathf.MoveTowards(alpha, show ? 1f : 0f, speed * Time.deltaTime);
        if (debugLog && seated && before > 0f && alpha <= 0f)
            Debug.Log($"[SeatHintPanel] seated hint gone after {Time.time - seatedShownTime:0.0} s");

        SetText(seated ? SeatedText(mode) : StandingText(mode));
        Place(seated);
        Apply(alpha);
    }

    string Key(string key) => $"<color={keyColor}>[{key}]</color>";

    string StandingText(InputMode mode) => $"{Key(mode == InputMode.Headset ? "B" : "F")} Sit";

    string SeatedText(InputMode mode) => mode switch
    {
        InputMode.Desktop => $"{Key("Click")} Tattoo  {Key("P")} Put back\n{Key("F")} Stand  {Key("J")} Done\n{Key("K")} Exit mouse mode",
        InputMode.Simulator => $"{Key("Click")} Tattoo  {Key("G")} Put back\n{Key("F")} Stand  {Key("J")} Done\n{Key("K")} Mouse mode",
        _ => $"{Key("Trigger")} Tattoo  {Key("Grip")} Put back\n{Key("B")} Stand\nPress DONE to finish",
    };

    void SetText(string text)
    {
        if (label == null || text == shownText)
            return;
        shownText = text;
        label.text = text;
        if (panel != null)
            panel.size = label.GetPreferredValues(text) + padding * 2f;
    }

    void Place(bool seated)
    {
        var anchor = seated ? seatedAnchor : standingAnchor;
        if (anchor == null)
            return;
        transform.position = anchor.position;
        if (seated)
        {
            transform.rotation = anchor.rotation;
            return;
        }

        // Standing: turn about the vertical axis to face the player (text faces -Z).
        var cam = Camera.main;
        if (cam == null)
            return;
        Vector3 away = transform.position - cam.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
    }

    void Apply(float a)
    {
        if (panel != null)
            panel.alpha = a;
        if (label != null)
        {
            label.alpha = a;
            label.enabled = a > 0.001f;
        }
    }
}
