using System.Collections;
using TMPro;
using UnityEngine;

// "+$50" that floats up off the job card, grows a little and fades out, then destroys itself.
// The card keeps an inactive template and plays copies of it (JobCard.SpawnPopup).
// The coin sound is played once when the popup starts (through ShopSounds' SFX group when there is one).
public class MoneyPopup : MonoBehaviour
{
    public TMP_Text label;
    public float seconds = 1.3f;
    [Tooltip("How far it rises (m)")]
    public float rise = 0.12f;
    public float startScale = 0.7f;
    public float endScale = 1.1f;

    [Header("Sound (optional)")]
    public AudioClip sound;
    [Range(0f, 1f)] public float volume = 0.7f;

    public void Play(string text, Color color)
    {
        gameObject.SetActive(true);
        label.text = text;
        label.color = color;
        if (sound != null)
        {
            if (ShopSounds.Instance != null)
                ShopSounds.Instance.PlayAt(sound, transform.position, volume);
            else
                AudioSource.PlayClipAtPoint(sound, transform.position, volume);
        }
        StartCoroutine(Rise(color));
    }

    IEnumerator Rise(Color color)
    {
        Vector3 start = transform.localPosition;
        for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
        {
            float e = 1f - (1f - t) * (1f - t);  // ease-out
            transform.localPosition = start + Vector3.up * (rise * e);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, e);
            // Pops in fast, holds, fades over the last 40 %.
            float a = Mathf.Clamp01(t / 0.1f) * Mathf.Clamp01((1f - t) / 0.4f);
            label.color = new Color(color.r, color.g, color.b, color.a * a);
            yield return null;
        }
        Destroy(gameObject);
    }
}
