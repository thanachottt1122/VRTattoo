using UnityEngine;
using UnityEngine.Events;

// The needle cartridge fitted on the tattoo machine: sets the line width the machine draws.
// Swapped by touching a NeedleCartridge with the needle tip and holding the trigger for swapHoldTime
// (the same gesture as refilling ink); a light buzz confirms the swap.
public class TattooNeedle : MonoBehaviour
{
    [Tooltip("Needle tip (leave empty to use TattooMachine.tip)")]
    public Transform tip;

    [Header("Fitted needle (the game starts with this one)")]
    public string needleName = "MEDIUM";
    public float diameter = 0.005f;
    public Color color = new Color(1f, 0.55f, 0.1f);

    [Tooltip("The tip counts as touching a cartridge when its trigger is within this distance (m)")]
    public float touchRadius = 0.005f;

    [Tooltip("Seconds of holding the trigger on a cartridge to swap to it")]
    public float swapHoldTime = 0.4f;

    [Header("Haptics")]
    [Range(0f, 1f)] public float holdHapticAmplitude = 0.08f;
    public float holdHapticDuration = 0.04f;
    public float holdHapticInterval = 0.1f;
    [Tooltip("Buzz when the swap is done")]
    [Range(0f, 1f)] public float swapHapticAmplitude = 0.3f;
    public float swapHapticDuration = 0.08f;

    [Header("Look on the machine")]
    [Tooltip("Coloured in the fitted needle's colour")]
    public Renderer[] colorRenderers;

    [Tooltip("Needle point model: its X/Z scale is set from the diameter")]
    public Transform tipVisual;
    [Tooltip("Tip model width per metre of needle diameter")]
    public float tipVisualScale = 0.5f;

    public UnityEvent onChanged;

    // The cartridge the needle tip is touching right now (null = none).
    public NeedleCartridge TouchedCartridge { get; private set; }

    // The cartridge being swapped to while the trigger is held (null = not swapping).
    public NeedleCartridge SwapTarget { get; private set; }

    // 0..1 of swapHoldTime.
    public float SwapProgress => SwapTarget != null ? Mathf.Clamp01(holdTime / Mathf.Max(swapHoldTime, 0.01f)) : 0f;

    public string Label => FormatLabel(needleName, diameter);

    public static string FormatLabel(string name, float diameter) => $"{name} {diameter * 1000f:0.#}mm";

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    TattooMachine machine;
    readonly Collider[] touchHits = new Collider[8];
    MaterialPropertyBlock block;
    float holdTime;
    float nextHapticTime;

    void Awake()
    {
        machine = GetComponent<TattooMachine>();
        if (tip == null && machine != null)
            tip = machine.tip;
        ApplyVisual();
    }

    void OnValidate() => ApplyVisual();

    bool IsFitted(NeedleCartridge cartridge) =>
        Mathf.Approximately(cartridge.diameter, diameter) && cartridge.needleName == needleName;

    void Update()
    {
        TouchedCartridge = FindTouchedCartridge();
        bool swapping = TouchedCartridge != null && machine != null && machine.TriggerPressed && !IsFitted(TouchedCartridge);
        if (!swapping)
        {
            SwapTarget = null;
            holdTime = 0f;
            return;
        }

        if (SwapTarget != TouchedCartridge)
        {
            SwapTarget = TouchedCartridge;
            holdTime = 0f;
        }

        holdTime += Time.deltaTime;
        if (holdTime < swapHoldTime)
        {
            if (Time.time >= nextHapticTime)
            {
                machine.SendHaptic(holdHapticAmplitude, holdHapticDuration);
                nextHapticTime = Time.time + holdHapticInterval;
            }
            return;
        }

        Fit(SwapTarget);
        machine.SendHaptic(swapHapticAmplitude, swapHapticDuration);
        SwapTarget = null;
        holdTime = 0f;
    }

    public void Fit(NeedleCartridge cartridge)
    {
        needleName = cartridge.needleName;
        diameter = cartridge.diameter;
        color = cartridge.Color;
        ApplyVisual();
        onChanged?.Invoke();
    }

    NeedleCartridge FindTouchedCartridge()
    {
        if (tip == null)
            return null;

        int count = Physics.OverlapSphereNonAlloc(tip.position, touchRadius, touchHits, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var cartridge = touchHits[i].GetComponentInParent<NeedleCartridge>();
            if (cartridge != null)
                return cartridge;
        }
        return null;
    }

    void ApplyVisual()
    {
        if (colorRenderers != null)
        {
            block ??= new MaterialPropertyBlock();
            foreach (var r in colorRenderers)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                r.SetPropertyBlock(block);
            }
        }

        if (tipVisual != null)
        {
            float width = diameter * tipVisualScale;
            tipVisual.localScale = new Vector3(width, tipVisual.localScale.y, width);
        }
    }
}
