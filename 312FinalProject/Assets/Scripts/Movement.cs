using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [SerializeField] private Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private Transform accelerationPoint;
    [SerializeField] private LayerMask driveable;
    [SerializeField] private GameObject[] tires = new GameObject[4];
    [SerializeField] private GameObject[] frontTireParents = new GameObject[2];
    [SerializeField] private TrailRenderer[] skidMarks = new TrailRenderer[2];
    [SerializeField] private ParticleSystem[] skidSmokes = new ParticleSystem[2];
    [SerializeField] private AudioSource engineSound, skidSound;

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
    [SerializeField] private float tireRotationSpeed = 3000f;
    [SerializeField] private float maxSteeringAngle = 30f;
    [SerializeField] private float minSideSkidVelocity = 10f;

    private int[] wheelsIsGrounded = new int[4];
    [HideInInspector] public bool isGrounded = false;
    private Vector2 movement;
    private Vector3 currentCarLocalVelocity = Vector3.zero;
    private float carVelocityRatio;
    private Vector3 startPos;
    private Quaternion startRotation;
    [SerializeField]
    [Range(0,1)] private float minPitch = 1f;
    [SerializeField]
    [Range(0,5)] private float maxpitch = 5f;

    void Awake()
    {
        // Initialize start position
        startPos = transform.position;
        startRotation = transform.rotation;
    }

    void Start()
    { 
        // Initialize RB
        carRB = GetComponent<Rigidbody>();
        carRB.isKinematic = true;
    }

    void FixedUpdate()
    {
        if (!RaceManager.Instance.raceStarted)
        return; // If the race isn't started, ignore all physics method calls

        Suspension();
        GroundCheck();
        CalculateCarVelocity();
        Move();
        Visuals();
        EngineSound();
    }
 
    // Get input using new Unity input actions
    public void OnMove(InputValue value)
    {
        if (RaceManager.Instance.raceStarted)
        {
            movement = value.Get<Vector2>();    
        }
        else
        {
            movement = Vector2.zero;
        }
    }

    // Reset player to start position and remove all RB velocity. 
    public void ResetPlayer()
    {
        transform.position = startPos;
        transform.rotation = startRotation;

        carRB.linearVelocity = Vector3.zero;
        carRB.angularVelocity = Vector3.zero;
    }

    public void StartPlayer()
    {
        carRB.isKinematic = false;
    }

    public void StopPlayer()
    {
        // Freeze physics so the car doesn't move or fall during menus
        carRB.linearVelocity = Vector3.zero;
        carRB.angularVelocity = Vector3.zero;
        carRB.isKinematic = true;
    }

    // Calculate car velocity as the ration between it's current forward speed and maximum speed.
    private void CalculateCarVelocity()
    {
        currentCarLocalVelocity = transform.InverseTransformDirection(carRB.linearVelocity);
        carVelocityRatio = currentCarLocalVelocity.z / maxSpeed;
    }

    // Handle movement
    private void Move()
    {
        if (isGrounded)
        {
            Acceleration();
            // Deceleration();
            Turn();
            SidewaysDrag();
        }
    }

    private void Acceleration()
    {
        carRB.AddForceAtPosition(acceleration * movement.y * transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }

    // private void Deceleration()
    // {
    //     carRB.AddForceAtPosition(deceleration * movement.y * -transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    // }

    // Evaluate turning power based on a steering curve (found in the editor)
    private void Turn()
    {
        carRB.AddTorque(steerStrength * movement.x * turningCurve.Evaluate(carVelocityRatio) * Mathf.Sign(carVelocityRatio) * transform.up, ForceMode.Acceleration);
    }

    // Add sideways drag to determine how "slippy" the tires are. 
    private void SidewaysDrag()
    {
        float currentSidewaysSpeed = currentCarLocalVelocity.x;

        float dragMagnitude = -currentSidewaysSpeed * dragCoefficient;

        Vector3 dragForce = transform.right * dragMagnitude;

        carRB.AddForceAtPosition(dragForce, carRB.worldCenterOfMass, ForceMode.Acceleration);
    }

    // Physics based suspension
    private void Suspension()
    {
        // For each raypoint that represents a tire suspension spring
        for (int i = 0; i < rayPoints.Length; i++)
        {
            RaycastHit hit;
            float maxLength = restLength + springTravel;

            // Calculate ray hit
            bool rayDidHit = Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxLength + wheelRadius, driveable);

            // If the ray didn't hit
            if (!rayDidHit)
            {   
                // Set that wheel to be ungrounded
                wheelsIsGrounded[i] = 0;
                SetTirePosition(tires[i], rayPoints[i].position - rayPoints[i].up * maxLength);
                Debug.DrawLine(rayPoints[i].position, rayPoints[i].position + (wheelRadius + maxLength) * -rayPoints[i].up, Color.green);
                continue;
            }

            // Otherwise, set that wheel to grounded
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

            // Add the calculated spring force
            carRB.AddForceAtPosition(springDir * force, rayPoints[i].position);

            // Adjust the visual tire position
            SetTirePosition(tires[i], hit.point + rayPoints[i].up * wheelRadius);

            Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
        }
    }

    // Ground check requires two wheels to be "touching" the ground to be grounded
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

    // Adjust tire visuals
    private void SetTirePosition(GameObject tire, Vector3 targetPosition)
    {
        tire.transform.position = targetPosition;
    }

    private void Visuals()
    {
        TireVisuals();
        VFX();
    }

    private void VFX()
    {
        // If we're grounded and our car has sideways momentum "skidding"
        if (isGrounded && currentCarLocalVelocity.x > minSideSkidVelocity)
        {
            ToggleSkidMarks(true);
            ToggleSkidSmokes(true);
            ToggleSkidSound(true);
        }
        else
        {
            ToggleSkidMarks(false);
            ToggleSkidSmokes(false);
            ToggleSkidSound(false);
        }
    }

    // Toggle the skid marks
    private void ToggleSkidMarks(bool toggle)
    {
        foreach (var skidMark in skidMarks)
        {
            skidMark.emitting = toggle;
        }
    }

    // Toggle the particle effects
    private void ToggleSkidSmokes(bool toggle)
    {
        foreach (var smoke in skidSmokes)
        {
            if (toggle)
            {
                smoke.Play();
            }
            else
            {
                smoke.Stop();
            }
        }
    }

    // Tire rotation effects
    private void TireVisuals()
    {
        float steeringAngle = maxSteeringAngle * movement.x;

        for (int i = 0; i < tires.Length; i++)
        {
            if (i < 2) // for the front tires we rotate forward/backwards and rotate on y-axis for steering
            {
                tires[i].transform.Rotate(Vector3.right, tireRotationSpeed * carVelocityRatio * Time.deltaTime, Space.Self);

                frontTireParents[i].transform.localEulerAngles = 
                    new Vector3(frontTireParents[i].transform.localEulerAngles.x,
                                steeringAngle, frontTireParents[i].transform.localEulerAngles.z);
            }
            else // rear tires just rotate forward and backwards.
            {
                tires[i].transform.Rotate(Vector3.right, tireRotationSpeed * movement.y * Time.deltaTime, Space.Self);
            }
        }
    }

    // Engine sound lerp for engine revving sound effect.
    private void EngineSound()
    {
        engineSound.pitch = Mathf.Lerp(minPitch, maxpitch, Mathf.Abs(carVelocityRatio));
    }

    private void ToggleSkidSound(bool toggle)
    {
        skidSound.mute = !toggle;
    }
}
