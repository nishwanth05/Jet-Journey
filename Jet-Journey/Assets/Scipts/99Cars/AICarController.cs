using UnityEngine;
using System.Collections;

public class AICarController : MonoBehaviour
{
    public enum AICarState { Racing, Crashed, Recovering }
    public enum MovementMode { FullPhysics, Simple, Fake }

    public enum AIPersonality
    {
        Racer,        // competes with player
        Challenger,   // competes with nearby cars
        Blocker,      // tries to disturb
        Drifter       // just exists
    }

    public AICarState currentState = AICarState.Racing;
    public MovementMode movementMode = MovementMode.FullPhysics;
    public AIPersonality personality;

    Rigidbody rb;
    Transform player;
    AICarSplineFollower spline;

    /* ================= SPEED ================= */
    [Header("Speed")]
    public float baseSpeed = 12f;
    public float catchUpBoost = 4f;
    public float leadBiasBoost = 1.5f;

    float speedMultiplier;
    float currentSpeed;

    /* ================= STEERING ================= */
    [Header("Steering")]
    public float steeringStrength = 4f;
    public float lookAheadDistance = 6f;

    /* ================= LATERAL BEHAVIOR ================= */
    float lateralBias;        // fixed per car
    float lateralAggression;  // how hard it pushes sideways

    /* ================= VARIATION ================= */
    float wanderOffset;
    public float wanderStrength = 0.4f;
    public float wanderSpeed = 0.6f;

    /* ================= AVOIDANCE ================= */
    public float avoidRadius = 2.2f;
    public float avoidStrength = 1.2f;

    /* ================= SETUP ================= */

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        speedMultiplier = Random.Range(0.9f, 1.1f);
        wanderOffset = Random.Range(0f, 100f);

        AssignPersonality();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    void Start()
    {
        spline = AICarSplineFollower.Instance;
    }

    /* ================= PERSONALITY ================= */

    void AssignPersonality()
    {
        float r = Random.value;

        if (r < 0.35f)
            personality = AIPersonality.Racer;
        else if (r < 0.6f)
            personality = AIPersonality.Challenger;
        else if (r < 0.8f)
            personality = AIPersonality.Blocker;
        else
            personality = AIPersonality.Drifter;

        // Personality tuning
        switch (personality)
        {
            case AIPersonality.Racer:
                lateralBias = Random.Range(-0.05f, 0.05f);
                lateralAggression = 0.4f;
                break;

            case AIPersonality.Challenger:
                lateralBias = Random.Range(-0.2f, 0.2f);
                lateralAggression = 0.7f;
                break;

            case AIPersonality.Blocker:
                lateralBias = Random.Range(-0.4f, 0.4f);
                lateralAggression = 1.2f;
                break;

            case AIPersonality.Drifter:
                lateralBias = Random.Range(-0.6f, 0.6f);
                lateralAggression = 0.2f;
                break;
        }
    }

    /* ================= UPDATE ================= */

    void FixedUpdate()
    {
        if (currentState != AICarState.Racing) return;
        if (!spline) return;

        UpdateSpeed();

        Move();
    }

    /* ================= SPEED ================= */

    void UpdateSpeed()
    {
        // Base speed
        currentSpeed = baseSpeed * speedMultiplier;

        /* ================= TURN SLOWDOWN (X BASED) ================= */

        float xTurn = Mathf.Abs(spline.GetXTurnDirection()); // 0, 1
        currentSpeed *= Mathf.Lerp(1f, 0.75f, xTurn);

        /* ================= PLAYER INTERACTION ================= */

        if (player)
        {
            Vector3 toPlayer = player.position - transform.position;
            float dot = Vector3.Dot(transform.forward, toPlayer.normalized);

            switch (personality)
            {
                case AIPersonality.Racer:
                    if (dot > 0.2f)
                        currentSpeed += catchUpBoost;
                    break;

                case AIPersonality.Challenger:
                    currentSpeed += Mathf.Sin(Time.time + wanderOffset) * 0.5f;
                    break;

                case AIPersonality.Blocker:
                    currentSpeed += leadBiasBoost;
                    break;

                case AIPersonality.Drifter:
                    currentSpeed *= 0.9f;
                    break;
            }
        }

        /* ================= FINAL CLAMP ================= */

        currentSpeed = Mathf.Clamp(
            currentSpeed,
            baseSpeed * 0.8f,
            baseSpeed * 1.6f
        );
    }

    /* ================= MOVEMENT ================= */

    void Move()
    {
        rb.isKinematic = false;

        rb.MovePosition(
            rb.position + transform.forward * currentSpeed * Time.fixedDeltaTime
        );

        Vector3 desired = GetDesiredDirection();

        transform.forward = Vector3.Lerp(
            transform.forward,
            desired,
            steeringStrength * Time.fixedDeltaTime
        );

        if (Vector3.Distance(transform.position,
            spline.GetCurrentWaypointPosition()) < lookAheadDistance)
        {
            spline.AdvanceWaypoint();
        }
    }

    /* ================= DIRECTION ================= */
    Vector3 GetDesiredDirection()
    {
        Vector3 splineDir = spline.GetForwardDirection();

        // 🔁 X-based turn
        float xTurn = spline.GetXTurnDirection();
        Vector3 turnDir = transform.right * xTurn * 0.7f;

        // Personality lateral bias
        Vector3 lateral =
            transform.right * lateralBias * lateralAggression;

        // Wander
        float wander =
            (Mathf.PerlinNoise(Time.time * wanderSpeed, wanderOffset) - 0.5f)
            * wanderStrength;

        Vector3 wanderDir =
            Quaternion.AngleAxis(wander * 30f, Vector3.up) * splineDir;

        Vector3 avoid = GetAvoidanceVector();

        return (wanderDir + turnDir + lateral + avoid).normalized;
    }
    Vector3 GetAvoidanceVector()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, avoidRadius);
        Vector3 avoid = Vector3.zero;

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform) continue;
            if (!hit.CompareTag("AICar")) continue;

            Vector3 dir = transform.position - hit.transform.position;
            avoid += dir.normalized / Mathf.Max(dir.magnitude, 0.5f);
        }

        return avoid * avoidStrength;
    }
}