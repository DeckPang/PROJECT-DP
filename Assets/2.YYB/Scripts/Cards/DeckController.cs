using System.Collections.Generic;
using UnityEngine;

public class DeckController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CharacterInfo ownerInfo;
    [SerializeField] private PlayerPawn ownerPawn;

    [Header("Deck Setup")]
    [SerializeField] private List<CardDefinition> startingDeck = new List<CardDefinition>();
    [SerializeField] private int handLimit = 5;
    [SerializeField] private bool autoInitializeOnStart = true;

    [Header("Runtime")]
    [SerializeField] private List<CardDefinition> drawPile = new List<CardDefinition>();
    [SerializeField] private List<CardDefinition> discardPile = new List<CardDefinition>();
    [SerializeField] private List<CardDefinition> hand = new List<CardDefinition>();

    public List<CardDefinition> Hand => hand;

    private void Start()
    {
        if (ownerInfo == null)
            ownerInfo = GetComponent<CharacterInfo>();

        if (ownerPawn == null)
            ownerPawn = GetComponent<PlayerPawn>();

        if (autoInitializeOnStart)
            InitializeDeck();
    }

    [ContextMenu("Initialize Deck")]
    public void InitializeDeck()
    {
        drawPile.Clear();
        discardPile.Clear();
        hand.Clear();

        drawPile.AddRange(startingDeck);
        Shuffle(drawPile);

        Debug.Log($"[Deck] 덱 초기화 완료 | DrawPile = {drawPile.Count}");
    }

    public void Shuffle(List<CardDefinition> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            CardDefinition temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public CardDefinition DrawCard()
    {
        if (hand.Count >= handLimit)
        {
            Debug.LogWarning("[Deck] 손패가 가득 차서 드로우할 수 없습니다.");
            return null;
        }

        if (drawPile.Count == 0)
        {
            ReshuffleDiscardIntoDrawPile();
        }

        if (drawPile.Count == 0)
        {
            Debug.LogWarning("[Deck] DrawPile과 DiscardPile이 모두 비어 있습니다.");
            return null;
        }

        CardDefinition card = drawPile[0];
        drawPile.RemoveAt(0);
        hand.Add(card);

        Debug.Log($"[Deck] 드로우: {card.CardName} | Hand = {hand.Count}, DrawPile = {drawPile.Count}");
        return card;
    }

    public void DrawMany(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard();
        }
    }

    public bool UseCard(int handIndex, BoardNode targetNode = null)
    {
        if (handIndex < 0 || handIndex >= hand.Count)
        {
            Debug.LogWarning("[Deck] 잘못된 handIndex 입니다.");
            return false;
        }

        CardDefinition card = hand[handIndex];

        if (ownerInfo != null && !ownerInfo.TrySpendMana(card.Cost))
            return false;

        bool success = ResolveCardEffect(card, targetNode);

        if (!success)
        {
            if (ownerInfo != null)
                ownerInfo.ChangeMana(card.Cost);

            Debug.LogWarning($"[Deck] 카드 효과 처리 실패: {card.CardName}");
            return false;
        }

        hand.RemoveAt(handIndex);
        discardPile.Add(card);

        Debug.Log($"[Deck] 카드 사용: {card.CardName} | Hand = {hand.Count}, Discard = {discardPile.Count}");
        return true;
    }

    private bool ResolveCardEffect(CardDefinition card, BoardNode targetNode)
    {
        switch (card.EffectType)
        {
            case CardEffectType.RandomMove:
                if (ownerPawn == null) return false;

                int randomStep = Random.Range(0, card.Value + 1);
                Debug.Log($"[Deck] RandomMove 발동 | {card.CardName} -> {randomStep}칸 이동");
                ownerPawn.MoveSteps(randomStep);
                return true;

            case CardEffectType.Move:
                if (ownerPawn == null) return false;

                Debug.Log($"[Deck] Move 발동 | {card.CardName} -> {card.Value}칸 이동");
                ownerPawn.MoveSteps(card.Value);
                return true;

            case CardEffectType.HealSelf:
                if (ownerInfo == null) return false;

                ownerInfo.ChangeHp(card.Value);
                return true;

            case CardEffectType.DrawCards:
                DrawMany(card.Value);
                return true;

            case CardEffectType.GainMana:
                if (ownerInfo == null) return false;

                ownerInfo.ChangeMana(card.Value);
                return true;

            case CardEffectType.InstallTrap:
                if (targetNode == null)
                {
                    targetNode = GetDefaultTargetNode();
                }

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

    private void ReshuffleDiscardIntoDrawPile()
    {
        if (discardPile.Count == 0)
            return;

        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);

        Debug.Log($"[Deck] DiscardPile을 DrawPile로 섞었습니다. | DrawPile = {drawPile.Count}");
    }

    public void PrintHand()
    {
        if (hand.Count == 0)
        {
            Debug.Log("[Deck] 현재 손패가 비어 있습니다.");
            return;
        }

        for (int i = 0; i < hand.Count; i++)
        {
            CardDefinition card = hand[i];
            Debug.Log($"[Deck] Hand[{i}] {card.CardName} | Cost={card.Cost} | Effect={card.EffectType}");
        }
    }
}