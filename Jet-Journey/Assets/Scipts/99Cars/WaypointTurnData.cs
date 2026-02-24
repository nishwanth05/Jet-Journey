using UnityEngine;

public class WaypointTurnData : MonoBehaviour
{
    public enum TurnDirection
    {
        Straight,
        Left,
        Right
    }

    [Header("Turn Control")]
    public TurnDirection direction = TurnDirection.Straight;

    [Range(0f, 1f)]
    public float turnAmount = 0.6f;   // how strong the steering is

    [Range(0.6f, 1f)]
    public float speedMultiplier = 0.85f; // slowdown on this turn
}