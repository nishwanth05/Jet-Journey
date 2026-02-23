using System.Collections.Generic;
using UnityEngine;

public class PlayerCarController : MonoBehaviour
{
    public enum Axel { Front, Rear }

    [System.Serializable]
    public struct Wheel
    {
        public GameObject WheelModel;
        public WheelCollider wheelCollider;
        public Axel axel;
    }

    /* ================= SPEED ================= */
    [Header("Speed (Arcade Racing)")]
    public float cruiseSpeed = 12f;          // SAME as AICarController.baseSpeed
    public float raceBoost = 1.8f;            // small competitive boost
    public float maxSpeed = 16f;               // hard cap (never exceed AI too much)

    [Header("Motor")]
    public float motorPower = 1500f;
    public float brakeForce = 3000f;

    /* ================= RACE DETECTION ================= */
    [Header("Racing Detection")]
    public float raceDetectRadius = 6f;
    public LayerMask aiCarLayer;

    float targetSpeed;
    float smoothedTargetSpeed;

    /* ================= STEERING ================= */
    [Header("Steering")]
    public float maxSteerAngle = 30f;
    public float steerSmoothTime = 0.15f;
    public float steerReturnSpeed = 6f;
    public float highSpeedSteerFactor = 0.6f;

    float currentSteerAngle;
    float steerVelocity;

    /* ================= PHYSICS ================= */
    [Header("Physics")]
    public Vector3 centerOfMass;

    Rigidbody rb;
    float turnInput;

    public List<Wheel> Wheels;

    /* ================= SETUP ================= */

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass;
    }

    /* ================= INPUT ================= */

    void Update()
    {
        turnInput = Input.GetAxis("Horizontal");
        WheelAnimation();
    }

    /* ================= PHYSICS ================= */

    void FixedUpdate()
    {
        UpdateTargetSpeed();
        ApplyMotor();
        SteerSmooth();
    }

    /* ================= SPEED LOGIC ================= */

    void UpdateTargetSpeed()
    {
        // Base stable speed
        targetSpeed = cruiseSpeed;

        // Check for nearby AI cars to "race"
        Collider[] nearbyCars = Physics.OverlapSphere(
            transform.position,
            raceDetectRadius,
            aiCarLayer
        );

        foreach (Collider hit in nearbyCars)
        {
            if (!hit.CompareTag("AICar"))
                continue;

            Vector3 dir = hit.transform.position - transform.position;
            float forwardDot = Vector3.Dot(transform.forward, dir.normalized);

            // AI is ahead or side-by-side → racing pressure
            if (forwardDot > -0.2f)
            {
                targetSpeed += raceBoost;
                break;
            }
        }

        targetSpeed = Mathf.Clamp(targetSpeed, cruiseSpeed, maxSpeed);
    }

    void ApplyMotor()
    {
        float currentSpeed = rb.linearVelocity.magnitude;

        // Smooth speed convergence (NO snapping)
        smoothedTargetSpeed = Mathf.Lerp(
            smoothedTargetSpeed,
            targetSpeed,
            Time.fixedDeltaTime * 2.5f
        );

        float speedError = smoothedTargetSpeed - currentSpeed;
        float torque = speedError * motorPower;
        torque = Mathf.Clamp(torque, 0f, motorPower);

        foreach (var wheel in Wheels)
        {
            if (wheel.axel != Axel.Rear)
                continue;

            wheel.wheelCollider.motorTorque = torque;
            wheel.wheelCollider.brakeTorque = 0f;
        }

        // Hard clamp (arcade safety)
        if (currentSpeed > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }

    /* ================= SMOOTH STEERING ================= */

    void SteerSmooth()
    {
        float speed = rb.linearVelocity.magnitude;
        float speedFactor = Mathf.Lerp(1f, highSpeedSteerFactor, speed / maxSpeed);

        float targetSteer =
            turnInput * maxSteerAngle * speedFactor;

        currentSteerAngle = Mathf.SmoothDamp(
            currentSteerAngle,
            targetSteer,
            ref steerVelocity,
            steerSmoothTime
        );

        // Auto-centering
        if (Mathf.Abs(turnInput) < 0.01f)
        {
            currentSteerAngle = Mathf.Lerp(
                currentSteerAngle,
                0f,
                Time.fixedDeltaTime * steerReturnSpeed
            );
        }

        foreach (var wheel in Wheels)
        {
            if (wheel.axel == Axel.Front)
                wheel.wheelCollider.steerAngle = currentSteerAngle;
        }
    }

    /* ================= WHEEL VISUALS ================= */

    void WheelAnimation()
    {
        foreach (var wheel in Wheels)
        {
            Vector3 pos;
            Quaternion rot;
            wheel.wheelCollider.GetWorldPose(out pos, out rot);

            wheel.WheelModel.transform.position = pos;

            float steerY =
                (wheel.axel == Axel.Front)
                ? wheel.wheelCollider.steerAngle
                : 0f;

            float rollZ = rot.eulerAngles.x;

            wheel.WheelModel.transform.localRotation =
                Quaternion.Euler(0f, steerY, -rollZ);
        }
    }
}