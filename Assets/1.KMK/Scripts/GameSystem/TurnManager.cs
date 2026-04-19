using System;
using Fusion;
using UnityEngine;

/// <summary>
/// 플레이어 턴 순서를 관리합니다. Host(StateAuthority)가 상태를 제어합니다.
/// </summary>
public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance { get; private set; }

    public static event Action<int> OnTurnUpdated;

    [Networked, OnChangedRender(nameof(OnTurnChanged))]
    public int CurrentPlayerIndex { get; private set; }

    [Networked]
    public int PlayerCount { get; private set; }

    [Networked]
    public NetworkBool IsGameRunning { get; private set; }

    public override void Spawned()
    {
        Instance = this;

        // 클라이언트가 늦게 접속했을 때 현재 상태를 UI에 반영
        if (IsGameRunning)
            OnTurnUpdated?.Invoke(CurrentPlayerIndex + 1);
    }

    /// <summary>게임 시작 시 Host가 호출합니다.</summary>
    public void StartGame(int playerCount)
    {
        if (!HasStateAuthority) return;

        PlayerCount = playerCount;
        CurrentPlayerIndex = 0;
        IsGameRunning = true;

        // 초기값(0)은 OnChangedRender가 안 불리므로 직접 발생
        OnTurnUpdated?.Invoke(CurrentPlayerIndex + 1);
        TimerManager.Instance?.StartTimer();
    }

    /// <summary>턴을 다음 플레이어로 넘깁니다.</summary>
    public void NextTurn()
    {
        if (!HasStateAuthority) return;
        if (!IsGameRunning) return;

        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % PlayerCount;
        TimerManager.Instance?.StartTimer();
    }

    // CurrentPlayerIndex 변경 시 모든 클라이언트에서 실행
    private void OnTurnChanged()
    {
        OnTurnUpdated?.Invoke(CurrentPlayerIndex + 1);
    }
}
