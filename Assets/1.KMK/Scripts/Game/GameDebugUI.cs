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

        // 미니게임에서 복귀한 NetworkPlayer는 다시 Spawn되지 않으므로 직접 수집합니다.
        foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
            OnPlayerSpawned(player);

        UpdateUI();
    }

    private void Update()
    {
        // 턴 타이머 카운트다운은 이벤트만으론 갱신 안 되므로 매 프레임 갱신 (디버그 UI).
        if (IsSessionValid() && _session.WinnerSlot < 0)
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
            p.TrophiesChanged -= UpdateUI;
            p.CoinsChanged    -= UpdateUI;
        }
    }

    // ── 세션 바인딩 ────────────────────────────────────────────────

    private void OnSessionReady(GameSession session)
    {
        UnbindSession();
        _session = session;
        _session.TurnChanged      += UpdateUI;
        _session.GameEnded        += OnGameEnded;
        _session.TrophyNodeChanged += UpdateUI;
        UpdateUI();
    }

    private void UnbindSession()
    {
        if (_session == null) return;
        _session.TurnChanged      -= UpdateUI;
        _session.GameEnded        -= OnGameEnded;
        _session.TrophyNodeChanged -= UpdateUI;
        _session = null;
    }

    private void OnGameEnded(int winnerSlot) => UpdateUI();

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
        player.TrophiesChanged += UpdateUI;
        player.CoinsChanged    += UpdateUI;
        UpdateUI();
    }

    private void OnPlayerDespawned(NetworkPlayer player)
    {
        if (player == null) return;
        player.PositionChanged -= UpdateUI;
        player.HandChanged     -= UpdateUI;
        player.TrophiesChanged -= UpdateUI;
        player.CoinsChanged    -= UpdateUI;
        _players.Remove(player);
        UpdateUI();
    }

    // ── UI 갱신 ────────────────────────────────────────────────────

    private void UpdateUI()
    {
        bool sessionValid = IsSessionValid();
        bool gameOver     = sessionValid && _session.WinnerSlot >= 0;

        // 턴 정보
        if (currentTurnText != null)
        {
            if (!sessionValid)
                currentTurnText.text = "GameSession 대기 중...";
            else if (gameOver)
                currentTurnText.text = $"게임 종료! 🏆 승자 = Slot {_session.WinnerSlot}";
            else
                currentTurnText.text = $"라운드 {_session.RoundNumber} · Turn {_session.TurnNumber} — Slot {_session.CurrentTurnSlot} · ⏱{_session.TurnSecondsRemaining:0.0}s";
        }

        // 플레이어 리스트 (+ 손패)
        if (playersListText != null)
        {
            var sb = new StringBuilder();
            if (sessionValid)
                sb.AppendLine($"🏆 트로피 노드: Node {_session.TrophyNodeId} (가격 {GameSession.TrophyPrice}💰)");
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
                    sb.AppendLine($"Slot {slot}: {p.PlayerName} | Node {p.CurrentNodeId} | 🏆{p.Trophies} 💰{p.Coins}{marker}");
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

        if (endTurnButton  != null) endTurnButton .interactable = sessionValid && !gameOver;
        if (moveOneButton  != null) moveOneButton .interactable = !gameOver && CanLocalPlayerMove();
        if (drawCardButton != null) drawCardButton.interactable = !gameOver && CanLocalPlayerDraw();
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
