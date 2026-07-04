using UnityEngine;
using TMPro;

public class Ball_timer : MonoBehaviour
{
    [SerializeField]
    private RoundSettings round_set;

    public float timer;
    public TMP_Text timertext;

    public bool IsTimeUp => timer <= 0f; // 외부에서 시간 다 됐는지 쉽게 체크용

    void Start()
    {
        timer = round_set.time;
    }

    void Update()
    {
        Timer();
    }

    void Timer()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            timertext.text = $"Time: {timer:F0}";
        }
        else
        {
            timer = 0.0f;
            timertext.text = $"Time: 0";
        }
    }
}