using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 씬 네트워크 상태 확인용 디버그 UI.
///  - GameSession 턴 정보 표시
///  - 모든 NetworkPlayer의 슬롯/이름/위치 표시
///  - "다음 턴" / "현재 차례 +1 이동" 버튼 (디버그)
/// </summary>
public class GameDebugUI : MonoBehaviour
{
    [SerializeField] private GameController controller;

    [Header("턴 정보")]
    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private Button          endTurnButton;

    [Header("플레이어 정보 (Phase 1.4)")]
    [SerializeField] private TextMeshProUGUI playersListText;
    [SerializeField] private Button          moveOneButton;

    [Header("덱 / 손패 (Phase 1.6)")]
    [SerializeField] private TextMeshProUGUI deckCountText;
    [SerializeField] private Button          drawCardButton;

    private GameSession _session;
    private GameDeck    _deck;
    private readonly List<NetworkPlayer> _players = new();

    // ── 생명주기 ────────────────────────────────────────────────────

    private void Start()
    {
        if (controller == null)
        {
            Debug.LogError("[GameDebugUI] controller가 인스펙터에 연결되지 않았습니다.");
            return;
        }

        controller.SessionReady += OnSessionReady;
        controller.DeckReady    += OnDeckReady;
        if (endTurnButton  != null) endTurnButton .onClick.AddListener(OnClickEndTurn);
        if (moveOneButton  != null) moveOneButton .onClick.AddListener(OnClickMoveOne);
        if (drawCardButton != null) drawCardButton.onClick.AddListener(OnClickDraw);

        // 이미 도착해 있을 수도 있음
        if (controller.Session != null) OnSessionReady(controller.Session);
        if (controller.Deck    != null) OnDeckReady(controller.Deck);

        // NetworkPlayer 등장/소멸 구독
        NetworkPlayer.OnSpawnedStatic   += OnPlayerSpawned;
        NetworkPlayer.OnDespawnedStatic += OnPlayerDespawned;

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.SessionReady -= OnSessionReady;
            controller.DeckReady    -= OnDeckReady;
        }
        UnbindSession();
        UnbindDeck();

        NetworkPlayer.OnSpawnedStatic   -= OnPlayerSpawned;
        NetworkPlayer.OnDespawnedStatic -= OnPlayerDespawned;
        foreach (var p in _players)
        {
            if (p == null) continue;
            p.PositionChanged -= UpdateUI;
            p.HandChanged     -= UpdateUI;
        }
    }

    // ── 세션 바인딩 ────────────────────────────────────────────────

    private void OnSessionReady(GameSession session)
    {
        UnbindSession();
        _session = session;
        _session.TurnChanged += UpdateUI;
        UpdateUI();
    }

    private void UnbindSession()
    {
        if (_session == null) return;
        _session.TurnChanged -= UpdateUI;
        _session = null;
    }

    private void OnDeckReady(GameDeck deck)
    {
        UnbindDeck();
        _deck = deck;
        _deck.DeckChanged += UpdateUI;
        UpdateUI();
    }

    private void UnbindDeck()
    {
        if (_deck == null) return;
        _deck.DeckChanged -= UpdateUI;
        _deck = null;
    }

    // ── NetworkPlayer 구독 ──────────────────────────────────────────

    private void OnPlayerSpawned(NetworkPlayer player)
    {
        if (player == null || _players.Contains(player)) return;
        _players.Add(player);
        player.PositionChanged += UpdateUI;
        player.HandChanged     += UpdateUI;
        UpdateUI();
    }

    private void OnPlayerDespawned(NetworkPlayer player)
    {
        if (player == null) return;
        player.PositionChanged -= UpdateUI;
        player.HandChanged     -= UpdateUI;
        _players.Remove(player);
        UpdateUI();
    }

    // ── UI 갱신 ────────────────────────────────────────────────────

    private void UpdateUI()
    {
        bool sessionValid = IsSessionValid();

        // 턴 정보
        if (currentTurnText != null)
        {
            currentTurnText.text = sessionValid
                ? $"Turn {_session.TurnNumber} — Slot {_session.CurrentTurnSlot}"
                : "GameSession 대기 중...";
        }

        // 플레이어 리스트 (+ 손패)
        if (playersListText != null)
        {
            var sb = new StringBuilder();
            for (int slot = 0; slot < 4; slot++)
            {
                var p = FindBySlot(slot);
                if (p == null)
                {
                    sb.AppendLine($"Slot {slot}: (비어있음)");
                }
                else
                {
                    string marker = (sessionValid && _session.CurrentTurnSlot == slot) ? " ◀ 차례" : "";
                    sb.AppendLine($"Slot {slot}: {p.PlayerName} | Node {p.CurrentNodeId}{marker}");
                    sb.AppendLine($"   ✋ {FormatHand(p)}");
                }
            }
            playersListText.text = sb.ToString();
        }

        // 덱 카운트
        if (deckCountText != null)
        {
            deckCountText.text = (_deck != null && _deck.Object != null && _deck.Object.IsValid)
                ? $"덱: {_deck.DrawPileCount}  /  버린 카드: {_deck.DiscardPileCount}"
                : "Deck 대기 중...";
        }

        if (endTurnButton  != null) endTurnButton .interactable = sessionValid;
        if (moveOneButton  != null) moveOneButton .interactable = CanLocalPlayerMove();
        if (drawCardButton != null) drawCardButton.interactable = CanLocalPlayerDraw();
    }

    private static string FormatHand(NetworkPlayer p)
    {
        var items = new List<string>();
        for (int i = 0; i < p.HandCards.Length; i++)
        {
            var id = p.HandCards[i].ToString();
            if (string.IsNullOrEmpty(id)) continue;
            var def = CardLibrary.GetById(id);
            items.Add(def != null ? def.CardName : id);
        }
        return items.Count == 0 ? "(빈 손)" : string.Join(", ", items);
    }

    private bool CanLocalPlayerDraw()
    {
        if (!IsSessionValid() || _deck == null) return false;
        var current = FindBySlot(_session.CurrentTurnSlot);
        return current != null && current.HasInputAuthority;
    }

    private bool IsSessionValid()
    {
        return _session != null && _session.Object != null && _session.Object.IsValid;
    }

    private NetworkPlayer FindBySlot(int slot)
    {
        foreach (var p in _players)
            if (p != null && p.SlotIndex == slot) return p;
        return null;
    }

    /// <summary>현재 차례의 NetworkPlayer를 내가 InputAuthority로 가지고 있는가.</summary>
    private bool CanLocalPlayerMove()
    {
        if (!IsSessionValid()) return false;
        var current = FindBySlot(_session.CurrentTurnSlot);
        if (current == null) return false;
        return current.HasInputAuthority;
    }

    // ── 버튼 ───────────────────────────────────────────────────────

    private void OnClickEndTurn()
    {
        if (!IsSessionValid()) return;
        _session.RPC_RequestEndTurn();
    }

    private void OnClickMoveOne()
    {
        if (!IsSessionValid()) return;
        var current = FindBySlot(_session.CurrentTurnSlot);
        if (current == null || !current.HasInputAuthority) return;

        current.RPC_RequestMove(1);
    }

    private void OnClickDraw()
    {
        if (!IsSessionValid()) return;
        var current = FindBySlot(_session.CurrentTurnSlot);
        if (current == null || !current.HasInputAuthority) return;

        current.RPC_RequestDraw();
    }
}
