using System.Collections.Generic;
using UnityEngine;

public class AICarController : MonoBehaviour
{
    public enum MovementMode { FullPhysics, Simple, Fake }

    public MovementMode movementMode = MovementMode.FullPhysics;
    public enum AICarState { Racing, Crashed, Recovering }
    public enum AIPersonality { Racer, Challenger, Drifter }

    public AICarState currentState = AICarState.Racing;
    public AIPersonality personality;
    struct DriveSegment
    {
        public Vector3 startPos;   // absolute start position
        public Vector3 direction;
        public float distance;
    }
    List<DriveSegment> drivePlan = new List<DriveSegment>();
    int driveIndex = 0;
    float drivenDistance = 0f;

    Rigidbody rb;
    Transform player;
    TrackSpline spline;

    /* ================= SPEED ================= */
    [Header("Speed")]
    public float baseSpeed = 12f;
    public float catchUpBoost = 4f;
    public float leadBiasBoost = 1.5f;

    float speedMultiplier;
    float currentSpeed;

    /* ================= STEERING ================= */
    [Header("Steering")]
    public float steeringStrength = 6f;
    public float lookAheadDistance = 6f;

    /* ================= PATH ================= */
    Vector3[] path;
    int pathIndex;

    /* ================= DEBUG ================= */
    public bool drawPathGizmos = true;
    float laneOffset;

    float turnSmoothness;   // how many segments in curve
    float turnStrength;     // how wide the curve is

    [Header("Avoidance")]
    public float avoidDistance = 3f;
    public float avoidStrength = 2f;
    public LayerMask carLayer;

    [Header("Turning")]
    public float turnSpeed = 6f;        // how fast car rotates
    public float maxSteerAngle = 45f;   // max angle change per second
    public float steeringResponsiveness = 8f;

    [Header("Ground Stick")]
    public float groundCheckDistance = 3f;
    public float groundStickForce = 20f;
    public LayerMask groundLayer;
    public enum AISpeedTier
    {
        Slow,
        Normal,
        Fast,
        Elite
    }
    [Header("Speed Tier")]
    public AISpeedTier speedTier;

    [Range(0f, 1f)]
    public float catchPlayerBias = 0f; // higher = more aggressive catch-up
    Vector3 ApplyAvoidance(Vector3 desiredDir)
    {
        RaycastHit hit;

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.SphereCast(
            origin,
            0.6f,
            desiredDir,
            out hit,
            avoidDistance,
            carLayer
        ))
        {
            Vector3 away = Vector3.Cross(Vector3.up, hit.normal);
            return (desiredDir + away * avoidStrength).normalized;
        }

        return desiredDir;
    }
    void OnCollisionEnter(Collision col)
    {
        if (col.relativeVelocity.magnitude > 4f)
        {
            currentState = AICarState.Crashed;
            Invoke(nameof(Recover), 1.2f);
        }
    }

    void Recover()
    {
        currentState = AICarState.Racing;
    }
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        AssignSpeedTier();   // 🔥 NEW
        AssignPersonality();
        AssignLaneOffset();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }
    void Start()
    {
        spline = TrackSpline.instance;

        BuildPath();        // 1️⃣ build raw path
        BuildDrivePlan();   // 2️⃣ convert to drive commands
    }

    void FixedUpdate()
    {
        if (currentState != AICarState.Racing) return;
        if (drivePlan.Count == 0) return;

        UpdateSpeed();
        ExecuteDrivePlan();

        StickToGround();
    }

    void ExecuteDrivePlan()
    {
        if (driveIndex >= drivePlan.Count)
            driveIndex = 0;

        DriveSegment seg = drivePlan[driveIndex];

        Vector3 desiredDir = seg.direction;

        // 🔹 Avoid cars
        desiredDir = ApplyAvoidance(desiredDir);

        // 🔹 Smooth turning
        Vector3 forward = rb.linearVelocity.sqrMagnitude > 0.1f
            ? rb.linearVelocity.normalized
            : transform.forward;

        Vector3 smoothDir = SmoothSteer(forward, desiredDir);

        Vector3 desiredVelocity = smoothDir * currentSpeed;

        rb.linearVelocity = Vector3.Lerp(
            rb.linearVelocity,
            desiredVelocity,
            Time.fixedDeltaTime * 4f
        );

        // 🔹 Smooth rotation
        Quaternion targetRot = Quaternion.LookRotation(smoothDir, Vector3.up);
        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRot,
                Time.fixedDeltaTime * turnSpeed
            )
        );

        drivenDistance += rb.linearVelocity.magnitude * Time.fixedDeltaTime;

        if (drivenDistance >= seg.distance)
        {
            drivenDistance = 0f;
            driveIndex++;
        }
    }
    void AssignLaneOffset()
    {
        switch (personality)
        {
            case AIPersonality.Racer:
                laneOffset = Random.Range(-0.5f, 0.5f);
                break;

            case AIPersonality.Challenger:
                laneOffset = Random.Range(-1.0f, 1.0f);
                break;

            case AIPersonality.Drifter:
                laneOffset = Random.Range(-2.0f, 2.0f);
                break;
        }
    }
    void AssignPersonality()
    {
        float r = Random.value;

        if (r < 0.35f)
            personality = AIPersonality.Racer;
        else if (r < 0.6f)
            personality = AIPersonality.Challenger;
        else
            personality = AIPersonality.Drifter;

        switch (personality)
        {
            case AIPersonality.Racer:
                turnSpeed = 7f;
                steeringResponsiveness = 9f;
                maxSteerAngle = 35f;
                break;

            case AIPersonality.Challenger:
                turnSpeed = 6f;
                steeringResponsiveness = 7f;
                maxSteerAngle = 40f;
                break;

            case AIPersonality.Drifter:
                turnSpeed = 4f;
                steeringResponsiveness = 4f;
                maxSteerAngle = 55f;
                break;
        }
    }
    void AssignSpeedTier()
    {
        float r = Random.value;

        if (r < 0.25f)
            speedTier = AISpeedTier.Slow;
        else if (r < 0.6f)
            speedTier = AISpeedTier.Normal;
        else if (r < 0.85f)
            speedTier = AISpeedTier.Fast;
        else
            speedTier = AISpeedTier.Elite;

        switch (speedTier)
        {
            case AISpeedTier.Slow:
                speedMultiplier = Random.Range(0.85f, 0.95f);
                catchPlayerBias = 0.1f;
                break;

            case AISpeedTier.Normal:
                speedMultiplier = Random.Range(0.95f, 1.05f);
                catchPlayerBias = 0.3f;
                break;

            case AISpeedTier.Fast:
                speedMultiplier = Random.Range(1.05f, 1.15f);
                catchPlayerBias = 0.6f;
                break;

            case AISpeedTier.Elite:
                speedMultiplier = Random.Range(1.15f, 1.3f);
                catchPlayerBias = 1.0f; // 🔥 VERY aggressive
                break;
        }
    }
    /* ================= PATH BUILD ================= */
    void BuildPath()
    {
        if (!spline || spline.waypoints == null || spline.waypoints.Length == 0)
            return;

        path = new Vector3[spline.waypoints.Length];
        for (int i = 0; i < spline.waypoints.Length; i++)
            path[i] = spline.waypoints[i].position;
    }
    void BuildDrivePlan()
    {
        drivePlan.Clear();

        if (path == null || path.Length < 2)
            return;

        Vector3 pos = transform.position; // SAME AS GIZMO START

        for (int i = 0; i < path.Length - 1; i++)
        {
            Vector3 a = path[i];
            Vector3 b = path[i + 1];

            Vector3 dir = (b - a).normalized;
            float totalDist = Vector3.Distance(a, b);

            // Turn detection
            if (i > 0)
            {
                Vector3 prevDir = (a - path[i - 1]).normalized;
                float signedAngle = SignedTurnAngle(prevDir, dir);
                float absAngle = Mathf.Abs(signedAngle);

                if (absAngle > 3f)
                {
                    AddCurvedSegmentAbsolute(ref pos, prevDir, signedAngle, totalDist);
                    continue;
                }
            }

            AddStraightSegmentAbsolute(ref pos, dir, totalDist);
        }
    }
    void AddStraightSegmentAbsolute(ref Vector3 pos, Vector3 dir, float dist)
    {
        Vector3 right = Vector3.Cross(dir, Vector3.up);
        Vector3 finalDir = (dir + right * laneOffset * 0.1f).normalized;

        DriveSegment seg = new DriveSegment
        {
            startPos = pos,
            direction = finalDir,
            distance = dist
        };

        drivePlan.Add(seg);

        pos += finalDir * dist;   // accumulate EXACTLY like gizmo
    }
    void AddCurvedSegmentAbsolute(
     ref Vector3 pos,
     Vector3 fromDir,
     float signedAngle,
     float totalDistance)
    {
        int steps = Mathf.Max(2, Mathf.RoundToInt(turnSmoothness));
        float stepAngle = signedAngle / steps;
        float stepDistance = totalDistance / steps;

        Vector3 baseDir = fromDir;

        for (int i = 0; i < steps; i++)
        {
            // Rotate ONLY the base direction
            baseDir = Quaternion.AngleAxis(stepAngle, Vector3.up) * baseDir;

            // Apply offset ONLY to steering direction
            Vector3 right = Vector3.Cross(baseDir, Vector3.up);
            Vector3 steeringDir =
                (baseDir + right * laneOffset * turnStrength * 0.1f).normalized;

            drivePlan.Add(new DriveSegment
            {
                startPos = pos,
                direction = steeringDir,
                distance = stepDistance
            });

            // 🔥 Advance position using BASE direction ONLY
            pos += baseDir * stepDistance;
        }
    }
    void AddCurvedTurn(Vector3 center, Vector3 fromDir, Vector3 toDir, float angle)
    {
        int steps = Mathf.RoundToInt(turnSmoothness);

        float stepAngle = angle / steps;

        Vector3 axis = Vector3.up;

        for (int s = 1; s <= steps; s++)
        {
            Quaternion rot = Quaternion.AngleAxis(stepAngle * s, axis);
            Vector3 newDir = rot * fromDir;

            Vector3 right = Vector3.Cross(newDir, Vector3.up);
            Vector3 finalDir = (newDir + right * laneOffset * turnStrength * 0.1f).normalized;

            drivePlan.Add(new DriveSegment
            {
                direction = finalDir,
                distance = 2.5f   // small curve piece length
            });
        }
    }
    Vector3 SmoothSteer(Vector3 currentDir, Vector3 targetDir)
    {
        float angle = Vector3.SignedAngle(currentDir, targetDir, Vector3.up);

        // Clamp steering angle
        float clampedAngle = Mathf.Clamp(
            angle,
            -maxSteerAngle,
            maxSteerAngle
        );

        Quaternion steerRot = Quaternion.AngleAxis(
            clampedAngle * Time.fixedDeltaTime * steeringResponsiveness,
            Vector3.up
        );

        return steerRot * currentDir;
    }
    /* ================= SPEED ================= */

    void UpdateSpeed()
    {
        currentSpeed = baseSpeed * speedMultiplier;

        switch (personality)
        {
            case AIPersonality.Racer:
                currentSpeed *= 1.1f;
                break;

            case AIPersonality.Challenger:
                currentSpeed *= 1.05f;
                break;

            case AIPersonality.Drifter:
                currentSpeed *= 0.9f;
                break;
        }

        if (player)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            Vector3 toPlayer = (player.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, toPlayer);

            if (dot > 0.15f && distanceToPlayer < 40f)
            {
                float catchChance =
                    catchPlayerBias *
                    Mathf.InverseLerp(40f, 10f, distanceToPlayer);

                if (Random.value < catchChance * Time.fixedDeltaTime)
                {
                    currentSpeed += catchUpBoost;
                }
            }
        }

        currentSpeed = Mathf.Clamp(
            currentSpeed,
            baseSpeed * 0.8f,
            baseSpeed * 1.6f
        );
    }
    /* ================= MOVEMENT ================= */
    void AdvancePath()
    {
        if (path == null || path.Length == 0) return;

        if (Vector3.Distance(transform.position, path[pathIndex]) < lookAheadDistance)
        {
            pathIndex = (pathIndex + 1) % path.Length;
        }
    }
    float SignedTurnAngle(Vector3 from, Vector3 to)
    {
        float angle = Vector3.Angle(from, to);
        float sign = Mathf.Sign(Vector3.Cross(from, to).y);
        return angle * sign;
    }

    /* ================= DIRECTION ================= */
    Vector3 GetDesiredDirection()
    {
        if (path == null || path.Length == 0)
            return transform.forward;

        Vector3 target = path[pathIndex];

        // Offset the target sideways
        Vector3 forwardDir = (target - transform.position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forwardDir);

        target += right * laneOffset;

        return (target - transform.position).normalized;
    }
    Color GetPersonalityColor()
    {
        switch (personality)
        {
            case AIPersonality.Racer:
                // Fast, precise
                return Color.green;

            case AIPersonality.Challenger:
                // Competitive, aggressive
                return Color.yellow;

            case AIPersonality.Drifter:
                // Loose, unpredictable
                return new Color(0.6f, 0.3f, 1f); // purple
        }

        return Color.white;
    }
    void StickToGround()
    {
        RaycastHit hit;

        Vector3 origin = transform.position + Vector3.up * 1.5f;

        if (Physics.Raycast(origin, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            // Lock Y position to ground
            Vector3 pos = transform.position;
            pos.y = hit.point.y+ 1f;
            transform.position = pos;

            // Align car to ground normal
            Quaternion groundRotation =
                Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                groundRotation,
                Time.fixedDeltaTime * 8f
            );

            // Kill vertical velocity completely
            Vector3 vel = rb.linearVelocity;
            vel.y = 0f;
            rb.linearVelocity = vel;
        }
    }
    void OnDrawGizmos()
    {
        if (!drawPathGizmos)
            return;

        /* ================= WAYPOINT PATH (REFERENCE) ================= */

        if (spline && spline.waypoints != null && spline.waypoints.Length > 1)
        {
            Gizmos.color = Color.cyan;

            for (int i = 0; i < spline.waypoints.Length - 1; i++)
            {
                Vector3 a = spline.waypoints[i].position;
                Vector3 b = spline.waypoints[i + 1].position;

                Gizmos.DrawLine(a, b);
                Gizmos.DrawSphere(a, 0.25f);
            }
        }

        /* ================= DRIVE PLAN (ACTUAL AI PATH) ================= */

        if (drivePlan == null || drivePlan.Count == 0 || path == null || path.Length == 0)
            return;

        Gizmos.color = GetPersonalityColor();

        // IMPORTANT: start from first waypoint, not spawn
        Vector3 pos = transform.position;

        for (int i = 0; i < drivePlan.Count; i++)
        {
            DriveSegment seg = drivePlan[i];

            Vector3 nextPos = pos + seg.direction * seg.distance;

            Gizmos.DrawLine(pos, nextPos);
            Gizmos.DrawSphere(nextPos, 0.2f);

            pos = nextPos;
        }

        /* ================= CURRENT SEGMENT (PLAY MODE) ================= */

        if (Application.isPlaying && driveIndex < drivePlan.Count)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.35f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                transform.position,
                transform.position + drivePlan[driveIndex].direction * 4f
            );
        }
    }
}