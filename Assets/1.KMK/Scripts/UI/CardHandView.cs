using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardHandView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameController controller;
    [SerializeField] private Transform handRoot;
    [SerializeField] private CardItemView cardItemPrefab;

    [Header("Texts")]
    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private TMP_Text dragGuideText;
    [SerializeField] private TMP_Text detailText;

    [Header("Config")]
    [SerializeField] private float useThresholdY = 100f;

    [SerializeField] private Transform hoverRoot;

    public Transform HoverRoot => hoverRoot != null ? hoverRoot : handRoot;

    private readonly List<CardItemView> _items = new();

    private NetworkPlayer _localPlayer;
    private GameSession _session;
    private GameDeck _deck;

    public float UseThresholdY => useThresholdY;

    private void Start()
    {
        if (controller == null)
            controller = FindAnyObjectByType<GameController>();

        if (controller != null)
        {
            controller.SessionReady += OnSessionReady;
            controller.DeckReady += OnDeckReady;

            if (controller.Session != null)
                OnSessionReady(controller.Session);

            if (controller.Deck != null)
                OnDeckReady(controller.Deck);
        }

        NetworkPlayer.OnSpawnedStatic += OnPlayerSpawned;
        NetworkPlayer.OnDespawnedStatic += OnPlayerDespawned;

        foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            OnPlayerSpawned(player);
        }

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.SessionReady -= OnSessionReady;
            controller.DeckReady -= OnDeckReady;
        }

        if (_session != null)
            _session.TurnChanged -= RefreshRuntimeOnly;

        if (_deck != null)
            _deck.DeckChanged -= RefreshRuntimeOnly;

        UnbindLocalPlayer();

        NetworkPlayer.OnSpawnedStatic -= OnPlayerSpawned;
        NetworkPlayer.OnDespawnedStatic -= OnPlayerDespawned;
    }

    private void Update()
    {
        RefreshRuntimeOnly();
    }

    private void OnSessionReady(GameSession session)
    {
        if (_session != null)
            _session.TurnChanged -= RefreshRuntimeOnly;

        _session = session;

        if (_session != null)
            _session.TurnChanged += RefreshRuntimeOnly;

        RefreshAll();
    }

    private void OnDeckReady(GameDeck deck)
    {
        if (_deck != null)
            _deck.DeckChanged -= RefreshRuntimeOnly;

        _deck = deck;

        if (_deck != null)
            _deck.DeckChanged += RefreshRuntimeOnly;

        RefreshAll();
    }

    private void OnPlayerSpawned(NetworkPlayer player)
    {
        if (player == null || !player.HasInputAuthority)
            return;

        if (_localPlayer == player)
            return;

        UnbindLocalPlayer();

        _localPlayer = player;
        _localPlayer.HandChanged += RefreshAll;
        _localPlayer.BranchStateChanged += RefreshRuntimeOnly;
        _localPlayer.PositionChanged += RefreshRuntimeOnly;

        RefreshAll();
    }

    private void OnPlayerDespawned(NetworkPlayer player)
    {
        if (player == null)
            return;

        if (_localPlayer == player)
        {
            UnbindLocalPlayer();
            RefreshAll();
        }
    }

    private void UnbindLocalPlayer()
    {
        if (_localPlayer == null)
            return;

        _localPlayer.HandChanged -= RefreshAll;
        _localPlayer.BranchStateChanged -= RefreshRuntimeOnly;
        _localPlayer.PositionChanged -= RefreshRuntimeOnly;
        _localPlayer = null;
    }

    private void RefreshAll()
    {
        RebuildHand();
        RefreshDeckText();
        RefreshGuideText();

        if (detailText != null && string.IsNullOrWhiteSpace(detailText.text))
            detailText.text = "카드를 선택하세요.";
    }

    public void RefreshRuntimeOnly()
    {
        RefreshDeckText();
        RefreshGuideText();

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null)
                _items[i].RefreshState();
        }
    }

    private void RebuildHand()
    {
        if (handRoot == null || cardItemPrefab == null)
            return;

        for (int i = handRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(handRoot.GetChild(i).gameObject);
        }

        _items.Clear();

        if (_localPlayer == null)
            return;

        for (int i = 0; i < _localPlayer.HandCards.Length; i++)
        {
            string cardId = _localPlayer.HandCards[i].ToString();
            if (string.IsNullOrEmpty(cardId))
                continue;

            CardDefinition def = CardLibrary.GetById(cardId);

            CardItemView item = Instantiate(cardItemPrefab, handRoot);
            item.gameObject.SetActive(true);
            item.Bind(this, i, cardId, def);
            _items.Add(item);
        }
    }

    public bool CanUseCard(int handIndex, CardDefinition def)
    {
        if (_localPlayer == null || def == null)
            return false;

        if (_session == null)
            return false;

        if (_session.CurrentTurnSlot != _localPlayer.SlotIndex)
            return false;

        if (_localPlayer.IsAwaitingBranch)
            return false;

        if (_localPlayer.PendingSteps > 0)
            return false;

        if (handIndex == NetworkPlayer.FixedHandIndex && _localPlayer.BasicWalkUsedThisTurn)
            return false;

        return true;
    }

    public bool TryUseCard(int handIndex)
    {
        if (_localPlayer == null)
            return false;

        if (handIndex < 0 || handIndex >= _localPlayer.HandCards.Length)
            return false;

        string cardId = _localPlayer.HandCards[handIndex].ToString();
        if (string.IsNullOrEmpty(cardId))
            return false;

        CardDefinition def = CardLibrary.GetById(cardId);
        if (!CanUseCard(handIndex, def))
            return false;

        _localPlayer.RPC_RequestUseCardAt(handIndex);
        return true;
    }

    public void ShowDetail(CardDefinition def, int handIndex)
    {
        if (detailText == null)
            return;

        if (def == null)
        {
            detailText.text = handIndex == NetworkPlayer.FixedHandIndex
                ? "0번 슬롯 (고정 슬롯)"
                : $"슬롯 {handIndex} (빈 슬롯)";
            return;
        }

        string slotLabel = handIndex == NetworkPlayer.FixedHandIndex ? "고정 슬롯" : $"슬롯 {handIndex}";
        detailText.text =
            $"{slotLabel}\n" +
            $"이름: {def.CardName}\n" +
            $"코스트: {def.Cost}\n" +
            $"타입: {def.CardType}\n" +
            $"공급: {def.CardPoolType}\n" +
            $"효과: {def.EffectType}\n" +
            $"설명: {def.Description}";
    }

    private void RefreshDeckText()
    {
        if (deckCountText == null)
            return;

        if (_deck == null)
        {
            deckCountText.text = "Deck: - / Discard: -";
            return;
        }

        deckCountText.text = $"Deck: {_deck.DrawPileCount} / Discard: {_deck.DiscardPileCount}";
    }

    private void RefreshGuideText()
    {
        if (dragGuideText == null)
            return;

        if (_localPlayer == null)
        {
            dragGuideText.text = "로컬 플레이어 대기 중...";
            return;
        }

        if (_session == null)
        {
            dragGuideText.text = "세션 대기 중...";
            return;
        }

        if (_session.CurrentTurnSlot != _localPlayer.SlotIndex)
        {
            dragGuideText.text = "상대 턴입니다.";
            return;
        }

        if (_localPlayer.IsAwaitingBranch)
        {
            dragGuideText.text = "갈림길 선택 중에는 카드 사용 불가";
            return;
        }

        if (_localPlayer.PendingSteps > 0)
        {
            dragGuideText.text = "이동 중...";
            return;
        }

        dragGuideText.text = $"카드를 위로 {useThresholdY:0}px 이상 드래그하면 사용";
    }
}