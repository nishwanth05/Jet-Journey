using UnityEngine;

public class AICarSplineFollower : MonoBehaviour
{
    public static AICarSplineFollower Instance;

    public Transform[] waypoints;

    public int index = 0;

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
    public float GetXTurnDirection()
    {
        if (waypoints.Length < 2) return 0f;

        int nextIndex = (index + 1) % waypoints.Length;

        float currentX = waypoints[index].position.x;
        float nextX = waypoints[nextIndex].position.x;

        float deltaX = nextX - currentX;

        // dead zone to avoid jitter
        if (Mathf.Abs(deltaX) < 0.1f)
            return 0f;

        return Mathf.Sign(deltaX); // +1 = right, -1 = left
    }
}