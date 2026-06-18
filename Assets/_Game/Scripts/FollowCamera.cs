using UnityEngine;

namespace SlingshotShopping
{
    /// <summary>
    /// Lightweight mobile chase camera (no Cinemachine). Follows the cart from behind
    /// and above, smoothly damped. Yaw tracks the cart's travel direction (velocity when
    /// moving, heading when slow) so a drifting/spinning cart stays nicely framed instead
    /// of whipping the camera around. Can hold a fixed "aim" framing before launch.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public Rigidbody targetBody;

        [Header("Chase framing")]
        public float distance = 9f;
        public float height = 4.5f;
        public float lookAhead = 6f;
        public float lookHeight = 1.5f;

        [Header("Smoothing")]
        public float positionDamp = 0.18f;
        public float rotationLerp = 6f;
        [Tooltip("Speed above which the camera aligns to velocity instead of heading.")]
        public float velocityAlignSpeed = 3f;

        [Header("Mode")]
        public bool follow = true;
        [Tooltip("Fixed framing used when follow is false (e.g. aiming the slingshot).")]
        public Vector3 aimPosition = new Vector3(0f, 4f, -8f);
        public Vector3 aimLookAt = new Vector3(0f, 1f, 6f);

        Vector3 _vel;       // SmoothDamp velocity
        Vector3 _smoothFwd = Vector3.forward;

        void LateUpdate()
        {
            if (!follow || target == null)
            {
                transform.position = Vector3.Lerp(transform.position, aimPosition, rotationLerp * Time.deltaTime);
                Quaternion aimRot = Quaternion.LookRotation((aimLookAt - transform.position).normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, aimRot, rotationLerp * Time.deltaTime);
                return;
            }

            // Direction the camera should sit behind: velocity if moving, else heading.
            Vector3 dir;
            if (targetBody != null)
            {
                Vector3 flatVel = Vector3.ProjectOnPlane(targetBody.linearVelocity, Vector3.up);
                dir = flatVel.magnitude > velocityAlignSpeed
                    ? flatVel.normalized
                    : Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
            }
            else
            {
                dir = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
            }
            if (dir.sqrMagnitude < 0.001f) dir = _smoothFwd;
            _smoothFwd = Vector3.Slerp(_smoothFwd, dir, rotationLerp * Time.deltaTime);

            Vector3 desiredPos = target.position - _smoothFwd * distance + Vector3.up * height;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _vel, positionDamp);

            Vector3 lookTarget = target.position + _smoothFwd * lookAhead + Vector3.up * lookHeight;
            Quaternion rot = Quaternion.LookRotation((lookTarget - transform.position).normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationLerp * Time.deltaTime);
        }
    }
}
