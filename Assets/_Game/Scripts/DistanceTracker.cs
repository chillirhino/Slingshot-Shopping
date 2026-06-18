using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace SlingshotShopping
{
    /// <summary>
    /// Measures how far the cart has travelled along the road by projecting its position
    /// onto the spline. This is the score metric and the hook for coins/upgrades later.
    /// </summary>
    public class DistanceTracker : MonoBehaviour
    {
        public SplineContainer spline;
        public Transform cart;

        public float Distance { get; private set; }

        float _length;

        void Start()
        {
            if (spline != null)
                _length = SplineUtility.CalculateLength(spline.Spline, float4x4.identity);
        }

        void Update()
        {
            if (spline == null || cart == null) return;
            SplineUtility.GetNearestPoint(spline.Spline, (float3)cart.position, out _, out float t);
            Distance = Mathf.Clamp01(t) * _length;
        }
    }
}
