using UnityEngine;

// A place a customer can be: the door, the bed or a waiting seat.
// Customers walk to `approach` (standing on the floor), then snap to `pose` if there is one.
public class CustomerLocation : MonoBehaviour
{
    [Tooltip("Standing point on the floor where walking ends and starts, facing the way to stand")]
    public Transform approach;

    [Tooltip("Where to snap after arriving (seat or bed surface). Empty = stay standing at the approach point")]
    public Transform pose;

    public CustomerPose arrivalPose = CustomerPose.Idle;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        if (approach != null)
        {
            Gizmos.DrawWireSphere(approach.position + Vector3.up * 0.05f, 0.15f);
            Gizmos.DrawLine(approach.position + Vector3.up * 0.05f, approach.position + Vector3.up * 0.05f + approach.forward * 0.3f);
        }
        if (pose != null)
        {
            Gizmos.DrawWireCube(pose.position, Vector3.one * 0.1f);
            Gizmos.DrawLine(pose.position, pose.position + pose.forward * 0.3f);
        }
    }
}
