using UnityEngine;

// A cap of ink. Dipping the needle tip into its trigger collider refills the machine (TattooInk) with this colour.
[RequireComponent(typeof(InkTint))]
public class InkCap : MonoBehaviour
{
    public string inkName = "Black";

    InkTint tint;

    public Color Color => (tint != null ? tint : tint = GetComponent<InkTint>()).color;
}
