using UnityEngine;

namespace SlingshotShopping
{
    /// <summary>
    /// Top-level game flow for one launch run:
    /// Aiming -> Rolling -> Stopped (cart slows to rest or falls off) -> retry.
    /// Owns the HUD updates and the reset.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public SlingshotLauncher launcher;
        public CartController cart;
        public Rigidbody cartBody;
        public DistanceTracker tracker;
        public GameUI ui;

        [Header("End conditions")]
        public float stopSpeed = 1.5f;
        public float stopTime = 1.5f;
        public float fallY = -5f;

        enum Phase { Aiming, Rolling, Stopped }
        Phase _phase;
        float _stopTimer;

        void Start()
        {
            if (ui != null && ui.retryButton != null)
                ui.retryButton.onClick.AddListener(Retry);
            BeginAiming();
        }

        void BeginAiming()
        {
            launcher.ResetToPouch();
            _phase = Phase.Aiming;
            _stopTimer = 0f;
            if (ui != null) ui.ShowAiming();
        }

        void Update()
        {
            switch (_phase)
            {
                case Phase.Aiming:
                    if (ui != null) ui.UpdatePower(launcher.CurrentPower);
                    if (launcher.State == SlingshotLauncher.LaunchState.Launched)
                        _phase = Phase.Rolling;
                    break;

                case Phase.Rolling:
                    if (ui != null) ui.UpdateDistance(tracker.Distance);

                    bool slow = Mathf.Abs(cart.ForwardSpeed) < stopSpeed && cart.IsGrounded;
                    _stopTimer = slow ? _stopTimer + Time.deltaTime : 0f;
                    bool fell = cartBody.position.y < fallY;

                    if (_stopTimer >= stopTime || fell)
                    {
                        _phase = Phase.Stopped;
                        if (ui != null) ui.ShowResult(tracker.Distance, fell);
                    }
                    break;

                case Phase.Stopped:
                    break;
            }
        }

        public void Retry() => BeginAiming();
    }
}
