using UnityEngine;

// A thin horizontal gauge in the neon style: a dim track (NeonPanel) with a coloured fill (NeonPanel)
// that grows from the left. Both quads are children of this object, which sits at the bar's centre.
// Used on the job card for pain, time left and the waiting customers' patience.
[ExecuteAlways]
public class NeonBar : MonoBehaviour
{
    public NeonPanel track;
    public NeonPanel fill;

    [Tooltip("Full length of the bar (m)")]
    public float length = 0.2f;
    [Tooltip("Height of the bar (m)")]
    public float thickness = 0.012f;

    [Range(0f, 1f)] public float value = 1f;
    [ColorUsage(true, true)] public Color color = Color.white;

    void OnValidate() => Apply();
    void OnEnable() => Apply();

    public void Set(float newValue, Color newColor)
    {
        value = Mathf.Clamp01(newValue);
        color = newColor;
        Apply();
    }

    public void Apply()
    {
        if (track != null)
        {
            track.size = new Vector2(length, thickness);
            track.cornerRadius = thickness * 0.5f;
            track.transform.localPosition = new Vector3(0f, 0f, track.transform.localPosition.z);
        }
        if (fill == null)
            return;

        // Never shorter than its own height so the rounded ends stay round.
        float filled = Mathf.Max(thickness, length * value);
        fill.size = new Vector2(filled, thickness);
        fill.cornerRadius = thickness * 0.5f;
        fill.transform.localPosition = new Vector3((filled - length) * 0.5f, 0f, fill.transform.localPosition.z);
        fill.fillColor = new Color(color.r, color.g, color.b, 0.9f);
        fill.edgeColor = color * 1.3f;
        fill.alpha = value > 0.001f ? 1f : 0f;
        fill.Apply();
    }
}
