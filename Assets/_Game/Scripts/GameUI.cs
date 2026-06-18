using UnityEngine;
using UnityEngine.UI;

namespace SlingshotShopping
{
    /// <summary>
    /// Whitebox HUD: distance readout, a pull/power bar while aiming, a hint line, and a
    /// result panel with a retry button. Driven entirely by GameManager.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public Text distanceText;
        public Text hintText;
        public Image powerBar;       // filled image (fillAmount = power)
        public GameObject resultPanel;
        public Text resultText;
        public Button retryButton;

        public void ShowAiming()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (powerBar != null) powerBar.gameObject.SetActive(true);
            if (hintText != null) { hintText.gameObject.SetActive(true); hintText.text = "Drag back & release to launch"; }
            if (distanceText != null) distanceText.text = "0 m";
        }

        public void UpdatePower(float power01)
        {
            if (powerBar != null) powerBar.fillAmount = power01;
        }

        public void UpdateDistance(float meters)
        {
            if (powerBar != null) powerBar.gameObject.SetActive(false);
            if (hintText != null) hintText.gameObject.SetActive(false);
            if (distanceText != null) distanceText.text = Mathf.FloorToInt(meters) + " m";
        }

        public void ShowResult(float meters, bool fell)
        {
            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultText != null)
                resultText.text = (fell ? "Off the road!\n" : "Stopped!\n") + Mathf.FloorToInt(meters) + " m";
        }
    }
}
