using System;
using Fusion;
using UnityEngine;

/// <summary>
/// Host가 Spawn하는 NetworkBehaviour.
/// Ready 상태와 슬롯 점유 여부를 모든 클라이언트에 동기화합니다.
/// </summary>
public class LobbyState : NetworkBehaviour
{
    public static LobbyState Instance { get; private set; }

    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public int OccupiedMask { get; set; } // bit N = slot N 점유

    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public int ReadyMask    { get; set; } // bit N = slot N 준비

    // ── Static 이벤트 (UI 구독용) ─────────────────────────────────────────

    public static event Action<int, bool> OnSlotOccupancyChanged;
    public static event Action<int, bool> OnSlotReadyChanged;
    public static event Action<bool>      OnCanStartChanged;

    // ── 생명주기 ─────────────────────────────────────────────────────────

    public override void Spawned()
    {
        Instance = this;
        Debug.Log($"[LobbyState] Spawned → HasStateAuthority={HasStateAuthority}");

        // 늦게 접속한 클라이언트를 위해 현재 상태를 즉시 UI에 반영
        OnStateChanged();
    }

    // ── Host 전용 ─────────────────────────────────────────────────────────

    public void SetOccupied(int slot, bool occupied)
    {
        if (!HasStateAuthority) return;

        if (occupied)
        {
            OccupiedMask |=  (1 << slot);
            if (slot == 0) ReadyMask |= 1; // 방장은 항상 준비됨
        }
        else
        {
            OccupiedMask &= ~(1 << slot);
            ReadyMask    &= ~(1 << slot);
        }
    }

    // ── 모든 플레이어 호출 가능 ───────────────────────────────────────────

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetReady(int slot, bool isReady)
    {
        if (isReady) ReadyMask |=  (1 << slot);
        else         ReadyMask &= ~(1 << slot);

        Debug.Log($"[LobbyState] RPC_SetReady → slot={slot}, isReady={isReady}");
    }

    // ── 상태 조회 ─────────────────────────────────────────────────────────

    public bool IsSlotOccupied(int slot) => (OccupiedMask & (1 << slot)) != 0;
    public bool IsSlotReady(int slot)    => (ReadyMask    & (1 << slot)) != 0;

    public bool CanStartGame()
    {
        int occupiedCount = 0;
        for (int i = 0; i < 4; i++)
        {
            if (!IsSlotOccupied(i)) continue;
            occupiedCount++;
            if (i != 0 && !IsSlotReady(i)) return false;
        }
        return occupiedCount >= 2;
    }

    // ── 변경 감지 (모든 클라이언트에서 실행) ─────────────────────────────

    private void OnStateChanged()
    {
        for (int i = 0; i < 4; i++)
        {
            OnSlotOccupancyChanged?.Invoke(i, IsSlotOccupied(i));
            OnSlotReadyChanged?.Invoke(i, IsSlotReady(i));
        }
        OnCanStartChanged?.Invoke(CanStartGame());
    }
}
