using UnityEngine;

// A needle cartridge on the trolley. Touching its trigger collider with the needle tip and holding the
// trigger fits this size of needle on the tattoo machine (TattooNeedle).
[RequireComponent(typeof(InkTint))]
public class NeedleCartridge : MonoBehaviour
{
    public string needleName = "MEDIUM";

    [Tooltip("Width of the line this needle draws (m)")]
    public float diameter = 0.005f;

    InkTint tint;

    public Color Color => (tint != null ? tint : tint = GetComponent<InkTint>()).color;

    public string Label => TattooNeedle.FormatLabel(needleName, diameter);
}
