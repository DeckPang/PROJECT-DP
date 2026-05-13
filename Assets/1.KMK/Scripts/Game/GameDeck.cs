using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// 공유 덱. Host가 1개만 Spawn하고 내부 상태(drawPile/discardPile)는 Host에서만 관리.
/// 모든 클라이언트는 Count만 동기화로 확인 (덱 안의 카드 순서는 비공개).
/// </summary>
public class GameDeck : NetworkBehaviour
{
    [Tooltip("게임에 존재하는 모든 카드 정의 (인스펙터에 8개 다 할당). " +
             "CardPoolType.SharedDeck인 카드만 덱에 들어가고, TurnGranted/Reward는 ID 조회용으로 등록됨.")]
    [SerializeField] private List<CardDefinition> allCards = new();

    // ── Networked 상태 (모두에게 보이는 카운트) ─────────────────

    [Networked, OnChangedRender(nameof(OnDeckChanged))]
    public int DrawPileCount { get; set; }

    [Networked, OnChangedRender(nameof(OnDeckChanged))]
    public int DiscardPileCount { get; set; }

    // ── 이벤트 ───────────────────────────────────────────────────

    public event Action DeckChanged;

    // ── Host 전용 내부 상태 (네트워크 X — 카드 순서 비공개) ────────

    private readonly List<string> _drawPile    = new();
    private readonly List<string> _discardPile = new();

    // ── 생명주기 ────────────────────────────────────────────────

    public override void Spawned()
    {
        Debug.Log($"[GameDeck] Spawned (HasStateAuthority={HasStateAuthority})");

        // 모든 클라이언트가 CardLibrary 등록 (스크립터블 자산은 동일하므로 안전)
        CardLibrary.RegisterAll(allCards);

        // GameController에게 알림 (LobbyState/GameSession 패턴 동일)
        var ctrl = FindAnyObjectByType<GameController>();
        if (ctrl != null) ctrl.RegisterDeck(this);

        if (HasStateAuthority)
        {
            InitializeDeck();
        }

        DeckChanged?.Invoke();
    }

    // ── Host 전용 ────────────────────────────────────────────────

    private void InitializeDeck()
    {
        if (!HasStateAuthority) return;

        _drawPile.Clear();
        _discardPile.Clear();

        foreach (var card in allCards)
        {
            if (card == null) continue;
            if (card.CardPoolType != CardPoolType.SharedDeck) continue;
            _drawPile.Add(card.CardId);
        }
        Shuffle(_drawPile);

        DrawPileCount    = _drawPile.Count;
        DiscardPileCount = _discardPile.Count;

        Debug.Log($"[GameDeck] 초기화 완료 — Draw {DrawPileCount} / Discard {DiscardPileCount}");
    }

    /// <summary>Host: 덱 top을 1장 뽑아 ID 반환. 덱 비면 discard 셔플 후 재시도.</summary>
    public string DrawTop()
    {
        if (!HasStateAuthority) return null;

        if (_drawPile.Count == 0)
            ReshuffleDiscardIntoDraw();

        if (_drawPile.Count == 0) return null;

        string cardId = _drawPile[0];
        _drawPile.RemoveAt(0);

        DrawPileCount = _drawPile.Count;
        return cardId;
    }

    /// <summary>Host: 사용된 카드를 discard로 보냄.</summary>
    public void AddToDiscard(string cardId)
    {
        if (!HasStateAuthority || string.IsNullOrEmpty(cardId)) return;

        var def = CardLibrary.GetById(cardId);
        if (def == null) return;
        if (def.CardPoolType != CardPoolType.SharedDeck) return;

        _discardPile.Add(cardId);
        DiscardPileCount = _discardPile.Count;
    }

    private void ReshuffleDiscardIntoDraw()
    {
        if (!HasStateAuthority) return;
        if (_discardPile.Count == 0) return;

        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);

        DrawPileCount    = _drawPile.Count;
        DiscardPileCount = _discardPile.Count;
        Debug.Log($"[GameDeck] discard 재셔플 → Draw {DrawPileCount}");
    }

    private static void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ── 변경 감지 ────────────────────────────────────────────────

    private void OnDeckChanged()
    {
        DeckChanged?.Invoke();
    }
}
