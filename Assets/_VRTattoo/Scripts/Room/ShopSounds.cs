using UnityEngine;
using UnityEngine.Audio;

// The shop's own sounds (one in the scene):
//   - room ambience, a quiet 2D loop for the whole game (Ambience group)
//   - UI: a click for every NeonButton, a quieter tick when one starts being pointed at (own source with a
//     random pitch, so it never retunes or cuts the click), a chime for 3 stars (2D)
//   - the door bell when a day starts and when a customer comes in, the door itself (random clip) when a
//     customer comes in or walks out (3D at the door's approach point; the Door location's own transform
//     sits at the room origin). An enter is skipped while the door is still sounding; a leave never is.
//   - a jingle when Game Complete shows (2D), a little quieter for grade C
// PlayAt plays any clip at a point through the SFX group (MoneyPopup's coin).
public class ShopSounds : MonoBehaviour
{
    public static ShopSounds Instance { get; private set; }

    [Header("Mixer groups")]
    public AudioMixerGroup ambienceOutput;
    public AudioMixerGroup sfxOutput;

    [Header("Sources")]
    public CustomerQueueManager queue;
    public ShopDayManager day;
    public JobCard card;

    [Header("Ambience")]
    public AudioClip ambience;
    [Range(0f, 1f)] public float ambienceVolume = 0.25f;

    [Header("UI")]
    public AudioClip click;
    [Range(0f, 1f)] public float clickVolume = 0.6f;
    public AudioClip hover;
    [Tooltip("Keep well under clickVolume: ui_hover is denser than ui_click, so equal volumes sound louder")]
    [Range(0f, 1f)] public float hoverVolume = 0.15f;
    [Tooltip("Random pitch of the hover tick, +/- this much")]
    [Range(0f, 0.3f)] public float hoverPitchJitter = 0.06f;
    public AudioClip threeStars;
    [Range(0f, 1f)] public float threeStarsVolume = 0.8f;

    [Header("Door bell")]
    public AudioClip bell;
    [Range(0f, 1f)] public float bellVolume = 0.35f;
    [Tooltip("A second ring this soon after the last one is skipped (s)")]
    public float bellCooldown = 1f;
    public float bellMinDistance = 1f;
    public float bellMaxDistance = 15f;

    [Header("Door")]
    [Tooltip("One at random when a customer comes in (the bell follows) or walks out")]
    public AudioClip[] doorClips;
    [Range(0f, 1f)] public float doorVolume = 0.35f;
    [Tooltip("The bell rings this long after the door starts opening (s)")]
    public float bellAfterDoor = 0.25f;
    [Tooltip("Full volume within this distance (m); the clips are quiet, so wider than the bell")]
    public float doorMinDistance = 2.5f;
    [Tooltip("Height of the door sound above the door's floor point (m)")]
    public float doorHeight = 1.2f;

    [Header("Game complete")]
    public AudioClip gameComplete;
    [Range(0f, 1f)] public float gameCompleteVolume = 0.8f;
    [Tooltip("Volume multiplier for grade C (S, A and B play at full volume)")]
    [Range(0f, 1f)] public float gradeCVolumeScale = 0.75f;

    public bool debugLog;

    AudioSource ambienceSource;
    AudioSource uiSource;
    AudioSource hoverSource;
    AudioSource bellSource;
    AudioSource doorSource;
    AudioClip lastDoorClip;
    float lastBellTime = float.NegativeInfinity;

    void Awake()
    {
        Instance = this;

        ambienceSource = CreateSource("Ambience", ambienceOutput, 0f);
        ambienceSource.clip = ambience;
        ambienceSource.loop = true;
        ambienceSource.volume = ambienceVolume;
        if (ambience != null)
            ambienceSource.Play();

        uiSource = CreateSource("UI", sfxOutput, 0f);
        hoverSource = CreateSource("UI Hover", sfxOutput, 0f);

        bellSource = CreateSource("Bell", sfxOutput, 1f);
        bellSource.minDistance = bellMinDistance;
        bellSource.maxDistance = bellMaxDistance;
        doorSource = CreateSource("Door", sfxOutput, 1f);
        doorSource.minDistance = doorMinDistance;
        doorSource.maxDistance = bellMaxDistance;
        bellSource.transform.position = DoorPoint + Vector3.up * (doorHeight + 0.8f);
        doorSource.transform.position = DoorPoint + Vector3.up * doorHeight;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        NeonButton.AnyPressed += OnButtonPressed;
        NeonButton.AnyHoverStarted += OnButtonHovered;
        if (queue != null)
        {
            queue.onCustomerEntered.AddListener(OnCustomerEntered);
            queue.onCustomerLeft.AddListener(OnCustomerLeft);
        }
        if (day != null)
            day.onDayStarted.AddListener(RingBell);
        if (card != null)
            card.onThreeStars.AddListener(OnThreeStars);
    }

    void OnDisable()
    {
        NeonButton.AnyPressed -= OnButtonPressed;
        NeonButton.AnyHoverStarted -= OnButtonHovered;
        if (queue != null)
        {
            queue.onCustomerEntered.RemoveListener(OnCustomerEntered);
            queue.onCustomerLeft.RemoveListener(OnCustomerLeft);
        }
        if (day != null)
            day.onDayStarted.RemoveListener(RingBell);
        if (card != null)
            card.onThreeStars.RemoveListener(OnThreeStars);
    }

    AudioSource CreateSource(string sourceName, AudioMixerGroup output, float spatialBlend)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = 0f;
        source.outputAudioMixerGroup = output;
        return source;
    }

    void OnButtonPressed(NeonButton button)
    {
        if (click != null)
            uiSource.PlayOneShot(click, clickVolume);
    }

    void OnButtonHovered(NeonButton button)
    {
        if (hover == null)
            return;
        hoverSource.pitch = 1f + Random.Range(-hoverPitchJitter, hoverPitchJitter);
        hoverSource.PlayOneShot(hover, hoverVolume);
        if (debugLog)
            Debug.Log($"[ShopSounds] hover {button.name}, volume {hoverVolume:0.00}, pitch {hoverSource.pitch:0.000}, " +
                      $"group {(sfxOutput != null ? sfxOutput.name : "none")}");
    }

    void OnThreeStars()
    {
        if (threeStars != null)
            uiSource.PlayOneShot(threeStars, threeStarsVolume);
    }

    void OnCustomerEntered(Customer customer)
    {
        PlayDoor("enter");
        Invoke(nameof(RingBell), bellAfterDoor);
    }

    void OnCustomerLeft(Customer customer) => PlayDoor("leave");

    // Where customers stand at the door (falls back to the location itself).
    Vector3 DoorPoint
    {
        get
        {
            if (queue == null || queue.door == null)
                return transform.position;
            return queue.door.approach != null ? queue.door.approach.position : queue.door.transform.position;
        }
    }

    // Called by GameCompletePanel when it shows.
    public void PlayGameComplete(string grade)
    {
        if (gameComplete == null)
            return;
        float volume = gameCompleteVolume * (grade == "C" ? gradeCVolumeScale : 1f);
        uiSource.PlayOneShot(gameComplete, volume);
        Debug.Log($"[ShopSounds] game complete (grade {grade}) -> {gameComplete.name}, volume {volume:0.00}");
    }

    public void RingBell()
    {
        if (bell == null || Time.time - lastBellTime < bellCooldown)
            return;
        lastBellTime = Time.time;
        bellSource.PlayOneShot(bell, bellVolume);
        if (debugLog)
            Debug.Log($"[ShopSounds] bell, volume {bellVolume:0.00}");
    }

    // An enter is skipped while the door is still sounding (customers coming in back to back); a leave
    // always plays, on a one-shot source at the door if the main one is busy.
    void PlayDoor(string reason)
    {
        bool leaving = reason == "leave";
        if (doorSource.isPlaying && !leaving)
        {
            if (debugLog)
                Debug.Log($"[ShopSounds] door ({reason}) skipped: still playing {doorSource.clip.name}");
            return;
        }

        var clip = Pick(doorClips, lastDoorClip);
        if (clip == null)
        {
            Debug.LogWarning("[ShopSounds] door: no door clips assigned");
            return;
        }

        if (doorSource.isPlaying)
        {
            var extra = PlayAt(clip, doorSource.transform.position, doorVolume);
            extra.minDistance = doorMinDistance;
            extra.maxDistance = bellMaxDistance;
        }
        else
        {
            doorSource.clip = clip;
            doorSource.volume = doorVolume;
            doorSource.Play();
        }
        lastDoorClip = clip;
        if (debugLog)
            Debug.Log($"[ShopSounds] door ({reason}) -> {clip.name}, volume {doorVolume:0.00}, " +
                      $"group {(sfxOutput != null ? sfxOutput.name : "none")}");
    }

    // A random non-null clip, avoiding `previous` when there is a choice.
    static AudioClip Pick(AudioClip[] clips, AudioClip previous)
    {
        if (clips == null || clips.Length == 0)
            return null;
        int start = Random.Range(0, clips.Length);
        AudioClip fallback = null;
        for (int i = 0; i < clips.Length; i++)
        {
            var clip = clips[(start + i) % clips.Length];
            if (clip == null)
                continue;
            if (clip != previous)
                return clip;
            fallback = clip;
        }
        return fallback;
    }

    // Like AudioSource.PlayClipAtPoint, but through the SFX group.
    public AudioSource PlayAt(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null)
            return null;
        var source = CreateSource("OneShot", sfxOutput, 1f);
        source.transform.SetParent(null);
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.Play();
        Destroy(source.gameObject, clip.length + 0.1f);
        return source;
    }
}
