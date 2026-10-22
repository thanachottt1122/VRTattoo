using System.Collections.Generic;
using UnityEngine;

// A walking route made of this object's children, in order. The start and end points are not
// part of the route (they come from the CustomerLocation the customer walks from and to);
// children are only the detour waypoints in between. A route with no children is a straight line.
public class CustomerRoute : MonoBehaviour
{
    public CustomerLocation from;
    public CustomerLocation to;

    public void AppendWaypoints(List<Vector3> points)
    {
        foreach (Transform child in transform)
            points.Add(child.position);
    }

    void OnDrawGizmos()
    {
        if (from == null || to == null || from.approach == null || to.approach == null)
            return;

        var points = new List<Vector3> { from.approach.position };
        AppendWaypoints(points);
        points.Add(to.approach.position);

        Gizmos.color = new Color(0.2f, 0.9f, 1f);
        for (int i = 0; i < points.Count - 1; i++)
            Gizmos.DrawLine(points[i] + Vector3.up * 0.05f, points[i + 1] + Vector3.up * 0.05f);
        for (int i = 1; i < points.Count - 1; i++)
            Gizmos.DrawWireSphere(points[i] + Vector3.up * 0.05f, 0.08f);
    }
}
