using UnityEngine;

namespace SlingshotShopping
{
    /// <summary>
    /// Mobile runtime baseline: cap to a steady frame rate and disable vSync so the
    /// target frame rate is honored on device. Add once to a bootstrap object.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class AppConfig : MonoBehaviour
    {
        [Tooltip("Target frame rate on device.")]
        public int targetFps = 60;

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFps;
        }
    }
}
