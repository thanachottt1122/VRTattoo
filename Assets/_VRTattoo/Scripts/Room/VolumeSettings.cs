using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public enum VolumeGroup { Master, Effects, Voice, Ambience }

// Player volume for the four GameAudio mixer groups (0-100%, saved in PlayerPrefs). Applied on Start:
// AudioMixer.SetFloat does not stick when called in Awake. Preview plays a short sample through the
// group being changed, a moment after the button click so the two do not overlap. Real time: works paused.
public class VolumeSettings : MonoBehaviour
{
    public static VolumeSettings Instance { get; private set; }

    public AudioMixer mixer;

    [Header("Exposed mixer parameters")]
    public string masterParameter = "MasterVolume";
    public string effectsParameter = "SFXVolume";
    public string voiceParameter = "VoiceVolume";
    public string ambienceParameter = "AmbienceVolume";

    [Header("Preview")]
    [Tooltip("Master preview plays through this group (SFX)")]
    public AudioMixerGroup effectsOutput;
    public AudioMixerGroup voiceOutput;
    public AudioMixerGroup ambienceOutput;
    public AudioClip masterPreview;
    public AudioClip effectsPreview;
    public AudioClip voicePreview;
    public AudioClip ambiencePreview;
    [Range(0f, 1f)] public float previewVolume = 0.6f;
    [Tooltip("Wait after the button click before the sample (s)")]
    public float previewDelay = 0.12f;
    [Tooltip("Longest a sample plays before fading out (s)")]
    public float previewMaxSeconds = 1.2f;
    public float previewFadeSeconds = 0.25f;

    const int Step = 10;
    readonly int[] percent = { 100, 100, 100, 100 };
    AudioSource previewSource;
    Coroutine previewRoutine;

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < percent.Length; i++)
            percent[i] = Mathf.Clamp(PlayerPrefs.GetInt(Key((VolumeGroup)i), 100), 0, 100);

        var go = new GameObject("Volume Preview");
        go.transform.SetParent(transform, false);
        previewSource = go.AddComponent<AudioSource>();
        previewSource.playOnAwake = false;
        previewSource.spatialBlend = 0f;
        previewSource.ignoreListenerPause = true;  // settings can be opened from the pause menu
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        for (int i = 0; i < percent.Length; i++)
            Apply((VolumeGroup)i);
        Debug.Log($"[Volume] Loaded Master {percent[0]}%, Effects {percent[1]}%, Voice {percent[2]}%, Ambience {percent[3]}%");
    }

    static string Key(VolumeGroup group) => "vol_" + group.ToString().ToLowerInvariant();

    public int Get(VolumeGroup group) => percent[(int)group];

    // Up or down one step; returns the new value.
    public int Change(VolumeGroup group, int steps)
    {
        Set(group, Get(group) + steps * Step);
        Preview(group);
        return Get(group);
    }

    public void Set(VolumeGroup group, int value)
    {
        value = Mathf.Clamp(Mathf.RoundToInt(value / (float)Step) * Step, 0, 100);
        percent[(int)group] = value;
        PlayerPrefs.SetInt(Key(group), value);
        PlayerPrefs.Save();
        float db = Apply(group);
        Debug.Log($"[Volume] {group} {value}% -> {db:0.0} dB");
    }

    float Apply(VolumeGroup group)
    {
        int p = percent[(int)group];
        float db = p <= 0 ? -80f : 20f * Mathf.Log10(p / 100f);
        string parameter = Parameter(group);
        if (mixer != null && !mixer.SetFloat(parameter, db))
            Debug.LogWarning($"[Volume] Mixer parameter '{parameter}' is not exposed");
        return db;
    }

    string Parameter(VolumeGroup group) => group switch
    {
        VolumeGroup.Master => masterParameter,
        VolumeGroup.Effects => effectsParameter,
        VolumeGroup.Voice => voiceParameter,
        _ => ambienceParameter,
    };

    public void Preview(VolumeGroup group)
    {
        AudioClip clip;
        AudioMixerGroup output;
        switch (group)
        {
            case VolumeGroup.Master: clip = masterPreview; output = effectsOutput; break;
            case VolumeGroup.Effects: clip = effectsPreview; output = effectsOutput; break;
            case VolumeGroup.Voice: clip = voicePreview; output = voiceOutput; break;
            default: clip = ambiencePreview; output = ambienceOutput; break;
        }
        if (clip == null)
            return;
        if (previewRoutine != null)
            StopCoroutine(previewRoutine);
        previewSource.Stop();
        previewRoutine = StartCoroutine(PlayPreview(clip, output));
    }

    IEnumerator PlayPreview(AudioClip clip, AudioMixerGroup output)
    {
        yield return new WaitForSecondsRealtime(previewDelay);
        previewSource.outputAudioMixerGroup = output;
        previewSource.clip = clip;
        previewSource.volume = previewVolume;
        previewSource.Play();

        float hold = Mathf.Min(clip.length, previewMaxSeconds) - previewFadeSeconds;
        if (hold > 0f)
            yield return new WaitForSecondsRealtime(hold);
        if (clip.length > previewMaxSeconds)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(previewFadeSeconds, 0.01f))
            {
                previewSource.volume = previewVolume * (1f - t);
                yield return null;
            }
            previewSource.Stop();
        }
        previewRoutine = null;
    }
}
