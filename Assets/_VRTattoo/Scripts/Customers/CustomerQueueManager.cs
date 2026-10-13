using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Keeps the shop filled with customers: one on the bed and one per waiting seat.
// CompleteTattoo() sends the bed customer home and moves everyone up the queue.
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
    public Key completeKey = Key.J;

    [Header("Events")]
    public UnityEvent<Customer> onCustomerLayDown;
    public UnityEvent<Customer> onCustomerLeft;

    // Slot 0 is the bed, slot i (i >= 1) is waitSeats[i - 1]. A customer owns its slot from the
    // moment it is sent there, even while still walking.
    Customer[] slots;
    float nextSpawnTime;

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
        if (keyboard != null && keyboard[completeKey].wasPressedThisFrame)
            CompleteTattoo();
#endif

        if (Time.time >= nextSpawnTime && FirstFreeSlot() >= 0)
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

        var leaving = slots[0];
        slots[0] = null;
        leaving.WalkTo(door, WaypointsBetween(bed, door), OnArrived);

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

    void Spawn()
    {
        int slot = FirstFreeSlot();
        var customer = Instantiate(customerPrefab, door.approach.position, door.approach.rotation, transform);
        customer.name = $"Customer ({Time.frameCount})";
        customer.PlaceAt(door);
        slots[slot] = customer;
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
