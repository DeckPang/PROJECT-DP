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

    /// <summary>현재 진행 중인 라운드 번호(1부터). 한 라운드 = 점유된 모든 슬롯이 한 턴씩 완료.</summary>
    [Networked, OnChangedRender(nameof(OnRoundChanged))]
    public int RoundNumber { get; set; }

    /// <summary>이번 라운드에서 지금까지 종료된 턴 수 (Host 집계용).</summary>
    [Networked] public int TurnsThisRound { get; set; }

    // ── 이벤트 (UI 구독용) ───────────────────────────────────────────

    public event Action TurnChanged;

    /// <summary>한 라운드가 완료된 직후(= 미니게임 트리거 지점). 인자: 방금 완료된 라운드 번호. 모든 클라에서 발생.</summary>
    public event Action<int> RoundCompleted;

    /// <summary>트로피 몇 개를 먼저 모으면 승리인지 (기획서: 3개).</summary>
    public const int TrophiesToWin = 3;

    /// <summary>게임 승자 슬롯. -1이면 진행 중.</summary>
    [Networked, OnChangedRender(nameof(OnWinnerChanged))]
    public int WinnerSlot { get; set; }

    /// <summary>승자가 확정되어 게임이 끝난 순간 모든 클라에서 발생. 인자: 승자 슬롯.</summary>
    public event Action<int> GameEnded;

    /// <summary>트로피 1개 구매에 필요한 코인 (마리오파티식). 프로토타입 임시값.</summary>
    public const int TrophyPrice = 10;

    /// <summary>현재 트로피가 놓인 노드 Id. -1이면 없음. 도착 시 코인으로 구매.</summary>
    [Networked, OnChangedRender(nameof(OnTrophyNodeChanged))]
    public int TrophyNodeId { get; set; }

    /// <summary>트로피 노드 위치가 바뀌었을 때 모든 클라에서 발생 (표식 갱신용).</summary>
    public event Action TrophyNodeChanged;

    // ── 턴 타이머 (기획서 2.3.2) ─────────────────────────────────────

    /// <summary>턴 기본 제한 시간(초). 기획서: 10초.</summary>
    public const float BaseTurnSeconds = 10f;

    /// <summary>카드 1장 사용 시 연장되는 시간(초). 기획서: +10초.</summary>
    public const float CardExtendSeconds = 10f;

    [Networked] private TickTimer TurnTimer { get; set; }

    /// <summary>현재 턴 남은 시간(초). 타이머 미작동 시 0. 모든 클라에서 조회 가능.</summary>
    public float TurnSecondsRemaining => TurnTimer.RemainingTime(Runner) ?? 0f;

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
            RoundNumber     = 1;
            TurnsThisRound  = 0;
            WinnerSlot      = -1;
            PickRandomTrophyNode();
            StartTurnTimer();
        }

        // 늦게 도착한 클라가 즉시 UI를 그릴 수 있도록 한 번 강제 발생
        TurnChanged?.Invoke();
    }

    // ── Host 전용 API ────────────────────────────────────────────────

    public void AdvanceTurn()
    {
        if (!HasStateAuthority) return;
        if (WinnerSlot >= 0) return;   // 이미 게임 종료

        // 방금 끝난 턴을 라운드 카운트에 반영. 점유된 모든 슬롯이 한 턴씩 끝내면 라운드 완료 → 미니게임 트리거.
        TurnsThisRound++;
        int occupied = CountOccupiedSlots();
        if (occupied > 0 && TurnsThisRound >= occupied)
        {
            TurnsThisRound = 0;
            RoundNumber++;   // OnChangedRender(OnRoundChanged) → 모든 클라에 RoundCompleted 발생
            Debug.Log($"[GameSession] 라운드 {RoundNumber - 1} 완료 → 미니게임");

            // 미니게임 순위별 코인 보상 + 1등 슬롯 반환. 다음 라운드는 1등부터 시작(선공).
            int firstSlot = ResolveMiniGameRewards();
            StartNextRoundFrom(firstSlot);
            return;
        }

        AdvanceToNextOccupied();
    }

    /// <summary>점유된 다음 슬롯으로 턴을 넘김 (라운드 내 일반 진행).</summary>
    private void AdvanceToNextOccupied()
    {
        for (int step = 1; step <= MaxSlots; step++)
        {
            int next = (CurrentTurnSlot + step) % MaxSlots;
            var nextPlayer = FindPlayerInSlot(next);
            if (nextPlayer != null)
            {
                CurrentTurnSlot = next;
                TurnNumber++;
                Debug.Log($"[GameSession] Turn {TurnNumber} → Slot {CurrentTurnSlot}");
                nextPlayer.PrepareTurnStart();
                StartTurnTimer();
                return;
            }
        }

        Debug.LogWarning("[GameSession] 점유된 슬롯이 없어 턴을 넘길 수 없음.");
    }

    /// <summary>다음 라운드를 지정 슬롯(미니게임 1등)부터 시작 — 선공 보상.</summary>
    private void StartNextRoundFrom(int slot)
    {
        var first = FindPlayerInSlot(slot);
        if (first == null) { AdvanceToNextOccupied(); return; }

        CurrentTurnSlot = slot;
        TurnNumber++;
        Debug.Log($"[GameSession] 라운드 {RoundNumber} 시작 → 선공 Slot {slot}");
        first.PrepareTurnStart();
        StartTurnTimer();
    }

    // ── 턴 타이머 처리 (Host 전용) ───────────────────────────────────

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (WinnerSlot >= 0) return;
        if (!TurnTimer.Expired(Runner)) return;

        // 현재 플레이어가 이동/분기 중이면 끝날 때까지 자동 종료 보류.
        var current = FindPlayerInSlot(CurrentTurnSlot);
        if (current != null && (current.PendingSteps > 0 || current.IsAwaitingBranch))
            return;

        Debug.Log($"[GameSession] 턴 시간 초과 → 자동 턴 종료 (Slot {CurrentTurnSlot})");
        AdvanceTurn();
    }

    /// <summary>새 턴 시작 — 제한 시간을 기본값으로 리셋 (Host 전용).</summary>
    private void StartTurnTimer()
    {
        if (!HasStateAuthority) return;
        TurnTimer = TickTimer.CreateFromSeconds(Runner, BaseTurnSeconds);
    }

    /// <summary>턴 제한 시간 연장 (Host 전용). 카드 사용 시마다 호출.</summary>
    public void ExtendTurn(float seconds)
    {
        if (!HasStateAuthority) return;
        if (seconds <= 0f) return;

        float remaining = TurnTimer.RemainingTime(Runner) ?? 0f;
        TurnTimer = TickTimer.CreateFromSeconds(Runner, remaining + seconds);
        Debug.Log($"[GameSession] 턴 시간 +{seconds:0}s → {remaining + seconds:0.0}s 남음");
    }

    private static NetworkPlayer FindPlayerInSlot(int slot)
    {
        var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (var p in players)
            if (p != null && p.SlotIndex == slot) return p;
        return null;
    }

    /// <summary>현재 게임에 존재하는(점유된) NetworkPlayer 수.</summary>
    private static int CountOccupiedSlots()
    {
        return FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Length;
    }

    /// <summary>미니게임 순위별 코인 보상. rank 0(1등)부터 지급량.</summary>
    private static readonly int[] MiniGameCoinsByRank = { 10, 6, 3, 0 };

    /// <summary>
    /// 라운드 완료 시 미니게임 결과 처리 (Host 전용). 순위별 코인 지급 + 1등 슬롯 반환(선공용).
    /// 슬라이스1(현재): 순위를 랜덤 산출(스텁). 슬라이스2(예정): 실제 미니게임 씬 결과로 교체.
    /// 트로피는 여기서 주지 않음 — 맵의 트로피 노드에서 코인으로 구매(마리오파티식).
    /// </summary>
    private int ResolveMiniGameRewards()
    {
        if (!HasStateAuthority) return CurrentTurnSlot;

        var players = new System.Collections.Generic.List<NetworkPlayer>(
            FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None));
        if (players.Count == 0) return CurrentTurnSlot;

        // 스텁: 랜덤 순위 (슬라이스2에서 실제 미니게임 결과로 교체)
        for (int i = 0; i < players.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, players.Count);
            (players[i], players[j]) = (players[j], players[i]);
        }

        for (int rank = 0; rank < players.Count; rank++)
        {
            int coins = rank < MiniGameCoinsByRank.Length ? MiniGameCoinsByRank[rank] : 0;
            if (coins > 0) players[rank].AddCoins(coins);
            Debug.Log($"[GameSession] 미니게임 {rank + 1}등 = Slot {players[rank].SlotIndex} (+{coins} 코인)");
        }

        return players[0].SlotIndex;   // 1등 → 다음 라운드 선공
    }

    /// <summary>트로피를 놓을 노드를 무작위로 선택 (Host 전용). Start 칸과 현재 위치는 제외.</summary>
    private void PickRandomTrophyNode()
    {
        if (!HasStateAuthority) return;

        var boardMgr = FindAnyObjectByType<BoardManager>();
        if (boardMgr == null || boardMgr.Nodes == null || boardMgr.Nodes.Count == 0)
        {
            TrophyNodeId = -1;
            Debug.LogWarning("[GameSession] BoardManager/노드를 찾지 못해 트로피 노드를 지정하지 못했습니다.");
            return;
        }

        var candidates = new System.Collections.Generic.List<int>();
        foreach (var n in boardMgr.Nodes)
        {
            if (n == null) continue;
            if (n.NodeType == BoardNodeType.Start) continue;
            if (n.NodeId == TrophyNodeId) continue;
            candidates.Add(n.NodeId);
        }
        if (candidates.Count == 0) { TrophyNodeId = -1; return; }

        TrophyNodeId = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        Debug.Log($"[GameSession] 트로피 노드 → Node {TrophyNodeId} (가격 {TrophyPrice} 코인)");
    }

    /// <summary>Host — 플레이어가 트로피 노드에 도착했고 코인이 충분하면 트로피 구매(마리오파티식).</summary>
    public void TryBuyTrophyAt(NetworkPlayer player)
    {
        if (!HasStateAuthority || player == null) return;
        if (WinnerSlot >= 0) return;
        if (TrophyNodeId < 0 || player.CurrentNodeId != TrophyNodeId) return;

        if (!player.TrySpendCoins(TrophyPrice))
        {
            Debug.Log($"[GameSession] Slot {player.SlotIndex} 트로피 노드 도착 — 코인 부족 ({player.Coins}/{TrophyPrice})");
            return;
        }

        player.AddTrophy(1);
        Debug.Log($"[GameSession] Slot {player.SlotIndex} 트로피 구매! (코인 -{TrophyPrice})");
        PickRandomTrophyNode();   // 구매 후 트로피는 다른 노드로 이동
        CheckWinCondition();
    }

    /// <summary>트로피 3개 이상 보유자가 있으면 승자 확정 (Host 전용). 트로피 노드 구매 후 호출.</summary>
    public void CheckWinCondition()
    {
        if (!HasStateAuthority) return;
        if (WinnerSlot >= 0) return;

        foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            if (p != null && p.Trophies >= TrophiesToWin)
            {
                WinnerSlot = p.SlotIndex;
                Debug.Log($"[GameSession] 게임 종료! 승자 = Slot {WinnerSlot} (트로피 {p.Trophies})");
                return;
            }
        }
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

    // RoundNumber가 증가한 순간(라운드 완료) 모든 클라이언트에서 호출됨.
    private void OnRoundChanged()
    {
        // RoundNumber는 이미 다음 라운드 번호로 증가된 상태 → 방금 완료된 라운드는 RoundNumber - 1.
        int completedRound = RoundNumber - 1;
        if (completedRound >= 1)
            RoundCompleted?.Invoke(completedRound);
    }

    // WinnerSlot이 확정된 순간 모든 클라이언트에서 호출됨.
    private void OnWinnerChanged()
    {
        if (WinnerSlot >= 0)
            GameEnded?.Invoke(WinnerSlot);
    }

    // 트로피 노드 위치가 바뀐 순간 모든 클라이언트에서 호출됨.
    private void OnTrophyNodeChanged()
    {
        TrophyNodeChanged?.Invoke();
    }
}
