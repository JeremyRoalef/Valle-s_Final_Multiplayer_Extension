using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [SerializeField] private Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private Transform accelerationPoint;
    [SerializeField] LayerMask driveable;

    [SerializeField] private float springStiffness;
    [SerializeField] private float damperStiffness;
    [SerializeField] private float restLength;
    [SerializeField] private float springTravel;
    [SerializeField] private float wheelRadius;

    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float steerStrength = 15f;
    [SerializeField] private AnimationCurve turningCurve;
    [SerializeField] private float dragCoefficient = 1f;

    private int[] wheelsIsGrounded = new int[4];
    private bool isGrounded = false;
    private Vector2 movement;
    private Vector3 currentCarLocalVelocity = Vector3.zero;
    private float carVelocityRatio;

    void Start()
    {
        carRB = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Debug.Log(movement.x);
        Debug.Log(movement.y);
    }

    void FixedUpdate()
    {
        Suspension();
        GroundCheck();
        CalculateCarVelocity();
        Move();
    }

    public void OnMove(InputValue value)
    {
        movement = value.Get<Vector2>();
    }

    private void CalculateCarVelocity()
    {
        currentCarLocalVelocity = transform.InverseTransformDirection(carRB.linearVelocity);
        carVelocityRatio = currentCarLocalVelocity.z / maxSpeed;
    }

    private void Move()
    {
        if (isGrounded)
        {
            Acceleration();
            Deceleration();
            Turn();
            SidewaysDrag();
        }
    }

    private void Acceleration()
    {
        carRB.AddForceAtPosition(acceleration * movement.y * transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void Deceleration()
    {
        carRB.AddForceAtPosition(deceleration * movement.y * -transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void Turn()
    {
        carRB.AddTorque(steerStrength * movement.x * turningCurve.Evaluate(carVelocityRatio) * Mathf.Sign(carVelocityRatio) * transform.up, ForceMode.Acceleration);
    }

    private void SidewaysDrag()
    {
        float currentSidewaysSpeed = currentCarLocalVelocity.x;

        float dragMagnitude = -currentSidewaysSpeed * dragCoefficient;

        Vector3 dragForce = transform.right * dragMagnitude;

        carRB.AddForceAtPosition(dragForce, carRB.worldCenterOfMass, ForceMode.Acceleration);
    }

    private void Suspension()

    {
        for (int i = 0; i < rayPoints.Length; i++)
        {
            RaycastHit hit;
            float maxLength = restLength + springTravel;
            bool rayDidHit = Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxLength + wheelRadius, driveable);

            if (!rayDidHit)
            {
                wheelsIsGrounded[i] = 0;
                Debug.DrawLine(rayPoints[i].position, rayPoints[i].position + (wheelRadius + maxLength) * -rayPoints[i].up, Color.green);
                continue;
            }

            wheelsIsGrounded[i] = 1;

            Vector3 springDir = rayPoints[i].up;   // direction of the spring

            // Effective suspension length (pivot to wheel contact)
            float currentLength = hit.distance - wheelRadius;

            // Extension relative to rest
            float displacement = restLength - currentLength;

            // Velocity along the spring direction
            float vel = Vector3.Dot(carRB.GetPointVelocity(rayPoints[i].position), springDir);

            // Hooke’s law + damping
            float force = (springStiffness * displacement) - (damperStiffness * vel);

            // Clamp so suspension doesn't "pull" downward
            if (force < 0) force = 0;

            carRB.AddForceAtPosition(springDir * force, rayPoints[i].position);

            Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
        }
    }

    private void GroundCheck()
    {
        int tempGroundedWheels = 0;

        for (int i = 0; i < wheelsIsGrounded.Length; i++)
        {
            tempGroundedWheels += wheelsIsGrounded[i];
        }

        if (tempGroundedWheels > 1)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    // if (rayDidHit)
    //         {
    //             float currentSpringLength = hit.distance - wheelRadius;
    //             float springCompression = (restLength - currentSpringLength) / springTravel;

    //             float springVelocity = Vector3.Dot(carRB.GetPointVelocity(rayPoint.position), rayPoint.up);
    //             float dampForce = damperStiffness * springVelocity;

    //             float springForce = springStiffness * springCompression;

    //             float netForce = springForce - dampForce;

    //             carRB.AddForceAtPosition(netForce * rayPoint.up, rayPoint.position);

    //             Debug.DrawLine(rayPoint.position, hit.point, Color.red);
    //         }
    //         else
    //         {
    //             Debug.DrawLine(rayPoint.position, rayPoint.position + (wheelRadius + maxLength) * -rayPoint.up, Color.green);
    //         }

    // // Suspension spring force
    // if (rayDidHit)
    // {
    //     // world-space direction of the spring force.
    //     Vector3 springDir = tireTransform.up;
        
    //     // world-space velocity of this tire
    //     Vector3 tireWorldVel = carRigidBody.GetPointVelocity(tireTransform.position);

    //     // calculate offset from the raycast
    //     float offset = suspenstionRestDist - tireRay.distance;

    //     // calculate the velocity along the spring direction
    //     // note that springDir is a unit vector, so this returns the magnitude of tireWorldVel
    //     // as projected onto springDir
    //     float vel = Vector3.Dot(springDir, tireWorldVel);

    //     // calculate the magnitude of the dampened spring force
    //     float force = (offset * springStrength) - (Vector2Lerp * springDamper);

    //     // apply the force at the location of this tire, in the direction of the suspension.
    //     carRigidBody.AddForceAtPosition(springDir * force, tireTransform.position)
    // }
}
