using UnityEngine;
using UnityEngine.Audio;

// On the Customer prefab: a 3D AudioSource that follows the head and plays the customer's voice
// lines: a greeting when walking in, an occasional pain sound while being tattooed and a reaction
// to the finished tattoo. The clip set comes from the character (CustomerCharacter.voiceOverride)
// or else by gender. Missing sets or empty slots play nothing.
// Every customer has their own pitch; each line is detuned a little more (the set's pitchRange).
// Pain cries are louder the more it hurts (CustomerPain), loudest on a flinch, and never closer
// together than minPainGap.
// A reaction (PlayReaction: the verdict on the tattoo, or walking out angry) waits for the line that is
// playing to finish, and from then on the customer makes no pain cries or greetings. The reaction clip is
// never the one the previous customer said.
public class CustomerVoice : MonoBehaviour
{
    [Header("Voice sets (Data/Voices)")]
    public CustomerVoiceSet maleVoice;
    public CustomerVoiceSet femaleVoice;

    [Tooltip("This customer's own pitch, picked once (on top of the set's per-line pitchRange)")]
    public Vector2 customerPitchRange = new Vector2(0.94f, 1.06f);

    [Header("3D sound")]
    public AudioMixerGroup output;
    public float minDistance = 0.5f;
    public float maxDistance = 8f;

    [Tooltip("Pain cries are this many times louder at the artist's chair. Done by widening their full-volume " +
             "distance (not by boosting the samples), so they never clip; capped by the clip's own level when very close.")]
    [Range(1f, 3f)] public float painLoudness = 1.5f;

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

    [Header("Pain cry loudness")]
    [Tooltip("Volume of a cry at 0 % and at 100 % pain (CustomerPain)")]
    public Vector2 painVolume = new Vector2(0.5f, 0.85f);
    [Tooltip("Volume of the cry when the customer flinches (the loudest)")]
    [Range(0f, 1f)] public float flinchVolume = 1f;
    [Tooltip("Shortest time between two pain cries, flinches included (s)")]
    public float minPainGap = 3f;

    [Header("Reaction")]
    [Tooltip("A reaction waits at most this long for the line that is playing (a pain cry) to finish, then cuts it (s)")]
    public float reactionMaxWait = 2f;

    public bool debugLog;

    public CustomerVoiceSet Voice { get; private set; }
    public AudioSource Source { get; private set; }
    // Talking, crying out, or about to react (CustomerFoley keeps quiet meanwhile).
    public bool IsSpeaking => (Source != null && Source.isPlaying) || reactionPending;

    // Shared by all customers, so two customers in a row never say the same reaction.
    static AudioClip lastReactionClip;

    AnimatorCustomerVisual visual;
    CustomerTattooSkin skin;
    CustomerPain pain;
    Transform head;
    AudioClip lastClip;
    AudioClip lastPainClip;
    float voicePitch = 1f;
    float lastPainTime = float.NegativeInfinity;
    float tattooSeconds;
    float nextPainTime;
    float greetTime = -1f;
    bool reacted;
    bool reactionPending;
    CustomerVoiceLine reactionLine;
    float reactionAskedTime;

    void Awake()
    {
        visual = GetComponent<AnimatorCustomerVisual>();
        skin = GetComponent<CustomerTattooSkin>();
        pain = GetComponent<CustomerPain>();
        voicePitch = Random.Range(customerPitchRange.x, customerPitchRange.y);

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
        Source.outputAudioMixerGroup = output;
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
            if (!reacted)
                Play(CustomerVoiceLine.Greeting);
        }

        if (reactionPending)
            UpdateReaction();
        else if (!reacted)
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

        PlayPain(false);
    }

    // A pain cry, louder with more pain and loudest on a flinch (CustomerPain). Skipped (false) when the
    // last cry was less than minPainGap ago.
    public bool PlayPain(bool flinch)
    {
        if (reacted)
            return false;
        float hurt = pain != null ? pain.Pain / 100f : 0.5f;
        if (Time.time - lastPainTime < minPainGap)
        {
            if (debugLog)
                Debug.Log($"[CustomerVoice] {name}: pain cry ({(flinch ? "flinch" : $"{hurt * 100f:0}%")}) skipped, " +
                          $"last cry {Time.time - lastPainTime:0.0}s ago");
            return false;
        }

        float volume = flinch ? flinchVolume : Mathf.Lerp(painVolume.x, painVolume.y, hurt);
        if (!Play(CustomerVoiceLine.Pain, volume, flinch ? "flinch" : $"pain {hurt * 100f:0}%"))
            return false;
        lastPainTime = Time.time;
        tattooSeconds = 0f;
        nextPainTime = Time.time + Random.Range(painCooldown.x, painCooldown.y);
        return true;
    }

    public void PlayMood(CustomerMood mood) => PlayReaction(CustomerVoiceSet.LineFor(mood));

    // The customer's last word, `delay` seconds from now (e.g. once the stars are on the job card) and
    // after the current line ends (see reactionMaxWait). No pain cries from the moment this is called.
    public void PlayReaction(CustomerVoiceLine line, float delay = 0f)
    {
        reacted = true;
        reactionPending = true;
        reactionLine = line;
        reactionAskedTime = Time.time + Mathf.Max(0f, delay);
        greetTime = -1f;
    }

    void UpdateReaction()
    {
        float waited = Time.time - reactionAskedTime;
        if (waited < 0f || (Source.isPlaying && waited < reactionMaxWait))
            return;
        if (debugLog && Source.isPlaying)
            Debug.Log($"[CustomerVoice] {name}: cut {Source.clip.name} after waiting {waited:0.0}s");
        reactionPending = false;
        Play(reactionLine, 1f, waited > 0.05f ? $"reaction, waited {waited:0.0}s" : "reaction");
    }

    public bool Play(CustomerVoiceLine line) => Play(line, 1f, null);

    // Plays a random clip of the line (never the same pain cry twice in a row). Returns false (silently)
    // when there is nothing to play.
    bool Play(CustomerVoiceLine line, float volume, string reason)
    {
        bool isPain = line == CustomerVoiceLine.Pain;
        bool isReaction = line >= CustomerVoiceLine.Ecstatic;
        var previous = isPain ? lastPainClip : isReaction ? lastReactionClip : lastClip;
        var clip = Voice != null ? Voice.Pick(line, previous) : null;
        if (clip == null)
        {
            if (debugLog)
                Debug.Log($"[CustomerVoice] {name}: {line} -> no clip");
            return false;
        }

        // Logarithmic rolloff: full volume inside minDistance, then minDistance / distance.
        float loudness = isPain ? painLoudness : 1f;
        Source.minDistance = minDistance * loudness;
        Source.maxDistance = maxDistance * loudness;

        Source.Stop();
        Source.clip = clip;
        Source.volume = Voice.volume * volume;
        Source.pitch = voicePitch * Random.Range(Voice.pitchRange.x, Voice.pitchRange.y);
        Source.Play();
        lastClip = clip;
        if (isPain)
            lastPainClip = clip;
        if (isReaction)
            lastReactionClip = clip;

        if (debugLog)
        {
            var listener = FindFirstObjectByType<AudioListener>();
            float distance = listener != null ? Vector3.Distance(listener.transform.position, Source.transform.position) : -1f;
            float gain = Mathf.Min(1f, Source.minDistance / Mathf.Max(distance, 0.01f));
            float plainGain = Mathf.Min(1f, minDistance / Mathf.Max(distance, 0.01f));
            Debug.Log($"[CustomerVoice] {name}: {line}{(reason != null ? $" ({reason})" : "")} -> {clip.name}, " +
                      $"volume {Source.volume:0.00}, pitch {Source.pitch:0.000} (own {voicePitch:0.000}), " +
                      $"{distance:0.00} m from the listener, x{gain:0.00} (x{gain / plainGain:0.00} of a normal line), " +
                      $"group {(output != null ? output.name : "none")}");
        }
        return true;
    }
}
