using TMPro;
using UnityEngine;

// One row of the settings page: name, [-], level bar, percent, [+]. Steps of 10% (VolumeSettings).
// Buttons instead of a slider: much easier to hit with a controller ray in the headset.
public class VolumeRow : MonoBehaviour
{
    public VolumeGroup group;
    public TMP_Text label;
    public NeonButton minusButton;
    public NeonButton plusButton;
    public NeonBar bar;
    public TMP_Text percentText;
    [ColorUsage(true, true)] public Color barColor = NeonPanel.NeonPink;

    void OnEnable()
    {
        if (minusButton != null)
            minusButton.onPressed.AddListener(PressMinus);
        if (plusButton != null)
            plusButton.onPressed.AddListener(PressPlus);
        Refresh();
    }

    void OnDisable()
    {
        if (minusButton != null)
            minusButton.onPressed.RemoveListener(PressMinus);
        if (plusButton != null)
            plusButton.onPressed.RemoveListener(PressPlus);
    }

    void PressMinus() => Change(-1);
    void PressPlus() => Change(1);

    void Change(int steps)
    {
        if (VolumeSettings.Instance != null)
            VolumeSettings.Instance.Change(group, steps);
        Refresh();
    }

    public void Refresh()
    {
        int value = VolumeSettings.Instance != null ? VolumeSettings.Instance.Get(group) : 100;
        if (bar != null)
            bar.Set(value / 100f, barColor);
        if (percentText != null)
            percentText.text = value + "%";
    }
}
