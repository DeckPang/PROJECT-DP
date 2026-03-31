using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    public void UpdateTimerDisplay(float currentTime)
    {
        timerText.text = "Time remaining: " + currentTime.ToString("F0");
    }
}
