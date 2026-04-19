using System;
using Fusion;
using UnityEngine;

/// <summary>
/// 턴 타이머를 관리합니다. Host가 카운트다운하고 모든 클라이언트에 동기화됩니다.
/// </summary>
public class TimerManager : NetworkBehaviour
{
    public static TimerManager Instance { get; private set; }

    public static event Action<float> OnTimerUpdated;

    [SerializeField] private float turnDuration = 30f;

    [Networked, OnChangedRender(nameof(OnTimerChanged))]
    public float TimeRemaining { get; private set; }

    [Networked]
    public NetworkBool IsRunning { get; private set; }

    public override void Spawned()
    {
        Instance = this;

        // 클라이언트가 늦게 접속했을 때 현재 타이머 상태를 UI에 반영
        if (IsRunning)
            OnTimerUpdated?.Invoke(TimeRemaining);
    }

    /// <summary>턴 시작 시 TurnManager에서 호출합니다.</summary>
    public void StartTimer()
    {
        if (!HasStateAuthority) return;

        TimeRemaining = turnDuration;
        IsRunning = true;
    }

    public void StopTimer()
    {
        if (!HasStateAuthority) return;

        IsRunning = false;
    }

    // Host에서만 카운트다운 (FixedUpdateNetwork는 모든 클라이언트에서 실행되지만 StateAuthority만 값 변경)
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (!IsRunning) return;

        TimeRemaining -= Runner.DeltaTime;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            IsRunning = false;
            TurnManager.Instance?.NextTurn();
        }
    }

    // TimeRemaining 변경 시 모든 클라이언트에서 실행
    private void OnTimerChanged()
    {
        OnTimerUpdated?.Invoke(TimeRemaining);
    }
}
