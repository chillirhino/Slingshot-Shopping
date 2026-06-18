using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace SlingshotShopping
{
    /// <summary>
    /// TEMPORARY test harness: auto-steers the cart to follow the spline so we can
    /// observe drift on sharp curves and jumps off ramps without manual input.
    /// Not part of the shipping game — remove once real input/launch is in.
    /// </summary>
    public class CartTestDriver : MonoBehaviour
    {
        public SplineContainer spline;
        public CartController cart;
        [Tooltip("How far ahead along the spline (normalized) to aim.")]
        public float lookAhead = 0.04f;
        public float steerGain = 3f;
        public bool active = true;

        void FixedUpdate()
        {
            if (!active || spline == null || cart == null) return;

            float3 pos = cart.transform.position;
            SplineUtility.GetNearestPoint(spline.Spline, pos, out float3 nearest, out float t);

            float ta = Mathf.Repeat(t + lookAhead, 1f);
            spline.Spline.Evaluate(ta, out float3 target, out _, out _);

            Vector3 toTarget = ((Vector3)target - cart.transform.position);
            toTarget.y = 0f;
            toTarget.Normalize();

            Vector3 fwd = cart.transform.forward; fwd.y = 0f; fwd.Normalize();
            // signed angle -> steer
            float signed = Vector3.SignedAngle(fwd, toTarget, Vector3.up); // degrees
            cart.steerInput = Mathf.Clamp(signed / 30f * steerGain, -1f, 1f);
            cart.motorInput = 1f;
        }
    }
}
