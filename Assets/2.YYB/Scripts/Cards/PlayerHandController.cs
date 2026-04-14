using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CharacterInfo ownerInfo;
    [SerializeField] private PlayerPawn ownerPawn;
    [SerializeField] private SharedDeckManager sharedDeckManager;

    [Header("Hand Setup")]
    [SerializeField] private int handLimit = 7;
    [SerializeField] private int drawCountAtTurnStart = 1;
    [SerializeField] private CardDefinition turnGrantedBasicMoveCard;

    [Header("Runtime")]
    [SerializeField] private List<CardDefinition> hand = new List<CardDefinition>();
    [SerializeField] private List<CardDefinition> usedRewardCards = new List<CardDefinition>();

    public IReadOnlyList<CardDefinition> Hand => hand;
    public int HandCount => hand.Count;

    public event Action OnHandChanged;

    private void Awake()
    {
        if (ownerInfo == null)
            ownerInfo = GetComponent<CharacterInfo>();

        if (ownerPawn == null)
            ownerPawn = GetComponent<PlayerPawn>();
    }

    public void StartTurnLocal()
    {
        RemoveOldTurnGrantedCards();

        ownerInfo?.RestoreManaToFull();
        GrantBasicMoveCard();
        DrawFromSharedDeck(drawCountAtTurnStart);

        Debug.Log($"[Hand] 턴 시작 처리 완료 | Hand={hand.Count}");
        RaiseHandChanged();
    }

    public void DrawFromSharedDeck(int count)
    {
        if (sharedDeckManager == null)
        {
            Debug.LogWarning("[Hand] SharedDeckManager reference가 없습니다.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (hand.Count >= handLimit)
            {
                Debug.LogWarning("[Hand] 손패가 가득 차서 더 드로우할 수 없습니다.");
                break;
            }

            CardDefinition card = sharedDeckManager.DrawOne();
            if (card == null)
                break;

            hand.Add(card);
        }

        RaiseHandChanged();
    }

    public void AddRewardCardToHand(CardDefinition rewardCard)
    {
        if (rewardCard == null)
        {
            Debug.LogWarning("[Hand] rewardCard가 null 입니다.");
            return;
        }

        if (hand.Count >= handLimit)
        {
            Debug.LogWarning("[Hand] 손패가 가득 차서 보상 카드를 받을 수 없습니다.");
            return;
        }

        hand.Add(rewardCard);
        Debug.Log($"[Hand] 보상 카드 획득: {rewardCard.CardName}");
        RaiseHandChanged();
    }

    public bool TryUseCardByIndex(int handIndex)
    {
        if (handIndex < 0 || handIndex >= hand.Count)
        {
            Debug.LogWarning("[Hand] 잘못된 handIndex 입니다.");
            return false;
        }

        CardDefinition card = hand[handIndex];

        if (ownerInfo != null && card.Cost > 0 && !ownerInfo.TrySpendMana(card.Cost))
            return false;

        bool success = ResolveCardEffect(card);

        if (!success)
        {
            if (ownerInfo != null && card.Cost > 0)
                ownerInfo.ChangeMana(card.Cost);

            Debug.LogWarning($"[Hand] 카드 효과 처리 실패: {card.CardName}");
            return false;
        }

        hand.RemoveAt(handIndex);
        RouteUsedCard(card);

        Debug.Log($"[Hand] 카드 사용 완료: {card.CardName}");
        RaiseHandChanged();
        return true;
    }

    private bool ResolveCardEffect(CardDefinition card)
    {
        switch (card.EffectType)
        {
            case CardEffectType.RandomMove:
                if (ownerPawn == null) return false;

                int randomStep = UnityEngine.Random.Range(0, card.Value + 1);
                Debug.Log($"[Hand] RandomMove: {card.CardName} -> {randomStep}칸");
                return ownerPawn.MoveSteps(randomStep);

            case CardEffectType.Move:
                if (ownerPawn == null) return false;

                Debug.Log($"[Hand] Move: {card.CardName} -> {card.Value}칸");
                return ownerPawn.MoveSteps(card.Value);

            case CardEffectType.HealSelf:
                if (ownerInfo == null) return false;

                ownerInfo.ChangeHp(card.Value);
                return true;

            case CardEffectType.DrawCards:
                DrawFromSharedDeck(card.Value);
                return true;

            case CardEffectType.GainMana:
                if (ownerInfo == null) return false;

                ownerInfo.ChangeMana(card.Value);
                return true;

            case CardEffectType.InstallTrap:
                BoardNode targetNode = GetDefaultTargetNode();
                if (targetNode == null)
                    return false;

                string installerName = ownerInfo != null ? ownerInfo.UserName : gameObject.name;
                targetNode.InstallTrap(card.TrapType, installerName);
                return true;
        }

        return false;
    }

    private BoardNode GetDefaultTargetNode()
    {
        if (ownerPawn == null || ownerPawn.CurrentNode == null)
            return null;

        List<BoardNode> nextNodes = ownerPawn.CurrentNode.NextNodes;
        if (nextNodes == null || nextNodes.Count == 0)
            return null;

        return nextNodes[0];
    }

    private void RouteUsedCard(CardDefinition card)
    {
        switch (card.CardPoolType)
        {
            case CardPoolType.SharedDeck:
                sharedDeckManager?.AddToDiscard(card);
                break;

            case CardPoolType.Reward:
                usedRewardCards.Add(card);
                Debug.Log($"[Hand] Reward 카드 사용 후 임시 usedRewardCards에 보관: {card.CardName}");
                break;

            case CardPoolType.TurnGranted:
                Debug.Log($"[Hand] TurnGranted 카드 사용 후 제거: {card.CardName}");
                break;
        }
    }

    private void GrantBasicMoveCard()
    {
        if (turnGrantedBasicMoveCard == null)
        {
            Debug.LogWarning("[Hand] 턴 지급 기본 카드가 비어 있습니다.");
            return;
        }

        if (hand.Count >= handLimit)
        {
            Debug.LogWarning("[Hand] 손패가 가득 차서 뚜벅뚜벅을 지급할 수 없습니다.");
            return;
        }

        hand.Add(turnGrantedBasicMoveCard);
        Debug.Log($"[Hand] 턴 시작 기본 카드 지급: {turnGrantedBasicMoveCard.CardName}");
    }

    private void RemoveOldTurnGrantedCards()
    {
        int removedCount = hand.RemoveAll(card => card != null && card.CardPoolType == CardPoolType.TurnGranted);

        if (removedCount > 0)
        {
            Debug.Log($"[Hand] 이전 턴 TurnGranted 카드 제거: {removedCount}장");
        }
    }

    public void PrintHandDebug()
    {
        if (hand.Count == 0)
        {
            Debug.Log("[Hand] 현재 손패가 비어 있습니다.");
            return;
        }

        for (int i = 0; i < hand.Count; i++)
        {
            CardDefinition card = hand[i];
            Debug.Log($"[Hand] Index={i} | {card.CardName} | Cost={card.Cost} | Pool={card.CardPoolType} | Effect={card.EffectType}");
        }
    }

    private void RaiseHandChanged()
    {
        OnHandChanged?.Invoke();
    }
}