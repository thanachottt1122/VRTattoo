using UnityEngine;

// On the Customer prefab: how much the tattoo hurts right now (0-100 %), shown on the job card.
// It rises while being tattooed (faster with a big needle, when the needle stays on one spot and when
// it is dragged too fast) and falls once the needle stops. Every customer has their own tolerance.
//   above flinchThreshold: the customer flinches now and then (arm twitch, a small jump in the line,
//                          pain cry, buzz in the artist's hand)
//   at 100 %:              MaxedOut; TattooJobManager sends them out with 0 points
public class CustomerPain : MonoBehaviour
{
    [Header("Tolerance")]
    [Tooltip("Random per customer. Pain builds up divided by this (1.4 = takes 40% longer to hurt)")]
    public Vector2 toleranceRange = new Vector2(0.85f, 1.5f);

    [Header("Pain while tattooing (% per second)")]
    [Tooltip("Pain per second of tattooing with the reference needle")]
    public float baseRate = 1.2f;
    [Tooltip("Needle diameter for baseRate (m)")]
    public float referenceDiameter = 0.005f;
    [Tooltip("Bigger needles hurt more: rate x (diameter / reference) ^ this")]
    public float needleExponent = 1.5f;

    [Tooltip("The needle counts as on the same spot while it stays this close (m)")]
    public float dwellRadius = 0.006f;
    [Tooltip("Time on one spot before it starts to hurt more (s)")]
    public float dwellGrace = 0.8f;
    [Tooltip("Extra pain per second, growing by this much for every second on the same spot")]
    public float dwellRate = 8f;
    [Tooltip("Most extra pain per second from staying on one spot")]
    public float dwellMaxRate = 30f;

    [Tooltip("Dragging faster than this hurts more (m/s)")]
    public float fastSpeed = 0.12f;
    [Tooltip("Extra pain per second for each multiple of fastSpeed above it")]
    public float fastRate = 8f;
    [Tooltip("Most extra pain per second from fast strokes")]
    public float fastMaxRate = 20f;

    [Header("Recovery")]
    [Tooltip("Pain starts falling this long after the needle stops (s)")]
    public float decayDelay = 0.4f;
    [Tooltip("Pain lost per second when not being tattooed")]
    public float decayRate = 10f;

    [Header("Flinch")]
    [Range(0f, 100f)] public float flinchThreshold = 60f;
    [Tooltip("Random time between flinches while above the threshold (s)")]
    public Vector2 flinchInterval = new Vector2(1.5f, 3f);
    [Tooltip("How far the arm twitches (degrees at the shoulder)")]
    public float flinchAngle = 4f;
    public float flinchSeconds = 0.25f;
    [Tooltip("How far the line jumps during a flinch (m, 0 = never)")]
    public float flinchInkOffset = 0.0015f;
    [Range(0f, 1f)] public float flinchHapticAmplitude = 0.5f;
    public float flinchHapticDuration = 0.1f;

    public bool debugLog;

    public float Pain { get; private set; }
    public float Tolerance { get; private set; }
    public bool MaxedOut { get; private set; }
    public bool IsFlinching => Time.time < flinchEnd;
    // Time.time of the last flinch (the job card flashes the hurt face).
    public float LastFlinchTime { get; private set; } = float.NegativeInfinity;

    CustomerTattooSkin skin;
    AnimatorCustomerVisual visual;
    CustomerVoice voice;
    TattooCanvas canvas;
    TattooMachine machine;

    float lastStrokeTime = float.NegativeInfinity;
    Vector3 lastStrokePoint;
    float lastDiameter;
    float speed;
    Vector3 dwellAnchor;
    float dwellStart;
    float nextFlinchTime;
    float flinchEnd;
    Vector3 flinchAxis;
    int lastLoggedStep = -1;

    void Awake()
    {
        skin = GetComponent<CustomerTattooSkin>();
        visual = GetComponent<AnimatorCustomerVisual>();
        voice = GetComponent<CustomerVoice>();
        Tolerance = Random.Range(toleranceRange.x, toleranceRange.y);
    }

    void OnDisable() => Subscribe(null);

    // A new tattoo starts from no pain (the tolerance stays the customer's own).
    public void ResetPain()
    {
        Pain = 0f;
        MaxedOut = false;
        lastLoggedStep = -1;
        flinchEnd = 0f;
    }

    void Update()
    {
        Subscribe(skin != null && skin.IsReady ? skin.Canvas : null);
        if (MaxedOut)
            return;

        float now = Time.time;
        bool tattooing = canvas != null && now - lastStrokeTime <= 0.15f;
        if (tattooing)
        {
            float rate = baseRate * Mathf.Pow(Mathf.Max(lastDiameter, 0.0001f) / referenceDiameter, needleExponent);
            float dwell = now - dwellStart - dwellGrace;
            if (dwell > 0f)
                rate += Mathf.Min(dwellRate * dwell, dwellMaxRate);
            if (speed > fastSpeed)
                rate += Mathf.Min(fastRate * (speed / fastSpeed - 1f), fastMaxRate);
            Pain += rate / Mathf.Max(Tolerance, 0.01f) * Time.deltaTime;
        }
        else if (now - lastStrokeTime > decayDelay)
        {
            Pain -= decayRate * Time.deltaTime;
        }
        Pain = Mathf.Clamp(Pain, 0f, 100f);

        if (debugLog && Mathf.FloorToInt(Pain / 10f) != lastLoggedStep)
        {
            lastLoggedStep = Mathf.FloorToInt(Pain / 10f);
            Debug.Log($"[CustomerPain] {name}: {Pain:0}% (tolerance {Tolerance:0.00}, speed {speed:0.00} m/s, needle {lastDiameter * 1000f:0.#}mm)");
        }

        if (Pain >= 100f)
        {
            MaxedOut = true;
            Debug.Log($"[CustomerPain] {name}: 100% - had enough (tolerance {Tolerance:0.00})");
            return;
        }

        if (Pain >= flinchThreshold && tattooing && now >= nextFlinchTime)
            Flinch();
        if (canvas != null && !IsFlinching && canvas.Nudge != Vector2.zero)
            canvas.Nudge = Vector2.zero;
    }

    void Flinch()
    {
        LastFlinchTime = Time.time;
        flinchEnd = Time.time + flinchSeconds;
        nextFlinchTime = Time.time + Random.Range(flinchInterval.x, flinchInterval.y);
        flinchAxis = Random.onUnitSphere;

        if (canvas != null && flinchInkOffset > 0f)
            canvas.Nudge = Random.insideUnitCircle.normalized * flinchInkOffset;
        if (voice != null)
            voice.PlayPain(true);
        if (machine == null)
            machine = FindFirstObjectByType<TattooMachine>();
        if (machine != null)
            machine.SendHaptic(flinchHapticAmplitude, flinchHapticDuration);
        if (debugLog)
            Debug.Log($"[CustomerPain] {name}: flinch at {Pain:0}%");
    }

    // After the Animator and IK: twitch the tattooed arm a little. The skin collider is baked once,
    // so the needle keeps drawing where it was; only the visible arm (and ink on it) jerks.
    void LateUpdate()
    {
        if (!IsFlinching || skin == null || !skin.IsReady || visual == null || visual.animator == null || !visual.animator.isHuman)
            return;
        var shoulder = visual.animator.GetBoneTransform(skin.UsesLeftArm ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
        if (shoulder == null)
            return;
        float t = 1f - (flinchEnd - Time.time) / Mathf.Max(flinchSeconds, 0.001f);
        float amount = Mathf.Sin(t * Mathf.PI) * flinchAngle;
        shoulder.rotation = Quaternion.AngleAxis(amount, flinchAxis) * shoulder.rotation;
    }

    void Subscribe(TattooCanvas target)
    {
        if (canvas == target)
            return;
        if (canvas != null)
        {
            canvas.Stroked -= OnStroke;
            canvas.Nudge = Vector2.zero;
        }
        canvas = target;
        if (canvas != null)
            canvas.Stroked += OnStroke;
    }

    void OnStroke(Vector3 point, float diameter)
    {
        float now = Time.time;
        float dt = now - lastStrokeTime;
        if (dt > 0f && dt < 0.2f)
        {
            // Smoothed tip speed, so one jumpy frame does not count as a fast stroke.
            float instant = Vector3.Distance(point, lastStrokePoint) / dt;
            speed = Mathf.Lerp(speed, instant, 0.3f);
        }
        else if (dt >= 0.2f)
        {
            speed = 0f;
        }

        if (dt > decayDelay || Vector3.Distance(point, dwellAnchor) > dwellRadius)
        {
            dwellAnchor = point;
            dwellStart = now;
        }

        if (dt > 0f)
        {
            lastStrokeTime = now;
            lastStrokePoint = point;
        }
        lastDiameter = diameter;
    }
}
