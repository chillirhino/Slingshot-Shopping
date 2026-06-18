using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SlingshotShopping
{
    /// <summary>
    /// Holds the cart in the slingshot pouch, lets the player drag back to set power
    /// (and a little aim), and on release fires a single physics impulse. The same
    /// Rigidbody then arcs, lands, and rolls under CartController. Toggles the chase
    /// camera between aim-framing and follow.
    /// </summary>
    public class SlingshotLauncher : MonoBehaviour
    {
        public enum LaunchState { Ready, Aiming, Launched }

        [Header("Refs")]
        public Rigidbody cartBody;
        public CartController cart;
        public FollowCamera cam;
        public Transform pouch;

        [Header("Launch")]
        public float minLaunchSpeed = 12f;
        public float maxLaunchSpeed = 34f;
        [Tooltip("Upward tilt of the launch direction (degrees).")]
        public float launchAngle = 16f;
        [Tooltip("Max left/right aim from horizontal drag (degrees).")]
        public float maxAimYaw = 18f;

        [Header("Input")]
        [Tooltip("Screen pixels of pull-back that map to full power.")]
        public float pullReferencePixels = 400f;

        // exposed for HUD / debugging
        public LaunchState State { get; private set; }
        public float CurrentPower { get; private set; }   // 0..1
        public float CurrentAimYaw { get; private set; }   // degrees

        Vector2 _pressStart;
        bool _wasPressed;

        void Start() => ResetToPouch();

        public void ResetToPouch()
        {
            if (cart != null) cart.enabled = false;
            if (cartBody != null)
            {
                cartBody.isKinematic = true;
                cartBody.linearVelocity = Vector3.zero;
                cartBody.angularVelocity = Vector3.zero;
                if (pouch != null)
                {
                    cartBody.position = pouch.position;
                    cartBody.rotation = pouch.rotation;
                    cartBody.transform.position = pouch.position;
                    cartBody.transform.rotation = pouch.rotation;
                }
            }
            if (cam != null) cam.follow = false;
            CurrentPower = 0f;
            CurrentAimYaw = 0f;
            State = LaunchState.Ready;
            _wasPressed = false;
        }

        void Update()
        {
            if (State == LaunchState.Launched) return;

#if ENABLE_INPUT_SYSTEM
            var p = Pointer.current;
            if (p == null) return;
            bool pressed = p.press.isPressed;
            Vector2 pos = p.position.ReadValue();

            if (pressed && !_wasPressed)
            {
                _pressStart = pos;
                State = LaunchState.Aiming;
            }
            else if (pressed && State == LaunchState.Aiming)
            {
                float pullDown = Mathf.Max(0f, _pressStart.y - pos.y);
                float horiz = pos.x - _pressStart.x;
                CurrentPower = Mathf.Clamp01(pullDown / pullReferencePixels);
                CurrentAimYaw = Mathf.Clamp(horiz / pullReferencePixels, -1f, 1f) * maxAimYaw;
            }
            else if (!pressed && _wasPressed && State == LaunchState.Aiming)
            {
                if (CurrentPower > 0.05f) Launch(CurrentPower, CurrentAimYaw);
                else State = LaunchState.Ready;
            }
            _wasPressed = pressed;
#endif
        }

        /// <summary>Fire the cart. power01 in [0,1], aimYaw in degrees. Callable for tests.</summary>
        public void Launch(float power01, float aimYaw)
        {
            if (cartBody == null) return;
            power01 = Mathf.Clamp01(power01);

            cartBody.isKinematic = false;
            if (cart != null) cart.enabled = true;

            Vector3 dir = Quaternion.Euler(-launchAngle, aimYaw, 0f) * Vector3.forward;
            float speed = Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, power01);
            cartBody.linearVelocity = dir.normalized * speed;

            if (cam != null) cam.follow = true;
            State = LaunchState.Launched;
        }
    }
}
