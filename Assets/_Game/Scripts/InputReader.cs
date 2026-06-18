using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SlingshotShopping
{
    /// <summary>
    /// Translates touch/mouse (and keyboard, for editor testing) into the cart's steer
    /// axis after launch. Hold the left half of the screen to steer left, the right half
    /// to steer right. Only active once the slingshot has launched, so it never fights
    /// the launcher's aim drag. Smooths the axis so steering ramps in/out.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        public CartController cart;
        public SlingshotLauncher launcher;
        [Tooltip("How fast the steer axis ramps toward the target (per second).")]
        public float steerSmoothing = 8f;

        float _steer;

        void Update()
        {
            if (cart == null) return;

            bool active = launcher == null ||
                          launcher.State == SlingshotLauncher.LaunchState.Launched;

            float target = 0f;
            if (active)
            {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) target -= 1f;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) target += 1f;
                }
                var p = Pointer.current;
                if (p != null && p.press.isPressed)
                {
                    float x = p.position.ReadValue().x;
                    target += (x < Screen.width * 0.5f) ? -1f : 1f;
                }
#endif
                target = Mathf.Clamp(target, -1f, 1f);
            }

            _steer = Mathf.MoveTowards(_steer, target, steerSmoothing * Time.deltaTime);
            cart.steerInput = _steer;
        }
    }
}
