using UnityEngine;
using UnityEngine.Events;

// Ink held by the tattoo machine. Used up by the length of line tattooed (not by time).
// Refilled by dipping the needle tip into an InkCap and holding the trigger: the ink rises over refillDuration.
// A cap of another colour first empties the old ink, then fills with the new colour.
public class TattooInk : MonoBehaviour
{
    [Tooltip("Needle tip (leave empty to use TattooMachine.tip)")]
    public Transform tip;

    [Tooltip("Ink left (%)")]
    [Range(0f, 100f)] public float amount = 100f;

    public Color color = new Color(0.02f, 0.02f, 0.02f);

    [Tooltip("Length of line one full load of ink draws (m). A medium stencil takes about half a load.")]
    public float metersPerLoad = 1.3f;

    [Tooltip("The tip counts as dipped when an ink cap's trigger is within this distance (m)")]
    public float dipRadius = 0.005f;

    [Tooltip("Seconds of holding the trigger in a cap to fill from empty to full")]
    public float refillDuration = 1.5f;

    [Header("Refill haptics")]
    [Range(0f, 1f)] public float refillHapticAmplitude = 0.15f;
    public float refillHapticDuration = 0.05f;
    public float refillHapticInterval = 0.1f;

    public UnityEvent onChanged;

    public bool IsEmpty => amount <= 0f;

    // The cap the needle tip is in right now (null = not dipped).
    public InkCap DippedCap { get; private set; }

    public bool IsRefilling { get; private set; }

    TattooMachine machine;
    readonly Collider[] dipHits = new Collider[8];
    float nextHapticTime;

    void Awake()
    {
        machine = GetComponent<TattooMachine>();
        if (tip == null && machine != null)
            tip = machine.tip;
    }

    void Update()
    {
        DippedCap = FindDippedCap();
        IsRefilling = DippedCap != null && machine != null && machine.TriggerPressed
            && (amount < 100f || DippedCap.Color != color);
        if (!IsRefilling)
            return;

        // Another colour: the old ink is flushed before the new one goes in.
        if (DippedCap.Color != color)
        {
            color = DippedCap.Color;
            amount = 0f;
        }

        amount = Mathf.Min(100f, amount + 100f * Time.deltaTime / Mathf.Max(refillDuration, 0.01f));
        onChanged?.Invoke();

        if (Time.time >= nextHapticTime)
        {
            machine.SendHaptic(refillHapticAmplitude, refillHapticDuration);
            nextHapticTime = Time.time + refillHapticInterval;
        }
    }

    InkCap FindDippedCap()
    {
        if (tip == null)
            return null;

        int count = Physics.OverlapSphereNonAlloc(tip.position, dipRadius, dipHits, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var cap = dipHits[i].GetComponentInParent<InkCap>();
            if (cap != null)
                return cap;
        }
        return null;
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
}
