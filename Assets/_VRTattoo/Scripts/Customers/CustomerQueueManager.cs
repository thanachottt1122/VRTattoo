using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Keeps the shop filled with customers: one on the bed and one per waiting seat.
// CompleteTattoo() sends the bed customer home and moves everyone up the queue. A waiting customer
// whose patience (CustomerPatience) runs out leaves on their own and the queue moves up too.
// ShopDayManager limits how many customers come in per day (BeginDay) and how patient they are.
public class CustomerQueueManager : MonoBehaviour
{
    public Customer customerPrefab;

    public CustomerLocation door;
    public CustomerLocation bed;

    [Tooltip("Queue order: element 0 is next in line for the bed")]
    public CustomerLocation[] waitSeats;

    [Tooltip("Leave empty to use every CustomerRoute under this object")]
    public CustomerRoute[] routes;

    [Tooltip("Cleared every time a new customer lies down on the bed")]
    public TattooCanvas tattooCanvas;

    [Tooltip("Seconds between customers coming in through the door")]
    public float spawnInterval = 1.5f;

    [Header("Testing (Unity Editor only)")]
    [Tooltip("None when another script (TattooJobManager) decides when the tattoo is done")]
    public Key completeKey = Key.None;

    [Header("Events")]
    [Tooltip("A new customer came in through the door")]
    public UnityEvent<Customer> onCustomerEntered;
    public UnityEvent<Customer> onCustomerLayDown;
    public UnityEvent<Customer> onCustomerLeft;
    [Tooltip("A waiting customer ran out of patience and is leaving")]
    public UnityEvent<Customer> onCustomerGaveUp;

    // Slot 0 is the bed, slot i (i >= 1) is waitSeats[i - 1]. A customer owns its slot from the
    // moment it is sent there, even while still walking.
    Customer[] slots;
    float nextSpawnTime;
    int spawnsLeft = int.MaxValue;
    float patienceScale = 1f;
    int walkingOut;  // sent home, still walking to the door

    // Today's customers have all come in and left.
    public bool DayDone => spawnsLeft <= 0 && walkingOut == 0 && System.Array.TrueForAll(slots, c => c == null);

    // `count` more customers come in today; their patience is multiplied by `patience`.
    public void BeginDay(int count, float patience)
    {
        spawnsLeft = count;
        patienceScale = patience;
        nextSpawnTime = Time.time;
    }

    // Back to the main menu mid-day: everyone in the shop is removed and no one else comes in.
    public void ClearAll()
    {
        spawnsLeft = 0;
        walkingOut = 0;
        for (int i = 0; i < slots.Length; i++)
            slots[i] = null;
        foreach (var c in GetComponentsInChildren<Customer>(true))
            Destroy(c.gameObject);
        if (tattooCanvas != null)
            tattooCanvas.Clear();
    }

    public Customer CustomerOnBed => slots != null ? slots[0] : null;

    // True when the customer on the bed is lying down and can be tattooed.
    public bool IsCustomerReady
    {
        get
        {
            var c = CustomerOnBed;
            return c != null && c.IsSettled && c.Location == bed;
        }
    }

    void Awake()
    {
        slots = new Customer[1 + waitSeats.Length];
        if (routes == null || routes.Length == 0)
            routes = GetComponentsInChildren<CustomerRoute>();
    }

    void Update()
    {
#if UNITY_EDITOR
        var keyboard = Keyboard.current;
        if (completeKey != Key.None && keyboard != null && keyboard[completeKey].wasPressedThisFrame)
            CompleteTattoo();
#endif

        CheckWaitingPatience();

        if (spawnsLeft > 0 && Time.time >= nextSpawnTime && FirstFreeSlot() >= 0)
        {
            Spawn();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    public void CompleteTattoo()
    {
        if (!IsCustomerReady)
        {
            Debug.Log("[CustomerQueue] CompleteTattoo ignored: no customer lying on the bed yet");
            return;
        }

        SendHome(0);
    }

    // The customer in `slot` walks out of the door and everyone behind moves up.
    void SendHome(int slot)
    {
        var leaving = slots[slot];
        slots[slot] = null;
        walkingOut++;
        if (leaving.TryGetComponent(out CustomerPatience patience))
            patience.Stop();
        leaving.WalkTo(door, WaypointsBetween(leaving.Location, door), OnArrived);

        // Move everyone forward, keeping their order.
        var queue = new List<Customer>();
        foreach (var c in slots)
            if (c != null)
                queue.Add(c);
        for (int i = 0; i < slots.Length; i++)
            slots[i] = i < queue.Count ? queue[i] : null;

        // Customers still walking are re-routed when they arrive (see OnArrived).
        foreach (var c in queue)
            if (!c.IsWalking)
                SendToSlot(c);
    }

    // The customer who owns a waiting seat (0 = next in line), even while still walking there;
    // null when the seat is free. The job card shows their patience.
    public Customer WaitingCustomer(int waitSeat) => slots != null && waitSeat + 1 < slots.Length ? slots[waitSeat + 1] : null;

    void CheckWaitingPatience()
    {
        for (int slot = 1; slot < slots.Length; slot++)
        {
            var c = slots[slot];
            if (c == null || !c.IsSettled || !c.TryGetComponent(out CustomerPatience patience) || !patience.OutOfPatience)
                continue;
            Debug.Log($"[CustomerQueue] {c.name} waited too long and leaves");
            if (c.TryGetComponent(out CustomerEmote emote))
                emote.Show(CustomerMood.Angry);
            if (c.TryGetComponent(out CustomerVoice voice))
                voice.PlayReaction(CustomerVoiceLine.Angry);
            onCustomerGaveUp?.Invoke(c);
            SendHome(slot);
            return;  // the queue changed; check the rest next frame
        }
    }

    void Spawn()
    {
        int slot = FirstFreeSlot();
        var customer = Instantiate(customerPrefab, door.approach.position, door.approach.rotation, transform);
        customer.name = $"Customer ({Time.frameCount})";
        customer.PlaceAt(door);
        if (customer.TryGetComponent(out CustomerPatience patience))
            patience.scale = patienceScale;
        if (spawnsLeft != int.MaxValue)
            spawnsLeft--;
        slots[slot] = customer;
        onCustomerEntered?.Invoke(customer);
        SendToSlot(customer);
    }

    void SendToSlot(Customer customer)
    {
        var target = SlotLocation(System.Array.IndexOf(slots, customer));
        if (customer.Location == target)
            return;
        customer.WalkTo(target, WaypointsBetween(customer.Location, target), OnArrived);
    }

    void OnArrived(Customer customer)
    {
        int slot = System.Array.IndexOf(slots, customer);
        if (slot < 0)
        {
            // Not in the queue any more: this customer was leaving and reached the door.
            walkingOut--;
            onCustomerLeft?.Invoke(customer);
            Destroy(customer.gameObject);
            return;
        }

        if (customer.Location != SlotLocation(slot))
        {
            // The queue moved while this customer was walking.
            SendToSlot(customer);
            return;
        }

        customer.Settle();
        if (slot > 0 && customer.TryGetComponent(out CustomerPatience patience))
            patience.StartWaiting();
        if (slot == 0)
        {
            if (tattooCanvas != null)
                tattooCanvas.Clear();
            onCustomerLayDown?.Invoke(customer);
        }
    }

    CustomerLocation SlotLocation(int slot) => slot == 0 ? bed : waitSeats[slot - 1];

    int FirstFreeSlot()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null)
                return i;
        return -1;
    }

    // Detour waypoints between two locations. A route can be used in either direction;
    // with no route the customer walks straight.
    List<Vector3> WaypointsBetween(CustomerLocation from, CustomerLocation to)
    {
        var points = new List<Vector3>();
        foreach (var route in routes)
        {
            if (route == null)
                continue;
            if (route.from == from && route.to == to)
            {
                route.AppendWaypoints(points);
                return points;
            }
            if (route.from == to && route.to == from)
            {
                route.AppendWaypoints(points);
                points.Reverse();
                return points;
            }
        }
        return points;
    }
}
