using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// One customer. Walks in straight lines through a list of points and snaps into the pose of the
// location it arrived at. Where to go is decided by CustomerQueueManager.
public class Customer : MonoBehaviour
{
    public float walkSpeed = 1.1f;

    [Tooltip("Turning speed while walking (degrees per second)")]
    public float turnSpeed = 360f;

    [Tooltip("Leave empty to use the CustomerVisual on this object")]
    public CustomerVisual visual;

    // The location the customer is at, or walked away from if IsWalking.
    public CustomerLocation Location { get; private set; }
    public bool IsWalking => walkRoutine != null;
    public bool IsSettled { get; private set; }

    Coroutine walkRoutine;

    void Awake()
    {
        if (visual == null)
            visual = GetComponent<CustomerVisual>();
    }

    // Appear standing at a location's approach point (e.g. spawning at the door).
    public void PlaceAt(CustomerLocation location)
    {
        StopWalking();
        Location = location;
        IsSettled = false;
        transform.SetPositionAndRotation(location.approach.position, location.approach.rotation);
        SetPose(CustomerPose.Idle);
    }

    // Get up if needed, then walk through the waypoints to the target's approach point.
    public void WalkTo(CustomerLocation target, List<Vector3> waypoints, Action<Customer> onArrived)
    {
        StopWalking();
        if (IsSettled && Location != null && Location.approach != null)
            transform.SetPositionAndRotation(Location.approach.position, Location.approach.rotation);
        IsSettled = false;

        var points = new List<Vector3>(waypoints) { target.approach.position };
        walkRoutine = StartCoroutine(Walk(target, points, onArrived));
    }

    // Snap into the arrival pose of the current location (sit on the seat, lie on the bed).
    public void Settle()
    {
        if (Location == null)
            return;

        var target = Location.pose != null ? Location.pose : Location.approach;
        transform.SetPositionAndRotation(target.position, target.rotation);
        SetPose(Location.pose != null ? Location.arrivalPose : CustomerPose.Idle);
        IsSettled = true;
    }

    IEnumerator Walk(CustomerLocation target, List<Vector3> points, Action<Customer> onArrived)
    {
        SetPose(CustomerPose.Walk);
        if (visual != null)
            visual.SetMoveSpeed(walkSpeed);

        foreach (var point in points)
        {
            while (true)
            {
                Vector3 toPoint = point - transform.position;
                Vector3 flat = new Vector3(toPoint.x, 0f, toPoint.z);
                if (flat.sqrMagnitude > 0.0001f)
                {
                    var look = Quaternion.LookRotation(flat);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
                }

                float step = walkSpeed * Time.deltaTime;
                if (toPoint.magnitude <= step)
                {
                    transform.position = point;
                    break;
                }
                transform.position += toPoint.normalized * step;
                yield return null;
            }
        }

        walkRoutine = null;
        Location = target;
        transform.rotation = target.approach.rotation;
        SetPose(CustomerPose.Idle);
        onArrived?.Invoke(this);
    }

    void StopWalking()
    {
        if (walkRoutine != null)
        {
            StopCoroutine(walkRoutine);
            walkRoutine = null;
        }
    }

    void SetPose(CustomerPose pose)
    {
        if (visual == null)
            return;
        visual.SetPose(pose);
        if (pose != CustomerPose.Walk)
            visual.SetMoveSpeed(0f);
    }
}
