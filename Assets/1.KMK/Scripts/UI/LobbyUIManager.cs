using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;

    // ── 패널 ──────────────────────────────────────────────────────────────

    [Header("패널")]
    [SerializeField] private GameObject initialPanel;
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private GameObject lobbyRoomPanel;

    // ── InitialPanel ───────────────────────────────────────────────────────

    [Header("InitialPanel")]
    [SerializeField] private Button showCreateRoomPanelButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TextMeshProUGUI initialStatusText;

    // ── CreateRoomPanel ────────────────────────────────────────────────────

    [Header("CreateRoomPanel")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField hostNicknameInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button backButton;

    // ── LobbyRoomPanel ─────────────────────────────────────────────────────

    [Header("LobbyRoomPanel - 슬롯")]
    [SerializeField] private PlayerSlotUI[] playerSlots; // 인스펙터에서 4개 연결

    [Header("LobbyRoomPanel - 하단")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    [SerializeField] private Button exitButton;
    [SerializeField] private TextMeshProUGUI playerCountText;

    // ── 생명주기 ───────────────────────────────────────────────────────────

    private void Start()
    {
        showCreateRoomPanelButton.onClick.AddListener(OnClickShowCreateRoom);
        joinButton.onClick.AddListener(OnClickJoin);
        createRoomButton.onClick.AddListener(OnClickCreateRoom);
        backButton.onClick.AddListener(OnClickBack);
        startButton.onClick.AddListener(OnClickStart);
        readyButton.onClick.AddListener(OnClickReady);
        exitButton.onClick.AddListener(OnClickExit);

        // LobbyState 이벤트: 점유/Ready/StartPossible — 모든 클라이언트에서 동기화됨
        LobbyState.OnSlotOccupancyChanged += OnSlotOccupancyChanged;
        LobbyState.OnSlotReadyChanged     += OnReadyStateChanged;
        LobbyState.OnCanStartChanged      += OnCanStartChanged;

        // 인원수는 LobbyManager 이벤트로 수신
        lobbyManager.OnPlayerCountChanged += OnPlayerCountChanged;

        ShowInitialPanel();
    }

    private void OnDestroy()
    {
        LobbyState.OnSlotOccupancyChanged -= OnSlotOccupancyChanged;
        LobbyState.OnSlotReadyChanged     -= OnReadyStateChanged;
        LobbyState.OnCanStartChanged      -= OnCanStartChanged;
        lobbyManager.OnPlayerCountChanged -= OnPlayerCountChanged;
    }

    // ── 버튼 이벤트 ────────────────────────────────────────────────────────

    private void OnClickShowCreateRoom()
    {
        initialPanel.SetActive(false);
        createRoomPanel.SetActive(true);
    }

    private void OnClickBack()
    {
        createRoomPanel.SetActive(false);
        initialPanel.SetActive(true);
    }

    private async void OnClickCreateRoom()
    {
        string roomName = roomNameInput != null ? roomNameInput.text.Trim() : "";
        string nickname = hostNicknameInput != null ? hostNicknameInput.text.Trim() : "Host";
        if (string.IsNullOrEmpty(nickname)) nickname = "Host";

        SetInitialStatus("방 생성 중...");
        createRoomButton.interactable = false;

        var result = await lobbyManager.StartHost(roomName, nickname);

        if (result.Ok)
            ShowLobbyRoomPanel();
        else
        {
            SetInitialStatus($"방 생성 실패: {result.ShutdownReason}");
            createRoomButton.interactable = true;
        }
    }

    private async void OnClickJoin()
    {
        string nickname = "Player";

        SetInitialStatus("참가 중...");
        joinButton.interactable = false;

        var result = await lobbyManager.StartClient(nickname);

        if (result.Ok)
            ShowLobbyRoomPanel();
        else
        {
            SetInitialStatus($"참가 실패: {result.ShutdownReason}");
            joinButton.interactable = true;
        }
    }

    private void OnClickStart()  => lobbyManager.StartGame();
    private void OnClickReady()  => lobbyManager.ToggleReady();

    private void OnClickExit()
    {
        // TODO: Runner 종료 후 InitialPanel 복귀
        ShowInitialPanel();
    }

    // ── 이벤트 수신 ────────────────────────────────────────────────────────

    /// <summary>LobbyState.OccupiedMask 변경 시 호출 — 모든 클라이언트에서 동작</summary>
    private void OnSlotOccupancyChanged(int slot, bool occupied)
    {
        if (slot >= playerSlots.Length) return;

        if (occupied)
            playerSlots[slot].SetOccupied($"Player_{slot}", isHost: slot == 0);
        else
            playerSlots[slot].SetEmpty();

        Debug.Log($"[LobbyUIManager] 슬롯 {slot} 점유={occupied}");
    }

    private void OnPlayerCountChanged(int count)
    {
        if (playerCountText != null)
            playerCountText.text = $"{count} / 4";
    }

    private void OnReadyStateChanged(int slot, bool isReady)
    {
        if (slot < playerSlots.Length)
            playerSlots[slot].SetReady(isReady);

        // 내 슬롯이면 버튼 텍스트도 변경
        if (slot == lobbyManager.GetMySlot() && readyButtonText != null)
            readyButtonText.text = isReady ? "준비 취소" : "준비";

        Debug.Log($"[LobbyUIManager] 슬롯 {slot} Ready={isReady} (내 슬롯={lobbyManager.GetMySlot()})");
    }

    private void OnCanStartChanged(bool canStart)
    {
        if (lobbyManager.IsHost)
            startButton.interactable = canStart;
    }

    // ── 패널 전환 ──────────────────────────────────────────────────────────

    private void ShowInitialPanel()
    {
        initialPanel.SetActive(true);
        createRoomPanel.SetActive(false);
        lobbyRoomPanel.SetActive(false);

        joinButton.interactable = true;
        SetInitialStatus("");
        ResetAllSlots();
    }

    private void ShowLobbyRoomPanel()
    {
        initialPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        lobbyRoomPanel.SetActive(true);

        bool isHost = lobbyManager.IsHost;
        startButton.gameObject.SetActive(isHost);
        readyButton.gameObject.SetActive(!isHost);
        startButton.interactable = false;
    }

    // ── 유틸 ───────────────────────────────────────────────────────────────

    private void ResetAllSlots()
    {
        foreach (var slot in playerSlots)
            slot.SetEmpty();
    }

    private void SetInitialStatus(string msg)
    {
        if (initialStatusText != null)
            initialStatusText.text = msg;
    }
}
