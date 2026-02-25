using UnityEngine;

public class AICarSplineFollower : MonoBehaviour
{
    public static AICarSplineFollower Instance;

    public Transform[] waypoints;

    int index = 0;

    void Awake()
    {
        Instance = this;
    }

    public Vector3 GetForwardDirection()
    {
        if (waypoints.Length == 0) return Vector3.forward;
        return waypoints[index].forward;
    }

    public Vector3 GetCurrentWaypointPosition()
    {
        if (waypoints.Length == 0) return transform.position;
        return waypoints[index].position;
    }

    public void AdvanceWaypoint()
    {
        if (waypoints.Length == 0) return;
        index = (index + 1) % waypoints.Length;
    }

    public void SnapToSpline(Transform car)
    {
        if (waypoints.Length == 0) return;

        car.position = waypoints[index].position;
        car.rotation = Quaternion.LookRotation(waypoints[index].forward);
    }
}