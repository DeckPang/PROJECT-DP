using System;
using System.Collections.Generic;
using UnityEngine;

public class SharedDeckManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private List<CardDefinition> sharedStartingDeck = new List<CardDefinition>();
    [SerializeField] private bool autoInitializeOnStart = true;

    [Header("Runtime")]
    [SerializeField] private List<CardDefinition> drawPile = new List<CardDefinition>();
    [SerializeField] private List<CardDefinition> discardPile = new List<CardDefinition>();

    public int DrawPileCount => drawPile.Count;
    public int DiscardPileCount => discardPile.Count;

    public event Action OnDeckStateChanged;

    private void Start()
    {
        if (autoInitializeOnStart)
        {
            InitializeDeck();
        }
    }

    [ContextMenu("Initialize Shared Deck")]
    public void InitializeDeck()
    {
        drawPile.Clear();
        discardPile.Clear();

        foreach (CardDefinition card in sharedStartingDeck)
        {
            if (card == null)
                continue;

            if (card.CardPoolType != CardPoolType.SharedDeck)
            {
                Debug.LogWarning($"[SharedDeck] {card.CardName} 은 SharedDeck 카드가 아니라서 시작 덱에 넣지 않았습니다.");
                continue;
            }

            drawPile.Add(card);
        }

        Shuffle(drawPile);
        Debug.Log($"[SharedDeck] 초기화 완료 | Draw={drawPile.Count}, Discard={discardPile.Count}");
        NotifyDeckChanged();
    }

    public CardDefinition DrawOne()
    {
        if (drawPile.Count == 0)
        {
            ReshuffleDiscardIntoDrawPile();
        }

        if (drawPile.Count == 0)
        {
            Debug.LogWarning("[SharedDeck] drawPile과 discardPile이 모두 비어 있습니다.");
            return null;
        }

        CardDefinition card = drawPile[0];
        drawPile.RemoveAt(0);

        Debug.Log($"[SharedDeck] 드로우: {card.CardName} | Draw={drawPile.Count}, Discard={discardPile.Count}");
        NotifyDeckChanged();
        return card;
    }

    public void AddToDiscard(CardDefinition card)
    {
        if (card == null)
            return;

        if (card.CardPoolType != CardPoolType.SharedDeck)
        {
            Debug.Log($"[SharedDeck] {card.CardName} 은 SharedDeck 순환 카드가 아니므로 discard에 넣지 않습니다.");
            return;
        }

        discardPile.Add(card);
        Debug.Log($"[SharedDeck] 버림: {card.CardName} | Draw={drawPile.Count}, Discard={discardPile.Count}");
        NotifyDeckChanged();
    }

    public void Shuffle(List<CardDefinition> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            CardDefinition temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void ReshuffleDiscardIntoDrawPile()
    {
        if (discardPile.Count == 0)
            return;

        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);

        Debug.Log($"[SharedDeck] discardPile을 섞어서 drawPile로 이동 | Draw={drawPile.Count}, Discard={discardPile.Count}");
        NotifyDeckChanged();
    }

    private void NotifyDeckChanged()
    {
        OnDeckStateChanged?.Invoke();
    }
}