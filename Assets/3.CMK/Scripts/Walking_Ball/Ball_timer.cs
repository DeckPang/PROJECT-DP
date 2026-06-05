using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Ball_timer : MonoBehaviour
{
    public float time;
    public TMP_Text timertext;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        time = 60.0f;
    }

    // Update is called once per frame
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
