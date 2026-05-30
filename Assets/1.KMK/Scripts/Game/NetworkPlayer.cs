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

    [Networked] public NetworkBool BasicWalkUsedThisTurn { get; set; }

    public const int HandCapacity = 11; // 5였는데 11로 수정함 - 여영부
    public const int FixedHandIndex = 0;  //추가
    public const string BasicWalkCardId = "basic_walk"; //추가

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
        //if (HasStateAuthority)
        //{
        //    var boardMgr = FindAnyObjectByType<BoardManager>();
        //    var startNode = boardMgr?.GetStartNode();
        //    if (startNode != null && CurrentNodeId != startNode.NodeId)
        //    {
        //        CurrentNodeId = startNode.NodeId;
        //    }

        //    // 현재 차례 슬롯이면 TurnGranted 카드 초기 지급
        //    var session = FindAnyObjectByType<GameSession>();
        //    if (session != null && session.CurrentTurnSlot == SlotIndex)
        //    {
        //        RefillTurnGrantedCards();
        //    }
        //}
        //여기 수정함
        if (HasStateAuthority)
        {
            var boardMgr = FindAnyObjectByType<BoardManager>();
            var startNode = boardMgr?.GetStartNode();
            if (startNode != null && CurrentNodeId != startNode.NodeId)
            {
                CurrentNodeId = startNode.NodeId;
            }

            DealInitialHandIfNeeded();

            var session = FindAnyObjectByType<GameSession>();
            if (session != null && session.CurrentTurnSlot == SlotIndex)
            {
                PrepareTurnStart();
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
        //if (steps <= 0) return;
        //if (IsAwaitingBranch) return;     // 이미 분기 대기 중
        //if (PendingSteps > 0) return;     // 이미 이동 중

        //var session = FindAnyObjectByType<GameSession>();
        //if (session == null || session.CurrentTurnSlot != SlotIndex) return;

        //PendingSteps = steps;
        //ProcessNextStep();

        TryStartMoveState(steps); //위에 주석처리하고 이거 추가함
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
    //-- 여기부터
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestUseCardAt(int handIndex)
    {
        if (!IsCurrentTurnOnHost()) return;
        if (IsAwaitingBranch) return;
        if (PendingSteps > 0) return;
        if (handIndex < 0 || handIndex >= HandCards.Length) return;

        string cardId = HandCards[handIndex].ToString();
        if (string.IsNullOrEmpty(cardId)) return;

        CardDefinition def = CardLibrary.GetById(cardId);
        if (def == null) return;

        bool isBasicWalk = handIndex == FixedHandIndex && cardId == BasicWalkCardId;
        if (isBasicWalk && BasicWalkUsedThisTurn) return;

        bool success = false;

        switch (def.EffectType)
        {
            case CardEffectType.RandomMove:
                {
                    int randomSteps = UnityEngine.Random.Range(0, def.Value + 1);
                    success = TryStartMoveState(randomSteps);
                    break;
                }

            case CardEffectType.Move:
                {
                    success = TryStartMoveState(def.Value);
                    break;
                }

            case CardEffectType.DrawCards:
                {
                    success = UseDrawCardEffect(def.Value);
                    break;
                }

            default:
                {
                    Debug.LogWarning($"[NetworkPlayer] 아직 KMK UI 단계에서 미구현 효과: {def.CardName} / {def.EffectType}");
                    return;
                }
        }

        if (!success) return;

        if (isBasicWalk)
        {
            BasicWalkUsedThisTurn = true;
        }
        else
        {
            RemoveCardAt(handIndex);
            RouteUsedCard(cardId, def);
        }

        Debug.Log($"[NetworkPlayer] Slot {SlotIndex} used card '{cardId}'");
    }

    private bool UseDrawCardEffect(int count)
    {
        var deck = FindAnyObjectByType<GameDeck>();
        if (deck == null) return false;

        bool drewAny = false;

        for (int i = 0; i < count; i++)
        {
            string cardId = deck.DrawTop();
            if (string.IsNullOrEmpty(cardId))
                break;

            if (!TryAddCard(cardId))
            {
                deck.AddToDiscard(cardId);
                break;
            }

            drewAny = true;
        }

        return drewAny;
    }

    private void RouteUsedCard(string cardId, CardDefinition def)
    {
        var deck = FindAnyObjectByType<GameDeck>();
        if (deck == null || def == null) return;

        if (def.CardPoolType == CardPoolType.SharedDeck)
        {
            deck.AddToDiscard(cardId);
        }
    }
    //-- 여기까지 함수추가함
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

    //    FindFreeHandSlot()
    //    TryAddCard()
    //    RemoveCardAt()
    //    RefillTurnGrantedCards()
    //    위에 함수들 주석처리함

    //public int FindFreeHandSlot()
    //{
    //    for (int i = 0; i < HandCards.Length; i++)
    //        if (string.IsNullOrEmpty(HandCards[i].ToString())) return i;
    //    return -1;
    //}

    /// <summary>Host: 손에 카드 1장 추가. 자리 없으면 false.</summary>
    //public bool TryAddCard(string cardId)
    //{
    //    if (!HasStateAuthority) return false;
    //    if (string.IsNullOrEmpty(cardId)) return false;

    //    int slot = FindFreeHandSlot();
    //    if (slot < 0) return false;

    //    HandCards.Set(slot, cardId);
    //    return true;
    //}

    /// <summary>Host: 손에서 idx 카드 제거.</summary>
    //public void RemoveCardAt(int idx)
    //{
    //    if (!HasStateAuthority) return;
    //    if (idx < 0 || idx >= HandCards.Length) return;
    //    HandCards.Set(idx, default);
    //}

    /// <summary>Host: TurnGranted 카드 중 손에 없는 것 자동 보충 (예: basic_walk).</summary>
    //public void RefillTurnGrantedCards()
    //{
    //    if (!HasStateAuthority) return;

    //    foreach (var def in CardLibrary.TurnGrantedCards)
    //    {
    //        if (def == null) continue;
    //        if (HasCardInHand(def.CardId)) continue;  // 이미 있으면 스킵
    //        TryAddCard(def.CardId);
    //    }
    //} 

    //--밑에 함수 추가
    public int FindFreeHandSlot()
    {
        for (int i = 1; i < HandCards.Length; i++)
        {
            if (string.IsNullOrEmpty(HandCards[i].ToString()))
                return i;
        }

        return -1;
    }

    /// <summary>Host: 손에 카드 1장 추가. 0번 슬롯은 basic_walk 고정 슬롯.</summary>
    public bool TryAddCard(string cardId)
    {
        if (!HasStateAuthority) return false;
        if (string.IsNullOrEmpty(cardId)) return false;

        if (cardId == BasicWalkCardId)
        {
            HandCards.Set(FixedHandIndex, cardId);
            return true;
        }

        int slot = FindFreeHandSlot();
        if (slot < 0) return false;

        HandCards.Set(slot, cardId);
        return true;
    }

    /// <summary>Host: 일반 손패 카드 제거. 0번 고정 슬롯은 제거하지 않음.</summary>
    public void RemoveCardAt(int idx)
    {
        if (!HasStateAuthority) return;
        if (idx <= FixedHandIndex || idx >= HandCards.Length) return;

        // 뒤 카드들을 앞으로 한 칸씩 당김
        for (int i = idx; i < HandCards.Length - 1; i++)
        {
            HandCards.Set(i, HandCards[i + 1]);
        }

        // 마지막 칸 비우기
        HandCards.Set(HandCards.Length - 1, default);
    }

    /// <summary>현재 턴 시작 처리: basic_walk 사용 가능 초기화 + 고정 슬롯 보정 + 턴 시작 드로우 1장.</summary>
    public void PrepareTurnStart()
    {
        if (!HasStateAuthority) return;

        BasicWalkUsedThisTurn = false;
        EnsureFixedBasicCard();
        DrawTurnStartCards(1);
    }

    private void EnsureFixedBasicCard()
    {
        if (string.IsNullOrEmpty(HandCards[FixedHandIndex].ToString()))
        {
            HandCards.Set(FixedHandIndex, BasicWalkCardId);
        }
    }

    private void DrawTurnStartCards(int count)
    {
        var deck = FindAnyObjectByType<GameDeck>();
        if (deck == null) return;

        for (int i = 0; i < count; i++)
        {
            string cardId = deck.DrawTop();
            if (string.IsNullOrEmpty(cardId))
                break;

            if (!TryAddCard(cardId))
            {
                deck.AddToDiscard(cardId);
                break;
            }
        }
    }

    private void DealInitialHandIfNeeded()
    {
        int normalCardCount = 0;

        for (int i = 1; i < HandCards.Length; i++)
        {
            if (!string.IsNullOrEmpty(HandCards[i].ToString()))
                normalCardCount++;
        }

        if (normalCardCount > 0)
            return;

        var deck = FindAnyObjectByType<GameDeck>();
        if (deck == null) return;

        for (int i = 0; i < 5; i++)
        {
            string cardId = deck.DrawTop();
            if (string.IsNullOrEmpty(cardId))
                break;

            if (!TryAddCard(cardId))
            {
                deck.AddToDiscard(cardId);
                break;
            }
        }
    }

    private bool IsCurrentTurnOnHost()
    {
        var session = FindAnyObjectByType<GameSession>();
        return session != null && session.CurrentTurnSlot == SlotIndex;
    }

    private bool TryStartMoveState(int steps)
    {
        if (!HasStateAuthority) return false;
        if (!IsCurrentTurnOnHost()) return false;
        if (IsAwaitingBranch) return false;
        if (PendingSteps > 0) return false;
        if (steps < 0) return false;

        PendingSteps = steps;

        if (steps > 0)
            ProcessNextStep();
        else
            Debug.Log($"[NetworkPlayer] Slot {SlotIndex} used move card with 0 steps.");

        return true;
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
