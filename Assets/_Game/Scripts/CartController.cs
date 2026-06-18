using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SlingshotShopping
{
    /// <summary>
    /// Arcade shopping-cart physics. One Rigidbody + four raycast "wheels" for ground
    /// contact and suspension. Forward momentum bleeds off via rolling drag (the cart
    /// slows down), steering is yaw torque, and an explicit grip model cancels sideways
    /// velocity — grip drops on hard turns at speed, which produces drift. Ramps/bumps are
    /// real geometry, so jumps just happen. Tunable for an exaggerated, funny feel.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CartController : MonoBehaviour
    {
        [Header("Wheels / Suspension")]
        public LayerMask groundMask = ~0;
        [Tooltip("Half the track width (left/right wheel offset).")]
        public float trackHalf = 0.55f;
        [Tooltip("Half the wheelbase (front/back wheel offset).")]
        public float baseHalf = 0.95f;
        [Tooltip("Local Y of the ray origins relative to body center.")]
        public float wheelOriginY = -0.2f;
        [Tooltip("Suspension ray length (also the rest hover distance).")]
        public float restLength = 0.6f;
        public float springStrength = 3200f;
        public float springDamper = 350f;

        [Header("Drive")]
        [Tooltip("Optional motor force for testing/keyboard. Real game relies on launch impulse.")]
        public float motorForce = 1400f;
        public float maxSpeed = 30f;
        [Tooltip("Exponential speed decay (1/s). Higher = stops sooner.")]
        public float rollingDrag = 0.5f;

        [Header("Steering")]
        public float steerTorque = 110f;
        [Tooltip("Forward speed at which steering reaches full authority.")]
        public float steerFullSpeed = 10f;
        [Range(0f, 1f)] public float airControl = 0.15f;

        [Header("Grip / Drift")]
        [Range(0f, 1f)] public float frontGrip = 0.9f;
        [Range(0f, 1f)] public float driftGrip = 0.15f;
        [Tooltip("Steer magnitude above which the cart starts to break traction.")]
        [Range(0f, 1f)] public float driftSteerThreshold = 0.55f;
        public float driftSpeedThreshold = 8f;

        [Header("Stability")]
        [Tooltip("Lowers center of mass to resist flipping.")]
        public Vector3 centerOfMass = new Vector3(0f, -0.55f, 0f);
        [Tooltip("Self-righting strength per degree of tilt (grounded).")]
        public float uprightAssist = 0.6f;
        [Tooltip("Fraction of self-righting applied while airborne (allows a little tumble).")]
        [Range(0f, 1f)] public float airUprightFactor = 0.12f;
        [Tooltip("Angular damping on the rigidbody (resists rolling/spinning).")]
        public float angularDamping = 2.5f;
        [Tooltip("Below this forward speed (and no throttle), parking friction kicks in.")]
        public float parkingSpeed = 2f;
        [Range(0f, 1f)] public float parkingFriction = 0.4f;

        [Header("Inputs (set externally)")]
        [Range(-1f, 1f)] public float steerInput;
        [Range(0f, 1f)] public float motorInput;

        [Header("Stability (safety)")]
        [Tooltip("Hard cap on linear speed; prevents physics-blowup runaway.")]
        public float maxLinearVelocity = 38f;
        [Tooltip("Clamp on per-wheel suspension force; prevents penetration ejection off ramps.")]
        public float maxSuspensionForce = 9000f;

        [Header("Testing")]
        public bool useKeyboard;
        public bool autoDrive;

        // --- runtime state, read by camera/UI ---
        public bool IsGrounded { get; private set; }
        public bool IsDrifting { get; private set; }
        public float ForwardSpeed { get; private set; }
        public int GroundedWheels { get; private set; }

        Rigidbody _rb;
        Vector3[] _wheels;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = true;
            _rb.linearDamping = 0f;
            _rb.angularDamping = angularDamping;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.centerOfMass = centerOfMass;
            _rb.maxLinearVelocity = maxLinearVelocity;
            _rb.maxAngularVelocity = 12f;
            BuildWheels();
        }

        void BuildWheels()
        {
            _wheels = new Vector3[]
            {
                new Vector3(-trackHalf, wheelOriginY,  baseHalf), // FL
                new Vector3( trackHalf, wheelOriginY,  baseHalf), // FR
                new Vector3(-trackHalf, wheelOriginY, -baseHalf), // RL
                new Vector3( trackHalf, wheelOriginY, -baseHalf), // RR
            };
        }

        void Update()
        {
            if (autoDrive)
            {
                motorInput = 1f;
                steerInput = Mathf.Sin(Time.time * 0.8f) * 0.7f;
                return;
            }
#if ENABLE_INPUT_SYSTEM
            if (useKeyboard && Keyboard.current != null)
            {
                var k = Keyboard.current;
                float s = 0f;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) s -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) s += 1f;
                steerInput = s;
                motorInput = (k.wKey.isPressed || k.upArrowKey.isPressed) ? 1f : 0f;
            }
#endif
        }

        void FixedUpdate()
        {
            if (_wheels == null) BuildWheels();

            int grounded = 0;
            Vector3 up = transform.up;

            // --- Suspension (raycast wheels) ---
            for (int i = 0; i < _wheels.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(_wheels[i]);
                if (Physics.Raycast(worldPos, -up, out RaycastHit hit, restLength, groundMask, QueryTriggerInteraction.Ignore))
                {
                    grounded++;
                    float offset = restLength - hit.distance;                 // compression (+)
                    float vel = Vector3.Dot(_rb.GetPointVelocity(worldPos), up);
                    float force = offset * springStrength - vel * springDamper;
                    // Allow negative (damping while the wheel is still in contact) so the
                    // spring doesn't pump energy; only cap the magnitude to avoid ejection.
                    force = Mathf.Clamp(force, -maxSuspensionForce, maxSuspensionForce);
                    _rb.AddForceAtPosition(up * force, worldPos);
                }
            }

            GroundedWheels = grounded;
            IsGrounded = grounded > 0;

            // Flatten forward/right onto the world ground plane for stable arcade handling.
            Vector3 flatFwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 flatRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            ForwardSpeed = Vector3.Dot(_rb.linearVelocity, flatFwd);

            // --- Motor (test/keyboard) ---
            if (IsGrounded && motorInput > 0.01f && ForwardSpeed < maxSpeed)
                _rb.AddForce(flatFwd * (motorInput * motorForce), ForceMode.Force);

            // --- Rolling drag: speed slowly decreases ---
            if (IsGrounded)
                _rb.AddForce(-flatFwd * (ForwardSpeed * rollingDrag), ForceMode.Acceleration);

            // --- Steering (yaw torque, scaled by speed & grounded) ---
            float speedFactor = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / steerFullSpeed);
            float control = IsGrounded ? 1f : airControl;
            float dir = Mathf.Sign(ForwardSpeed == 0f ? 1f : ForwardSpeed);
            _rb.AddTorque(Vector3.up * (steerInput * steerTorque * speedFactor * dir * control), ForceMode.Acceleration);

            // --- Grip / Drift ---
            if (IsGrounded)
            {
                float lateral = Vector3.Dot(_rb.linearVelocity, flatRight);
                IsDrifting = Mathf.Abs(steerInput) > driftSteerThreshold &&
                             Mathf.Abs(ForwardSpeed) > driftSpeedThreshold;
                float grip = IsDrifting ? driftGrip : frontGrip;
                _rb.AddForce(-flatRight * (lateral * grip), ForceMode.VelocityChange);

                // Parking friction: when grounded, not driving and nearly stopped, kill
                // residual horizontal velocity so the cart doesn't creep off the road.
                if (motorInput < 0.01f && Mathf.Abs(ForwardSpeed) < parkingSpeed)
                {
                    Vector3 hv = Vector3.ProjectOnPlane(_rb.linearVelocity, Vector3.up);
                    _rb.AddForce(-hv * parkingFriction, ForceMode.VelocityChange);
                }
            }
            else
            {
                IsDrifting = false;
            }

            // --- Self-righting: keep the cart from staying flipped. Strong on the
            // ground, mild in the air (so it can still tumble a little off jumps).
            // Deadzone near upright avoids jitter that would nudge the cart around. ---
            float tilt = Vector3.Angle(transform.up, Vector3.up); // 0=upright, 180=inverted
            if (tilt > 2f)
            {
                Vector3 axis = Vector3.Cross(transform.up, Vector3.up);
                if (axis.sqrMagnitude < 1e-4f) axis = transform.forward; // inverted: escape axis
                float k = IsGrounded ? uprightAssist : uprightAssist * airUprightFactor;
                _rb.AddTorque(axis.normalized * (tilt * k), ForceMode.Acceleration);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (_wheels == null) BuildWheels();
            Gizmos.color = Color.cyan;
            foreach (var w in _wheels)
            {
                Vector3 o = transform.TransformPoint(w);
                Gizmos.DrawSphere(o, 0.08f);
                Gizmos.DrawLine(o, o - transform.up * restLength);
            }
        }
    }
}
