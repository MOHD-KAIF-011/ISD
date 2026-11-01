using UnityEngine;
using UnityEngine.InputSystem;

// WheelCollider approach adapted from:
// https://github.com/ilisescu03/Unity---Car-Controller
// MIT License, Copyright (c) 2026 Ilisescu Adrian Corneliu.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Driving")]
    public float motorTorque = 1800f;
    public float maxSteeringAngle = 30f;
    public float steeringSpeed = 180f;
    public float maxForwardSpeed = 25f;
    public float maxReverseSpeed = 8f;
    public float brakeTorque = 3500f;
    public float rollingBrakeTorque = 250f;

    [Header("Stability")]
    public float centerOfMassHeight = -0.45f;
    public float antiRollForce = 5000f;

    [Header("Wheels")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    public float respawnHeight = -10f;

    private Rigidbody rb;
    private Vector2 input;
    private Vector3 spawnPoint;
    private float steeringAngle;

    private WheelCollider[] AllWheels => new[]
    {
        frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel
    };

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.centerOfMass = new Vector3(0f, centerOfMassHeight, 0f);
        spawnPoint = transform.position;

        EnsureWheelColliders();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        input = keyboard == null
            ? Vector2.zero
            : new Vector2(
                (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f));

        input = Vector2.ClampMagnitude(input, 1f);

        if (transform.position.y < respawnHeight)
        {
            rb.position = spawnPoint;
            rb.rotation = Quaternion.identity;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float throttle = input.y;
        bool braking = Mathf.Abs(throttle) < 0.01f ||
                       (Mathf.Sign(throttle) != Mathf.Sign(forwardSpeed) &&
                        Mathf.Abs(forwardSpeed) > 0.5f);

        steeringAngle = Mathf.MoveTowards(
            steeringAngle,
            input.x * maxSteeringAngle,
            steeringSpeed * Time.fixedDeltaTime);
        frontLeftWheel.steerAngle = steeringAngle;
        frontRightWheel.steerAngle = steeringAngle;

        float speedLimit = throttle >= 0f ? maxForwardSpeed : maxReverseSpeed;
        bool underSpeedLimit = Mathf.Abs(forwardSpeed) < speedLimit ||
                               Mathf.Sign(forwardSpeed) != Mathf.Sign(throttle);
        float torque = underSpeedLimit && !braking ? throttle * motorTorque : 0f;

        rearLeftWheel.motorTorque = torque;
        rearRightWheel.motorTorque = torque;

        float appliedBrake = braking
            ? brakeTorque
            : (Mathf.Abs(throttle) < 0.01f ? rollingBrakeTorque : 0f);
        foreach (WheelCollider wheel in AllWheels)
            wheel.brakeTorque = appliedBrake;

        ApplyAntiRoll(frontLeftWheel, frontRightWheel);
        ApplyAntiRoll(rearLeftWheel, rearRightWheel);
    }

    private void EnsureWheelColliders()
    {
        if (frontLeftWheel != null && frontRightWheel != null &&
            rearLeftWheel != null && rearRightWheel != null)
            return;

        frontLeftWheel = CreateWheel("FrontLeftWheel", new Vector3(-0.72f, 0.35f, 1.2f));
        frontRightWheel = CreateWheel("FrontRightWheel", new Vector3(0.72f, 0.35f, 1.2f));
        rearLeftWheel = CreateWheel("RearLeftWheel", new Vector3(-0.72f, 0.35f, -1.2f));
        rearRightWheel = CreateWheel("RearRightWheel", new Vector3(0.72f, 0.35f, -1.2f));
    }

    private WheelCollider CreateWheel(string wheelName, Vector3 localPosition)
    {
        GameObject wheelObject = new GameObject(wheelName);
        wheelObject.transform.SetParent(transform, false);
        wheelObject.transform.localPosition = localPosition;

        WheelCollider wheel = wheelObject.AddComponent<WheelCollider>();
        wheel.radius = 0.36f;
        wheel.suspensionDistance = 0.2f;
        JointSpring suspension = wheel.suspensionSpring;
        suspension.spring = 35000f;
        suspension.damper = 4500f;
        suspension.targetPosition = 0.5f;
        wheel.suspensionSpring = suspension;
        return wheel;
    }

    private void ApplyAntiRoll(WheelCollider left, WheelCollider right)
    {
        WheelHit leftHit;
        WheelHit rightHit;
        bool leftGrounded = left.GetGroundHit(out leftHit);
        bool rightGrounded = right.GetGroundHit(out rightHit);

        float travelDifference = 0f;
        if (leftGrounded)
            travelDifference += (left.transform.InverseTransformPoint(leftHit.point).y + left.suspensionDistance) / left.suspensionDistance;
        if (rightGrounded)
            travelDifference -= (right.transform.InverseTransformPoint(rightHit.point).y + right.suspensionDistance) / right.suspensionDistance;

        float antiRoll = travelDifference * antiRollForce;
        if (leftGrounded) rb.AddForceAtPosition(left.transform.up * -antiRoll, left.transform.position);
        if (rightGrounded) rb.AddForceAtPosition(right.transform.up * antiRoll, right.transform.position);
    }
}
