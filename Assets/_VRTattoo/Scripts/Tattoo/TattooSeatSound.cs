using UnityEngine;
using UnityEngine.Audio;

// On the artist chair: a 3D chair creak when the player sits down or stands up (TattooSeat).
// sitClip is used when set (Foley/chair_sit); otherwise a random fallbackClip (moving on the chair).
// Standing up plays the same sound a little higher so the two are easy to tell apart.
[RequireComponent(typeof(TattooSeat))]
public class TattooSeatSound : MonoBehaviour
{
    public AudioMixerGroup output;

    [Header("Clips")]
    [Tooltip("Used when set")]
    public AudioClip sitClip;
    [Tooltip("Picked at random when there is no sitClip")]
    public AudioClip[] fallbackClips;

    [Range(0f, 1f)] public float volume = 0.8f;
    public float sitPitch = 1f;
    public float standPitch = 1.12f;
    [Tooltip("Random pitch change added on top (±)")]
    public float pitchJitter = 0.03f;

    [Header("3D sound")]
    public float minDistance = 1f;
    public float maxDistance = 8f;
    [Tooltip("Height of the sound above the chair's pivot (m)")]
    public float height = 0.4f;

    public bool debugLog;

    TattooSeat seat;
    AudioSource source;
    AudioClip lastClip;
    bool wasSeated;

    void Awake()
    {
        seat = GetComponent<TattooSeat>();

        var go = new GameObject("SeatSound");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * height;
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
        source.outputAudioMixerGroup = output;
    }

    void Start() => wasSeated = seat.IsSeated;

    void Update()
    {
        bool seated = seat.IsSeated;
        if (seated != wasSeated)
            Play(seated ? sitPitch : standPitch, seated ? "sit" : "stand");
        wasSeated = seated;
    }

    void Play(float pitch, string reason)
    {
        var clip = sitClip != null ? sitClip : Pick(fallbackClips);
        if (clip == null)
            return;

        source.pitch = pitch + Random.Range(-pitchJitter, pitchJitter);
        source.PlayOneShot(clip, volume);
        lastClip = clip;

        if (debugLog)
            Debug.Log($"[TattooSeatSound] {reason} -> {clip.name}, pitch {source.pitch:0.00}, volume {volume:0.00}, " +
                      $"at {source.transform.position}, group {(output != null ? output.name : "none")}");
    }

    // A random non-null clip, avoiding the last one when there is a choice.
    AudioClip Pick(AudioClip[] clips)
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
            if (clip != lastClip)
                return clip;
            fallback = clip;
        }
        return fallback;
    }
}
