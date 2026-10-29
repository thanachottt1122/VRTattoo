using UnityEngine;
using UnityEngine.Audio;

// Faint electrical hum of a light: a 3D loop through the Ambience group, only heard up close so it never
// covers the rest of the mix. Each instance starts at a random point in the clip so two buzzes don't phase.
// With a NeonFlicker, the hum dips together with the sign's blinks.
public class LightBuzz : MonoBehaviour
{
    public AudioClip clip;
    public AudioMixerGroup output;
    [Range(0f, 1f)] public float volume = 0.06f;
    [Tooltip("Full volume within this distance (m)")]
    public float minDistance = 0.4f;
    [Tooltip("Silent beyond this distance (m)")]
    public float maxDistance = 3f;

    [Tooltip("Optional: follow this sign's blinks")]
    public NeonFlicker flicker;
    [Tooltip("Volume multiplier at the sign's dimmest; brighter blinks scale between this and 1")]
    [Range(0f, 1f)] public float dimVolume = 0.15f;

    public bool debugLog;

    AudioSource source;
    float lastBrightness = 1f;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.outputAudioMixerGroup = output;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
        source.volume = volume;
    }

    void Start()
    {
        if (clip == null)
            return;
        source.time = Random.Range(0f, clip.length);
        source.Play();
        if (debugLog)
            Debug.Log($"[LightBuzz] {name}: {clip.name}, volume {volume:0.000}, {minDistance}-{maxDistance} m, " +
                      $"group {(output != null ? output.name : "none")}, flicker {(flicker != null ? flicker.name : "none")}");
    }

    void Update()
    {
        if (flicker == null)
            return;
        // Brightness runs from the sign's dimLevel (k = 0) to 1 (k = 1).
        float b = flicker.Brightness;
        float k = Mathf.InverseLerp(flicker.dimLevel, 1f, b);
        source.volume = volume * Mathf.Lerp(dimVolume, 1f, k);
        if (debugLog && !Mathf.Approximately(b, lastBrightness))
            Debug.Log($"[LightBuzz] {name}: brightness {b:0.00} -> volume {source.volume:0.000}");
        lastBrightness = b;
    }
}
