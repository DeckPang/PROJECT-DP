using System;
using Fusion;
using UnityEngine;

/// <summary>
/// 게임 씬의 플레이어 1명을 대표하는 NetworkBehaviour.
/// Host가 슬롯마다 1개씩 Spawn하며, InputAuthority는 해당 PlayerRef.
/// 향후: 위치, HP, 마나, 손패, 캐릭터 ID 등이 여기 누적될 예정.
/// 현재(Phase 1.3)는 SlotIndex/PlayerName만 보유 — "존재 확인" 단계.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    // ── Networked 상태 ───────────────────────────────────────────────

    [Networked] public int SlotIndex { get; set; }

    [Networked] public NetworkString<_16> PlayerName { get; set; }

    [Networked, OnChangedRender(nameof(OnPositionChanged))]
    public int CurrentNodeId { get; set; }

    [Networked, OnChangedRender(nameof(OnBranchStateChanged))]
    public NetworkBool IsAwaitingBranch { get; set; }

    [Networked] public int PendingSteps { get; set; }

    public const int HandCapacity = 5;

    /// <summary>손에 든 카드들. 빈 슬롯은 빈 문자열. CardId로 CardLibrary 조회.</summary>
    [Networked, Capacity(HandCapacity), OnChangedRender(nameof(OnHandChanged))]
    public NetworkArray<NetworkString<_32>> HandCards => default;

    // ── 편의 프로퍼티 ────────────────────────────────────────────────

    /// <summary>이 NetworkPlayer를 소유한 실제 플레이어 (Fusion PlayerRef).</summary>
    public PlayerRef Owner => Object.InputAuthority;

    /// <summary>해당 슬롯이 호스트(0번)인가.</summary>
    public bool IsHostSlot => SlotIndex == 0;

    // ── 이벤트 (View가 구독) ────────────────────────────────────────

    /// <summary>이 NetworkPlayer가 등장하거나 사라질 때 호출 (정적: 누구든 구독 가능).</summary>
    public static event Action<NetworkPlayer> OnSpawnedStatic;
    public static event Action<NetworkPlayer> OnDespawnedStatic;

    /// <summary>CurrentNodeId가 변경됐을 때 호출 (이 인스턴스 한정).</summary>
    public event Action PositionChanged;

    /// <summary>IsAwaitingBranch가 변경됐을 때 호출 (이 인스턴스 한정).</summary>
    public event Action BranchStateChanged;

    /// <summary>HandCards가 변경됐을 때 호출.</summary>
    public event Action HandChanged;

    // ── 생명주기 ─────────────────────────────────────────────────────

    public override void Spawned()
    {
        Debug.Log($"[NetworkPlayer] Slot {SlotIndex} | Name '{PlayerName}' | Owner {Owner} | " +
                  $"HasInput={HasInputAuthority}, HasState={HasStateAuthority}");

        // Host: 시작 노드로 초기화 + 현재 턴이면 TurnGranted 지급
        if (HasStateAuthority)
        {
            var boardMgr = FindAnyObjectByType<BoardManager>();
            var startNode = boardMgr?.GetStartNode();
            if (startNode != null && CurrentNodeId != startNode.NodeId)
            {
                CurrentNodeId = startNode.NodeId;
            }

            // 현재 차례 슬롯이면 TurnGranted 카드 초기 지급
            var session = FindAnyObjectByType<GameSession>();
            if (session != null && session.CurrentTurnSlot == SlotIndex)
            {
                RefillTurnGrantedCards();
            }
        }

        OnSpawnedStatic?.Invoke(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        OnDespawnedStatic?.Invoke(this);
    }

    // ── RPC: Client(InputAuthority) → Host(StateAuthority) ──────────

    /// <summary>이 플레이어를 N칸 앞으로 이동 요청. 자기 차례일 때만 적용.</summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestMove(int steps)
    {
        if (steps <= 0) return;
        if (IsAwaitingBranch) return;     // 이미 분기 대기 중
        if (PendingSteps > 0) return;     // 이미 이동 중

        var session = FindAnyObjectByType<GameSession>();
        if (session == null || session.CurrentTurnSlot != SlotIndex) return;

        PendingSteps = steps;
        ProcessNextStep();
    }

    /// <summary>공유 덱에서 카드 1장 뽑기 요청. 자기 차례일 때만 유효.</summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestDraw()
    {
        var session = FindAnyObjectByType<GameSession>();
        if (session == null || session.CurrentTurnSlot != SlotIndex) return;

        var deck = FindAnyObjectByType<GameDeck>();
        if (deck == null) return;

        string cardId = deck.DrawTop();
        if (string.IsNullOrEmpty(cardId)) return;

        if (!TryAddCard(cardId))
        {
            // 손이 꽉 차서 못 받음 → 다시 discard
            deck.AddToDiscard(cardId);
            Debug.LogWarning($"[NetworkPlayer] Slot {SlotIndex} hand full, drawn card discarded.");
        }
        else
        {
            Debug.Log($"[NetworkPlayer] Slot {SlotIndex} drew '{cardId}'");
        }
    }

    /// <summary>분기 노드에서 길 선택. IsAwaitingBranch일 때만 유효.</summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ChooseBranch(int branchIndex)
    {
        if (!IsAwaitingBranch) return;

        var boardMgr = FindAnyObjectByType<BoardManager>();
        if (boardMgr == null) return;

        var current = boardMgr.GetNodeById(CurrentNodeId);
        if (current == null || current.NextNodes == null) return;
        if (branchIndex < 0 || branchIndex >= current.NextNodes.Count) return;

        var next = current.NextNodes[branchIndex];
        if (next == null) return;

        CurrentNodeId    = next.NodeId;
        PendingSteps     = Mathf.Max(0, PendingSteps - 1);
        IsAwaitingBranch = false;

        ProcessNextStep();
    }

    /// <summary>Host 전용 — PendingSteps만큼 외길로 진행, 분기 만나면 대기 상태로 진입.</summary>
    private void ProcessNextStep()
    {
        if (!HasStateAuthority) return;

        var boardMgr = FindAnyObjectByType<BoardManager>();
        if (boardMgr == null) return;

        while (PendingSteps > 0)
        {
            var node = boardMgr.GetNodeById(CurrentNodeId);
            if (node == null || node.NextNodes == null || node.NextNodes.Count == 0)
            {
                // 더 이상 갈 수 없음
                PendingSteps = 0;
                Debug.Log($"[NetworkPlayer] Slot {SlotIndex} dead-end at Node {CurrentNodeId}");
                return;
            }

            if (node.NextNodes.Count == 1)
            {
                // 외길 — 자동 진행
                CurrentNodeId = node.NextNodes[0].NodeId;
                PendingSteps--;
            }
            else
            {
                // 분기 — 입력 대기
                IsAwaitingBranch = true;
                Debug.Log($"[NetworkPlayer] Slot {SlotIndex} awaiting branch at Node {CurrentNodeId} (steps left: {PendingSteps})");
                return;
            }
        }

        Debug.Log($"[NetworkPlayer] Slot {SlotIndex} move complete → Node {CurrentNodeId}");
    }

    // ── 손패 조작 (Host 전용) ────────────────────────────────────────

    public bool HasCardInHand(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        for (int i = 0; i < HandCards.Length; i++)
            if (HandCards[i].ToString() == cardId) return true;
        return false;
    }

    public int FindFreeHandSlot()
    {
        for (int i = 0; i < HandCards.Length; i++)
            if (string.IsNullOrEmpty(HandCards[i].ToString())) return i;
        return -1;
    }

    /// <summary>Host: 손에 카드 1장 추가. 자리 없으면 false.</summary>
    public bool TryAddCard(string cardId)
    {
        if (!HasStateAuthority) return false;
        if (string.IsNullOrEmpty(cardId)) return false;

        int slot = FindFreeHandSlot();
        if (slot < 0) return false;

        HandCards.Set(slot, cardId);
        return true;
    }

    /// <summary>Host: 손에서 idx 카드 제거.</summary>
    public void RemoveCardAt(int idx)
    {
        if (!HasStateAuthority) return;
        if (idx < 0 || idx >= HandCards.Length) return;
        HandCards.Set(idx, default);
    }

    /// <summary>Host: TurnGranted 카드 중 손에 없는 것 자동 보충 (예: basic_walk).</summary>
    public void RefillTurnGrantedCards()
    {
        if (!HasStateAuthority) return;

        foreach (var def in CardLibrary.TurnGrantedCards)
        {
            if (def == null) continue;
            if (HasCardInHand(def.CardId)) continue;  // 이미 있으면 스킵
            TryAddCard(def.CardId);
        }
    }

    // ── 변경 감지 ────────────────────────────────────────────────────

    private void OnPositionChanged()
    {
        PositionChanged?.Invoke();
    }

    private void OnBranchStateChanged()
    {
        BranchStateChanged?.Invoke();
    }

    private void OnHandChanged()
    {
        HandChanged?.Invoke();
    }
}
