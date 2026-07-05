using UnityEngine;
using TMPro;

public class Ball_timer : MonoBehaviour
{
    public float time;
    public TMP_Text timertext;

    public bool IsTimeUp => time <= 0f; // 외부에서 시간 다 됐는지 쉽게 체크용

    void Start()
    {
        time = 60.0f;
    }

    void Update()
    {
        Timer();
    }

    void Timer()
    {
        if (time > 0)
        {
            time -= Time.deltaTime;
            timertext.text = $"Time: {time:F0}";
        }
        else
        {
            time = 0.0f;
            timertext.text = $"Time: 0";
        }
    }
}