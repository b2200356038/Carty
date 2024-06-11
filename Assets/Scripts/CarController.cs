using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CarController : NetworkBehaviour
{
    [Header("Car Properties")] public float carSpeed;
    public bool isPlayerCar;

    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> carHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public Transform[] wheels;
    public Transform[] wheelMeshes;
    public Rigidbody carRigidbody;
    public float wheelRadius = 0.25f;
    public float carTopSpeed = 10f;
    public float speedFactor = 1000f;
    public float breakFactor = 1000f;

    public float rotationSpeed = 360f;
    public bool isBreaking;

    [Header("Car Jump Properties")] public int jumpFactor = 10;
    public float jumpTime = 1f;
    public int stretchFactor = 10;
    public bool isJump;

    [Header("Car Nitro Properties")] public float nitroAmount, maxNitroAmount = 3f;
    public float nitroFactor = 1000f;
    public AnimationCurve nitroCurve;

    [Header("Car Suspension Properties")] public float suspensionSpringForce = 20000f;
    public float suspensionDamperForce = 2000f;
    public float suspensionRestDistance = 0.5f;

    [Header("Car Steering Properties")] public NetworkVariable<int> wheelAngle =
        new NetworkVariable<int>(30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float wheelGripFactor = 0.5f;
    public float wheelMass = 20f;
    public AnimationCurve powerCurve, steeringCurve;
    public float maxAngle = 30f;

    [Header("Car Input")] public float accelerationInput;
    public float steeringInput;
    private NetworkObject _networkObject;

    [Header("PowerUp Properties")] public int powerUpCount;
    public int powerUpLimit;
    public List<PowerUpType> powerUps = new List<PowerUpType>();


    private void Update()
    {
        if (isPlayerCar)
        {
            accelerationInput = Input.GetAxis("Vertical");
            steeringInput = Input.GetAxis("Horizontal");
            if (Input.GetKey(KeyCode.Space) && jumpTime < 1f)
            {
                jumpTime += Time.deltaTime;
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                isJump = true;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                isReady.Value = !isReady.Value;
            }

            wheelAngle.Value = (int)(steeringInput * maxAngle);
        }

        wheels[0].localEulerAngles =
            new Vector3(wheels[0].localEulerAngles.x, wheelAngle.Value, wheels[0].localEulerAngles.z);
        wheels[1].localEulerAngles =
            new Vector3(wheels[1].localEulerAngles.x, wheelAngle.Value, wheels[1].localEulerAngles.z);
    }

    private void Start()
    {
        StartCoroutine(CalculateSpeed());
        _networkObject = GetComponent<NetworkObject>();
        if (_networkObject.IsOwner)
        {
            Application.targetFrameRate = 1000;
            isPlayerCar = true;
            SmoothCameraFollow.Instance.target = transform;
        }
        else
        {
            isPlayerCar = false;
        }
    }

    IEnumerator CalculateSpeed()
    {
        while (true)
        {
            Vector3 lastPosition = transform.position;
            yield return new WaitForFixedUpdate();
            carSpeed = ((transform.position - lastPosition).magnitude) / Time.fixedDeltaTime;
            if (Vector3.Dot(transform.forward, transform.position - lastPosition) < 0) carSpeed = -carSpeed;
        }
    }

    private void FixedUpdate()
    {
        if (isPlayerCar)
        {
            if (Input.GetKey(KeyCode.LeftShift) && nitroAmount > 0)
            {
                float availableNitroForce = nitroCurve.Evaluate(nitroAmount) * nitroFactor;
                carRigidbody.AddForce(transform.forward * availableNitroForce);
                nitroAmount -= Time.fixedDeltaTime;
                UIManager.Instance.UpdateNitroVisualizer(nitroAmount / maxNitroAmount);
            }
            else
            {
                nitroAmount = Mathf.Min(nitroAmount + Time.fixedDeltaTime, maxNitroAmount);
                UIManager.Instance.UpdateNitroVisualizer(nitroAmount / maxNitroAmount);
            }
        }

        int i = 0;
        foreach (Transform wheel in wheels)
        {
            wheelMeshes[i].Rotate(Vector3.right, carSpeed * Time.fixedDeltaTime * rotationSpeed);
            Debug.DrawRay(wheel.position, -wheel.up * wheelRadius, Color.red);
            RaycastHit hit;
            if (Physics.Raycast(wheel.position, -wheel.up, out hit, suspensionRestDistance))
            {
                var position = wheel.position;
                var up = wheel.up;
                wheelMeshes[i].position = position - (up * (hit.distance - wheelRadius));
                if (isPlayerCar)
                {
                    // Apply suspension force
                    Vector3 springDirection = up;
                    Vector3 wheelVelocity = carRigidbody.GetPointVelocity(position);
                    float offset = suspensionRestDistance - hit.distance;
                    float vel = Vector3.Dot(springDirection, wheelVelocity);
                    float force = (suspensionSpringForce * offset) - (suspensionDamperForce * vel);
                    carRigidbody.AddForceAtPosition(springDirection * force, position);

                    // Apply wheel rotation
                    Vector3 steeringDirection = wheel.right;
                    float steeringVelocity = Vector3.Dot(steeringDirection, wheelVelocity);
                    float desiredVelocityChange = -steeringVelocity * wheelGripFactor;
                    float desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime;
                    carRigidbody.AddForceAtPosition(steeringDirection * (desiredAcceleration * wheelMass), position);

                    //Apply acceleration/brake force
                    Vector3 accelerationDirection = wheel.forward;
                    float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / carTopSpeed);
                    if (!isBreaking) wheelGripFactor = steeringCurve.Evaluate(normalizedSpeed);
                    UIManager.Instance.UpdateSpeedometer((int)carSpeed, normalizedSpeed);
                    if (accelerationInput > 0 && normalizedSpeed <= 1f)
                    {
                        float availableTorque = powerCurve.Evaluate(normalizedSpeed) * accelerationInput * speedFactor;
                        carRigidbody.AddForceAtPosition(accelerationDirection * availableTorque, wheel.position);
                    }
                    else if (accelerationInput < 0 && normalizedSpeed <= 1f)
                    {
                        float availableTorque = powerCurve.Evaluate(normalizedSpeed) * accelerationInput * breakFactor;
                        carRigidbody.AddForceAtPosition(accelerationDirection * availableTorque, position);
                    }

                    //Apply jump force 
                    if (jumpTime > 0f && !isJump)
                        carRigidbody.AddForceAtPosition(Vector3.down * (jumpFactor * jumpTime / stretchFactor),
                            position);
                    if (isJump)
                    {
                        jumpTime = Mathf.Max(jumpTime, 0.3f);
                        if (i == 0 || i == 1)
                            carRigidbody.AddForceAtPosition(Vector3.up * (jumpFactor * jumpTime), position);
                        else
                        {
                            carRigidbody.AddForceAtPosition(Vector3.up * (jumpFactor * jumpTime * 1.05f), position);
                        }
                    }
                }
            }

            i++;
        }

        if (isJump)
        {
            isJump = false;
            jumpTime = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PowerUp powerUp = other.GetComponent<PowerUp>();
        if (powerUp != null)
        {
            if (powerUpCount >= powerUpLimit)
            {
                return;
            }

            powerUpCount++;
            powerUps.Add(powerUp.powerUpType);
            other.GetComponent<NetworkObject>().Despawn(true);
        }
    }

    public void UsePowerUp(PowerUpType powerUpType)
    {
        if (powerUps.Contains(powerUpType))
        {
            powerUps.Remove(powerUpType);
            powerUpCount--;
            switch (powerUpType)
            {
                case PowerUpType.Nitro:
                    nitroAmount = maxNitroAmount;
                    break;
                case PowerUpType.Repair:
                    carHealth.Value = 100;
                    break;
                case PowerUpType.Fire:
                    carHealth.Value = Mathf.Max(0, carHealth.Value - 20);
                    break;
            }
        }
    }
}