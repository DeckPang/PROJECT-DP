using System;
using Fusion;
using UnityEngine;

/// <summary>
/// 게임 씬의 최상위 NetworkBehaviour. Host가 1개만 Spawn.
/// 턴 진행/페이즈/타이머 등 게임 전체 상태를 보유합니다.
/// </summary>
public class GameSession : NetworkBehaviour
{
    /// <summary>Phase 1.3까지 임시 — 추후 NetworkPlayer 수로 대체.</summary>
    public const int MaxSlots = 4;

    // ── Networked 상태 ───────────────────────────────────────────────

    [Networked, OnChangedRender(nameof(OnTurnChanged))]
    public int CurrentTurnSlot { get; set; }

    [Networked, OnChangedRender(nameof(OnTurnChanged))]
    public int TurnNumber { get; set; }

    // ── 이벤트 (UI 구독용) ───────────────────────────────────────────

    public event Action TurnChanged;

    // ── 생명주기 ─────────────────────────────────────────────────────

    public override void Spawned()
    {
        Debug.Log($"[GameSession] Spawned (HasStateAuthority={HasStateAuthority})");

        // GameController에 자신을 알림 (Client 동기화 타이밍 안전판)
        var ctrl = FindAnyObjectByType<GameController>();
        if (ctrl != null) ctrl.RegisterSession(this);

        // Host: 초기값 세팅
        if (HasStateAuthority)
        {
            CurrentTurnSlot = 0;
            TurnNumber      = 1;
        }

        // 늦게 도착한 클라가 즉시 UI를 그릴 수 있도록 한 번 강제 발생
        TurnChanged?.Invoke();
    }

    // ── Host 전용 API ────────────────────────────────────────────────

    public void AdvanceTurn()
    {
        if (!HasStateAuthority) return;

        // 점유된 다음 슬롯까지 건너뜀
        for (int step = 1; step <= MaxSlots; step++)
        {
            int next = (CurrentTurnSlot + step) % MaxSlots;
            var nextPlayer = FindPlayerInSlot(next);
            if (nextPlayer != null)
            {
                CurrentTurnSlot = next;
                TurnNumber++;
                Debug.Log($"[GameSession] Turn {TurnNumber} → Slot {CurrentTurnSlot}");

                // 새 차례 플레이어에게 TurnGranted 카드 자동 지급
                nextPlayer.RefillTurnGrantedCards();
                return;
            }
        }

        Debug.LogWarning("[GameSession] 점유된 슬롯이 없어 턴을 넘길 수 없음.");
    }

    private static NetworkPlayer FindPlayerInSlot(int slot)
    {
        var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (var p in players)
            if (p != null && p.SlotIndex == slot) return p;
        return null;
    }

    // ── RPC: Client → Host ──────────────────────────────────────────

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestEndTurn(RpcInfo info = default)
    {
        // Phase 1.2: 디버그 목적 — 누구나 호출 가능
        // TODO Phase 1.3+: info.Source가 현재 턴의 슬롯 주인인지 검증
        AdvanceTurn();
    }

    // ── 변경 감지 (모든 클라이언트에서 호출됨) ────────────────────────

    private void OnTurnChanged()
    {
        TurnChanged?.Invoke();
    }
}
