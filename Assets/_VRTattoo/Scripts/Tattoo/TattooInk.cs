using UnityEngine;
using UnityEngine.Events;

// Ink held by the tattoo machine. Used up by the length of line tattooed (not by time),
// refilled to full with a cap's colour by dipping the needle tip into an InkCap.
public class TattooInk : MonoBehaviour
{
    [Tooltip("Needle tip (leave empty to use TattooMachine.tip)")]
    public Transform tip;

    [Tooltip("Ink left (%)")]
    [Range(0f, 100f)] public float amount = 100f;

    public Color color = new Color(0.02f, 0.02f, 0.02f);

    [Tooltip("Length of line one full load of ink draws (m). A medium stencil takes about 1.5 loads.")]
    public float metersPerLoad = 1f;

    [Tooltip("The tip counts as dipped when an ink cap's trigger is within this distance (m)")]
    public float dipRadius = 0.005f;

    public UnityEvent onChanged;

    public bool IsEmpty => amount <= 0f;

    readonly Collider[] dipHits = new Collider[8];

    void Awake()
    {
        if (tip == null && TryGetComponent(out TattooMachine machine))
            tip = machine.tip;
    }

    void Update()
    {
        if (tip == null)
            return;

        int count = Physics.OverlapSphereNonAlloc(tip.position, dipRadius, dipHits, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var cap = dipHits[i].GetComponentInParent<InkCap>();
            if (cap != null)
            {
                Refill(cap.Color);
                break;
            }
        }
    }

    // Uses the ink for a line of this length. Returns false when there was no ink.
    public bool Consume(float meters)
    {
        if (IsEmpty)
            return false;

        amount = Mathf.Max(0f, amount - 100f * meters / Mathf.Max(metersPerLoad, 0.0001f));
        onChanged?.Invoke();
        return true;
    }

    public void Refill() => Refill(color);

    public void Refill(Color newColor)
    {
        if (amount >= 100f && newColor == color)
            return;

        amount = 100f;
        color = newColor;
        onChanged?.Invoke();
    }
}
