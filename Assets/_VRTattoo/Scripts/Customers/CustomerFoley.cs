using UnityEngine;
using UnityEngine.Audio;

// On the Customer prefab: clothes rustling, 3D from the customer.
//   sitting down on a waiting seat: a random sitClip
//   lying down on the bed:          a random lieClip
//   while being tattooed:           a quiet lieClip after firstRustle, then every rustleInterval seconds of
//                                   tattooing (pauses between strokes don't count), and one straight away
//                                   when the customer flinches (CustomerPain)
// Never two at once, never while the customer is speaking or crying out (CustomerVoice) and never closer
// together than minGap; a skipped one is tried again next frame (a flinch only for flinchWait).
[RequireComponent(typeof(Customer))]
public class CustomerFoley : MonoBehaviour
{
    public AudioMixerGroup output;

    [Header("3D sound")]
    [Tooltip("Full volume within this distance (the artist's chair is about 1 m from the bed)")]
    public float minDistance = 1.5f;
    public float maxDistance = 10f;
    [Tooltip("Height of the sound above the customer's pivot (feet when standing, hips when sitting or lying)")]
    public float height = 0.3f;

    [Header("Clips")]
    public AudioClip[] sitClips;
    public AudioClip[] lieClips;
    [Range(0f, 1f)] public float settleVolume = 0.7f;

    [Header("While being tattooed")]
    [Range(0f, 1f)] public float rustleVolume = 0.5f;
    [Range(0f, 1f)] public float flinchVolume = 0.7f;
    [Tooltip("A flinch that can't play yet (another foley sound is playing) is dropped after this long (s)")]
    public float flinchWait = 1.5f;
    [Tooltip("Seconds of tattooing before the first rustle of a job")]
    public Vector2 firstRustle = new Vector2(2f, 4f);
    [Tooltip("Seconds of tattooing between rustles")]
    public Vector2 rustleInterval = new Vector2(6f, 12f);
    [Tooltip("The canvas counts as being tattooed if it was drawn on this recently (s)")]
    public float drawGrace = 0.2f;

    [Tooltip("Shortest time between two foley sounds (s)")]
    public float minGap = 1.5f;

    public bool debugLog;

    Customer customer;
    CustomerTattooSkin skin;
    CustomerPain pain;
    CustomerVoice voice;
    AudioSource source;
    AudioClip lastClip;
    bool wasSettled;
    float lastFlinchTime = float.NegativeInfinity;
    float lastPlayTime = float.NegativeInfinity;
    float rustleTimer;

    void Awake()
    {
        customer = GetComponent<Customer>();
        skin = GetComponent<CustomerTattooSkin>();
        pain = GetComponent<CustomerPain>();
        voice = GetComponent<CustomerVoice>();

        var go = new GameObject("Foley");
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

    void Update()
    {
        bool settled = customer.IsSettled;
        if (settled && !wasSettled && customer.Location != null && customer.Location.pose != null)
        {
            if (customer.Location.arrivalPose == CustomerPose.Sit)
                Play(sitClips, settleVolume, "sit");
            else if (customer.Location.arrivalPose == CustomerPose.Lie)
            {
                Play(lieClips, settleVolume, "lie down");
                rustleTimer = Random.Range(firstRustle.x, firstRustle.y);
            }
        }
        wasSettled = settled;

        UpdateTattooing();
    }

    void UpdateTattooing()
    {
        // A flinch while another sound plays waits for it, but goes stale after flinchWait.
        if (pain != null && pain.LastFlinchTime > lastFlinchTime)
        {
            if (Time.time - pain.LastFlinchTime > flinchWait)
                lastFlinchTime = pain.LastFlinchTime;
            else if (Play(lieClips, flinchVolume, "flinch"))
            {
                lastFlinchTime = pain.LastFlinchTime;
                rustleTimer = Random.Range(rustleInterval.x, rustleInterval.y);
            }
        }

        var canvas = skin != null && skin.IsReady ? skin.Canvas : null;
        if (canvas == null || Time.time - canvas.LastDrawTime > drawGrace)
            return;

        rustleTimer -= Time.deltaTime;
        if (rustleTimer <= 0f && Play(lieClips, rustleVolume, "rustle"))
            rustleTimer = Random.Range(rustleInterval.x, rustleInterval.y);
    }

    bool Play(AudioClip[] clips, float volume, string reason)
    {
        if (source.isPlaying || Time.time - lastPlayTime < minGap || (voice != null && voice.IsSpeaking))
            return false;
        var clip = Pick(clips);
        if (clip == null)
            return false;

        source.clip = clip;
        source.volume = volume;
        source.pitch = Random.Range(0.95f, 1.05f);
        source.Play();
        lastClip = clip;
        lastPlayTime = Time.time;

        if (debugLog)
        {
            var listener = FindFirstObjectByType<AudioListener>();
            float distance = listener != null ? Vector3.Distance(listener.transform.position, source.transform.position) : -1f;
            // Logarithmic rolloff: full volume inside minDistance, then minDistance / distance.
            float gain = distance > minDistance ? minDistance / distance : 1f;
            Debug.Log($"[CustomerFoley] {name}: {reason} -> {clip.name}, volume {volume:0.00}, " +
                      $"{distance:0.00} m from the listener (x{gain:0.00}), group {(output != null ? output.name : "none")}");
        }
        return true;
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
