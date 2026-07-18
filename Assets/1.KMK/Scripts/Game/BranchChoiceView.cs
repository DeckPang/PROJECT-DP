using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 내가 InputAuthority를 가진 NetworkPlayer가 분기 대기 상태에 들어가면 UI를 표시.
/// 버튼 클릭 시 RPC_ChooseBranch로 호스트에 선택을 전달.
/// </summary>
public class BranchChoiceView : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private GameObject panel;          // 패널 GameObject (활성/비활성 토글)
    [SerializeField] private Transform  buttonContainer; // 버튼 부모
    [SerializeField] private Button     buttonPrefab;   // 분기 버튼 프리팹 (TextMeshProUGUI 자식 포함)
    [SerializeField] private TextMeshProUGUI titleText; // 선택 안내 텍스트 (선택)

    // 내가 가진 NetworkPlayer들 추적 (dev 모드에선 여러 개일 수 있음)
    private readonly List<NetworkPlayer> _ownedPlayers = new();
    private readonly List<Button>        _activeButtons = new();
    private NetworkPlayer _activePlayer;

    //스크립트가 캐릭터 생성보다 늦게 활성화되서 플레이어 못잡는거같아서 추가함. -여영부
    private void Start()
    {
        foreach (var player in FindObjectsByType<NetworkPlayer>(
                     FindObjectsSortMode.None))
        {
            HandlePlayerSpawned(player);
        }

        OnAnyBranchStateChanged();
    }

    // ── 생명주기 ────────────────────────────────────────────────────

    private void OnEnable()
    {
        NetworkPlayer.OnSpawnedStatic   += HandlePlayerSpawned;
        NetworkPlayer.OnDespawnedStatic += HandlePlayerDespawned;
        HidePanel();
    }

    private void OnDisable()
    {
        NetworkPlayer.OnSpawnedStatic   -= HandlePlayerSpawned;
        NetworkPlayer.OnDespawnedStatic -= HandlePlayerDespawned;

        foreach (var p in _ownedPlayers)
            if (p != null) p.BranchStateChanged -= OnAnyBranchStateChanged;

        _ownedPlayers.Clear();
        _activePlayer = null;
        ClearButtons();
    }

    // ── NetworkPlayer 등장/소멸 ─────────────────────────────────────

    private void HandlePlayerSpawned(NetworkPlayer p)
    {
        if (p == null || !p.HasInputAuthority) return;  // 내가 컨트롤하는 폰만
        if (_ownedPlayers.Contains(p)) return;

        _ownedPlayers.Add(p);
        p.BranchStateChanged += OnAnyBranchStateChanged;
        OnAnyBranchStateChanged();
    }

    private void HandlePlayerDespawned(NetworkPlayer p)
    {
        if (p == null) return;
        if (_ownedPlayers.Remove(p))
            p.BranchStateChanged -= OnAnyBranchStateChanged;
        if (_activePlayer == p) _activePlayer = null;
        OnAnyBranchStateChanged();
    }

    // ── 분기 상태 변경 시 ───────────────────────────────────────────

    private void OnAnyBranchStateChanged()
    {
        // 분기 대기 중인 내 NetworkPlayer를 찾음
        _activePlayer = null;
        foreach (var p in _ownedPlayers)
        {
            if (p != null && p.IsAwaitingBranch)
            {
                _activePlayer = p;
                break;
            }
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_activePlayer == null) { HidePanel(); return; }

        var boardMgr = FindAnyObjectByType<BoardManager>();
        if (boardMgr == null) { HidePanel(); return; }

        var node = boardMgr.GetNodeById(_activePlayer.CurrentNodeId);
        if (node == null || node.NextNodes == null || node.NextNodes.Count <= 1)
        {
            HidePanel();
            return;
        }

        ShowPanel(node);
    }

    // ── UI 표시/숨김 ────────────────────────────────────────────────

    private void ShowPanel(BoardNode node)
    {
        ClearButtons();
        if (panel != null) panel.SetActive(true);

        if (titleText != null)
            titleText.text = $"Slot {_activePlayer.SlotIndex} — 길을 선택하세요";

        for (int i = 0; i < node.NextNodes.Count; i++)
        {
            int idx = i;
            var nextNode = node.NextNodes[i];
            if (nextNode == null) continue;

            var btn = Instantiate(buttonPrefab, buttonContainer);
            btn.gameObject.SetActive(true);

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                string routeName = i switch
                {
                    0 => "안쪽길",
                    1 => "바깥길",
                    _ => $"길 {i + 1}"
                };

                label.text = $"{routeName}\nNode {nextNode.NodeId}";
            }

            btn.onClick.AddListener(() => OnChooseBranch(idx));
            _activeButtons.Add(btn);
        }
    }

    private void HidePanel()
    {
        if (panel != null) panel.SetActive(false);
        ClearButtons();
    }

    private void ClearButtons()
    {
        foreach (var b in _activeButtons)
            if (b != null) Destroy(b.gameObject);
        _activeButtons.Clear();
    }

    // ── 버튼 핸들러 ─────────────────────────────────────────────────

    private void OnChooseBranch(int idx)
    {
        if (_activePlayer == null) return;
        _activePlayer.RPC_ChooseBranch(idx);
    }
}
