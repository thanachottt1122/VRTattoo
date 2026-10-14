using UnityEngine;

// On the Customer prefab: a 3D AudioSource that follows the head and plays the customer's voice
// lines: a greeting when walking in, an occasional pain sound while being tattooed and a reaction
// to the finished tattoo. The clip set comes from the character (CustomerCharacter.voiceOverride)
// or else by gender. Missing sets or empty slots play nothing.
public class CustomerVoice : MonoBehaviour
{
    [Header("Voice sets (Data/Voices)")]
    public CustomerVoiceSet maleVoice;
    public CustomerVoiceSet femaleVoice;

    [Header("3D sound")]
    public float minDistance = 0.5f;
    public float maxDistance = 8f;

    [Header("Greeting")]
    public bool greetOnArrival = true;
    public Vector2 greetingDelay = new Vector2(0.2f, 0.8f);

    [Header("Pain while being tattooed")]
    [Tooltip("Seconds of tattooing (in total since the last cry) before a cry is possible")]
    public float painMinTattooSeconds = 1.5f;

    [Tooltip("Random wait after a cry before the next one is possible (s)")]
    public Vector2 painCooldown = new Vector2(6f, 12f);

    [Tooltip("Chance per second of tattooing once the two conditions above are met")]
    [Range(0f, 1f)] public float painChancePerSecond = 0.35f;

    [Tooltip("The canvas counts as being tattooed if it was drawn on this recently (s)")]
    public float drawGrace = 0.2f;

    public bool debugLog;

    public CustomerVoiceSet Voice { get; private set; }
    public AudioSource Source { get; private set; }

    AnimatorCustomerVisual visual;
    CustomerTattooSkin skin;
    Transform head;
    AudioClip lastClip;
    float tattooSeconds;
    float nextPainTime;
    float greetTime = -1f;

    void Awake()
    {
        visual = GetComponent<AnimatorCustomerVisual>();
        skin = GetComponent<CustomerTattooSkin>();

        var go = new GameObject("Voice");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * 1.6f;
        Source = go.AddComponent<AudioSource>();
        Source.playOnAwake = false;
        Source.spatialBlend = 1f;
        Source.rolloffMode = AudioRolloffMode.Logarithmic;
        Source.minDistance = minDistance;
        Source.maxDistance = maxDistance;
        Source.dopplerLevel = 0f;
    }

    // After CustomerCharacterPicker (Awake) has created the character.
    void Start()
    {
        var character = visual != null ? visual.character : GetComponentInChildren<CustomerCharacter>();
        if (character != null && character.voiceOverride != null)
            Voice = character.voiceOverride;
        else
            Voice = character != null && character.gender == CustomerGender.Female ? femaleVoice : maleVoice;

        nextPainTime = Time.time + Random.Range(painCooldown.x, painCooldown.y) * 0.5f;
        if (greetOnArrival)
            greetTime = Time.time + Random.Range(greetingDelay.x, greetingDelay.y);
    }

    void Update()
    {
        if (greetTime >= 0f && Time.time >= greetTime)
        {
            greetTime = -1f;
            Play(CustomerVoiceLine.Greeting);
        }

        UpdatePain();
    }

    void LateUpdate()
    {
        if (head == null && visual != null && visual.animator != null && visual.animator.isHuman)
            head = visual.animator.GetBoneTransform(HumanBodyBones.Head);
        if (head != null)
            Source.transform.position = head.position;
    }

    void UpdatePain()
    {
        var canvas = skin != null && skin.IsReady ? skin.Canvas : null;
        if (canvas == null || Time.time - canvas.LastDrawTime > drawGrace)
            return;

        tattooSeconds += Time.deltaTime;
        if (tattooSeconds < painMinTattooSeconds || Time.time < nextPainTime || Source.isPlaying)
            return;
        if (Random.value >= painChancePerSecond * Time.deltaTime)
            return;

        Play(CustomerVoiceLine.Pain);
        tattooSeconds = 0f;
        nextPainTime = Time.time + Random.Range(painCooldown.x, painCooldown.y);
    }

    public void PlayMood(CustomerMood mood) => Play(CustomerVoiceSet.LineFor(mood));

    // Plays a random clip of the line. Returns false (silently) when there is nothing to play.
    public bool Play(CustomerVoiceLine line)
    {
        var clip = Voice != null ? Voice.Pick(line, lastClip) : null;
        if (debugLog)
            Debug.Log($"[CustomerVoice] {name}: {line} -> {(clip != null ? clip.name : "no clip")}");
        if (clip == null)
            return false;

        Source.Stop();
        Source.clip = clip;
        Source.volume = Voice.volume;
        Source.pitch = Random.Range(Voice.pitchRange.x, Voice.pitchRange.y);
        Source.Play();
        lastClip = clip;
        return true;
    }
}
