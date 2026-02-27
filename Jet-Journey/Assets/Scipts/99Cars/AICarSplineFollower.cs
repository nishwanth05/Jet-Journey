using UnityEngine;

public class TrackSpline : MonoBehaviour
{
    public static TrackSpline instance;
    public Transform[] waypoints;

    private void Awake()
    {
        instance = this;
    }
}