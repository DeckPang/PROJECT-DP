using System;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public float currentTime;
    private bool isTimerRunning = false;

    public void StartTimer(float time)
    {
        currentTime = time;
        isTimerRunning = true;
    }

    public void ResetTimer(float time)
    {
        currentTime = time;
    }

    public void AddTime(float extraTime)
    {
        currentTime += extraTime;
        Debug.Log($"시간 연장. 남은 시간: {currentTime}초");
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    public void TickTimer(float deltaTime)
    {
        if (!isTimerRunning) return;

        currentTime -= deltaTime; // 매 프레임마다 시간 깎기
        if (currentTime <= 0)
        {
            currentTime = 0;
            isTimerRunning = false;
        }
    }
    // 지금 몇 초 남았는지 알려주는 함수
    public float GetCurrentTime()
    {
        return currentTime;
    }
    public bool IsTimeUp()
    {
        return !isTimerRunning && currentTime <= 0;
    }
}
