using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Sounds of the tattoo machine, 3D from the needle tip:
//   - the motor loop while the trigger is pressed (hand or Desktop Test Mode), fading in and out; silent while
//     the tip is in an ink cap or on a needle cartridge (the trigger refills / swaps there).
//     Every press picks another loop (never the same one twice in a row), starts it at a random point and
//     detunes it a little. Thin needles sound higher, wide ones lower. On the skin it gets a bit louder and
//     heavier and wavers with the stroke speed.
//   - snap + tighten when a needle cartridge is fitted (TattooNeedle)
//   - a random ink sound when a refill starts or the ink changes colour (TattooInk); a new colour sounds higher
// Runs after TattooInk / TattooNeedle so their cap and cartridge contacts are this frame's.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(TattooMachine))]
public class TattooMachineSound : MonoBehaviour
{
    public AudioMixerGroup output;

    [Header("3D sound")]
    public float minDistance = 0.3f;
    public float maxDistance = 6f;

    [Header("Motor loop")]
    public AudioClip[] loops;
    [Range(0f, 1f)] public float runVolume = 0.35f;
    [Tooltip("Volume while the needle touches the skin")]
    [Range(0f, 1f)] public float touchVolume = 0.5f;
    [Tooltip("Seconds from silent to runVolume")]
    public float fadeIn = 0.15f;
    [Tooltip("Seconds from runVolume to silent")]
    public float fadeOut = 0.25f;

    [Header("Motor pitch")]
    [Tooltip("Random detune per press (0.05 = ±5%)")]
    [Range(0f, 0.2f)] public float pressPitchRange = 0.05f;
    [Tooltip("Needle diameter that plays at normal pitch (m)")]
    public float referenceDiameter = 0.005f;
    [Tooltip("pitch × (referenceDiameter / diameter) ^ this: 3 mm ≈ +6%, 9 mm ≈ -7%")]
    public float needlePitchExponent = 0.12f;
    [Tooltip("Pitch while the needle is on the skin (the motor works harder)")]
    public float touchPitch = 0.97f;
    [Tooltip("Pitch waver at full stroke speed (0.02 = ±2%)")]
    public float waverDepth = 0.02f;
    [Tooltip("Waver cycles per second")]
    public float waverRate = 6f;
    [Tooltip("Stroke speed with the full waver (m/s)")]
    public float waverFullSpeed = 0.12f;
    [Tooltip("How fast the pitch follows the needle / skin changes (1/s)")]
    public float pitchFollow = 8f;

    [Header("Needle swap")]
    public AudioClip needleSnap;
    public AudioClip needleTighten;
    [Tooltip("Pause between the snap and the tighten (s)")]
    public float tightenDelay = 0.15f;
    [Range(0f, 1f)] public float needleVolume = 0.8f;

    [Header("Ink")]
    [Tooltip("One is picked at random for each refill, never the same one twice in a row")]
    public AudioClip[] inkClips;
    [Range(0f, 1f)] public float inkVolume = 0.8f;
    [Tooltip("Random pitch (0.1 = ±10%): a refill of the same colour is in the lower half, a new colour in the upper half")]
    [Range(0f, 0.3f)] public float inkPitchRange = 0.1f;
    [Tooltip("Gap between the two halves, so a new colour is always a little higher")]
    [Range(0f, 0.1f)] public float newColourGap = 0.02f;

    public bool debugLog;

    TattooMachine machine;
    TattooNeedle needle;
    TattooInk ink;
    // Two loop sources: a new press starts on the other one while the last one fades out.
    readonly AudioSource[] loopSources = new AudioSource[2];
    int activeLoop;
    bool wasRunning;
    AudioClip lastLoop;
    float pressPitch = 1f;
    float basePitch = 1f;
    Vector3 lastTipPosition;
    float strokeSpeed;
    AudioSource oneShotSource;
    AudioSource inkSource;
    AudioClip lastInkClip;
    bool wasRefilling;
    InkCap refillCap;
    Color knownColor;

    void Awake()
    {
        machine = GetComponent<TattooMachine>();
        needle = machine.needle != null ? machine.needle : GetComponent<TattooNeedle>();
        ink = machine.ink != null ? machine.ink : GetComponent<TattooInk>();

        for (int i = 0; i < loopSources.Length; i++)
        {
            loopSources[i] = CreateSource("MachineLoop" + (i + 1));
            loopSources[i].loop = true;
            loopSources[i].volume = 0f;
        }
        oneShotSource = CreateSource("MachineOneShots");
        inkSource = CreateSource("MachineInk");
        if (ink != null)
            knownColor = ink.color;
    }

    void OnEnable()
    {
        if (needle != null)
            needle.onChanged.AddListener(OnNeedleChanged);
    }

    void OnDisable()
    {
        if (needle != null)
            needle.onChanged.RemoveListener(OnNeedleChanged);
        foreach (var source in loopSources)
        {
            source.Stop();
            source.volume = 0f;
        }
        wasRunning = false;
    }

    AudioSource CreateSource(string sourceName)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(machine.tip != null ? machine.tip : transform, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
        source.outputAudioMixerGroup = output;
        return source;
    }

    void Update()
    {
        UpdateStrokeSpeed();
        UpdateLoop();
        UpdateInk();
    }

    // Smoothed tip speed while drawing (0 otherwise).
    void UpdateStrokeSpeed()
    {
        Vector3 tip = machine.tip != null ? machine.tip.position : transform.position;
        float speed = machine.IsDrawing && Time.deltaTime > 0f ? Vector3.Distance(tip, lastTipPosition) / Time.deltaTime : 0f;
        strokeSpeed = Mathf.Lerp(strokeSpeed, speed, 1f - Mathf.Exp(-10f * Time.deltaTime));
        lastTipPosition = tip;
    }

    void UpdateLoop()
    {
        bool running = machine.TriggerPressed && !machine.AtTrolley;
        if (running && !wasRunning)
            StartLoop();
        wasRunning = running;

        float target = running ? (machine.IsDrawing ? touchVolume : runVolume) : 0f;
        for (int i = 0; i < loopSources.Length; i++)
        {
            var source = loopSources[i];
            float sourceTarget = i == activeLoop ? target : 0f;
            float seconds = sourceTarget > source.volume ? fadeIn : fadeOut;
            float step = Mathf.Max(runVolume, 0.01f) / Mathf.Max(seconds, 0.01f) * Time.deltaTime;
            source.volume = Mathf.MoveTowards(source.volume, sourceTarget, step);
            if (source.volume <= 0f && source.isPlaying)
                source.Stop();
        }

        float needlePitch = needle != null && needle.diameter > 0f
            ? Mathf.Pow(referenceDiameter / needle.diameter, needlePitchExponent)
            : 1f;
        float targetPitch = pressPitch * needlePitch * (machine.IsDrawing ? touchPitch : 1f);
        basePitch = Mathf.Lerp(basePitch, targetPitch, 1f - Mathf.Exp(-pitchFollow * Time.deltaTime));
        float waver = waverDepth * Mathf.Clamp01(strokeSpeed / Mathf.Max(waverFullSpeed, 0.001f))
            * Mathf.Sin(Time.time * waverRate * 2f * Mathf.PI);
        loopSources[activeLoop].pitch = basePitch * (1f + waver);
    }

    void StartLoop()
    {
        var clip = Pick(loops, lastLoop);
        if (clip == null)
            return;

        // The previous press keeps fading out on its own source.
        activeLoop = 1 - activeLoop;
        var source = loopSources[activeLoop];
        source.Stop();
        source.clip = clip;
        source.volume = 0f;
        source.time = Random.Range(0f, clip.length * 0.95f);
        pressPitch = Random.Range(1f - pressPitchRange, 1f + pressPitchRange);
        float needlePitch = needle != null && needle.diameter > 0f
            ? Mathf.Pow(referenceDiameter / needle.diameter, needlePitchExponent)
            : 1f;
        basePitch = pressPitch * needlePitch;
        source.pitch = basePitch;
        source.Play();
        lastLoop = clip;

        if (debugLog)
            Debug.Log($"[TattooMachineSound] loop {clip.name} from {source.time:0.00}s, pitch {basePitch:0.000} " +
                      $"(press x{pressPitch:0.000}, needle {(needle != null ? needle.Label : "-")} x{needlePitch:0.000})");
    }

    // A refill starting, or moving to another cap while still refilling (a colour change).
    void UpdateInk()
    {
        if (ink == null)
            return;

        bool refilling = ink.IsRefilling;
        if (refilling && (!wasRefilling || ink.DippedCap != refillCap))
        {
            bool newColour = ink.DippedCap != null && ink.DippedCap.Color != knownColor;
            var clip = Pick(inkClips, lastInkClip);
            if (clip != null)
            {
                inkSource.pitch = newColour
                    ? Random.Range(1f + newColourGap / 2f, 1f + inkPitchRange)
                    : Random.Range(1f - inkPitchRange, 1f - newColourGap / 2f);
                inkSource.PlayOneShot(clip, inkVolume);
                lastInkClip = clip;
                if (debugLog)
                    Debug.Log($"[TattooMachineSound] ink {clip.name}, pitch {inkSource.pitch:0.000}{(newColour ? " (new colour)" : "")}");
            }
            if (ink.DippedCap != null)
                knownColor = ink.DippedCap.Color;
        }
        else if (!refilling)
        {
            knownColor = ink.color;
        }
        wasRefilling = refilling;
        refillCap = refilling ? ink.DippedCap : null;
    }

    void OnNeedleChanged()
    {
        StopAllCoroutines();
        StartCoroutine(NeedleSwap());
    }

    IEnumerator NeedleSwap()
    {
        PlayOneShot(needleSnap, needleVolume);
        yield return new WaitForSeconds(tightenDelay);
        PlayOneShot(needleTighten, needleVolume);
    }

    void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip != null)
            oneShotSource.PlayOneShot(clip, volume);
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
}
