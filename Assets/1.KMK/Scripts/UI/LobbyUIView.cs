using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 UI 뷰. LobbyController/LobbyState를 구독해 화면을 그립니다.
/// 입력은 LobbyController에 직접 호출, 출력은 SlotsChanged 이벤트 구독.
/// </summary>
public class LobbyUIView : MonoBehaviour
{
    [SerializeField] private LobbyController controller;

    [Header("패널")]
    [SerializeField] private GameObject initialPanel;
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private GameObject lobbyRoomPanel;

    [Header("InitialPanel")]
    [SerializeField] private Button          showCreateRoomButton;
    [SerializeField] private Button          joinButton;
    [SerializeField] private TextMeshProUGUI initialStatusText;

    [Header("CreateRoomPanel")]
    [SerializeField] private TMP_InputField  roomNameInput;
    [SerializeField] private TMP_InputField  hostNicknameInput;
    [SerializeField] private Button          createRoomButton;
    [SerializeField] private Button          backButton;

    [Header("LobbyRoomPanel")]
    [SerializeField] private PlayerSlotView[] slotViews;     // 인스펙터에서 4개 연결
    [SerializeField] private Button           startButton;
    [SerializeField] private Button           readyButton;
    [SerializeField] private TextMeshProUGUI  readyButtonText;
    [SerializeField] private Button           exitButton;
    [SerializeField] private TextMeshProUGUI  playerCountText;

    private LobbyState _state;

    // ── 생명주기 ────────────────────────────────────────────────────

    private void Start()
    {
        showCreateRoomButton.onClick.AddListener(OnClickShowCreateRoom);
        joinButton          .onClick.AddListener(OnClickJoin);
        createRoomButton    .onClick.AddListener(OnClickCreateRoom);
        backButton          .onClick.AddListener(OnClickBack);
        startButton         .onClick.AddListener(OnClickStart);
        readyButton         .onClick.AddListener(OnClickReady);
        exitButton          .onClick.AddListener(OnClickExit);

        controller.LobbyStateReady    += OnLobbyStateReady;
        controller.LobbyDisconnected  += OnDisconnected;

        ShowInitialPanel();
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.LobbyStateReady   -= OnLobbyStateReady;
            controller.LobbyDisconnected -= OnDisconnected;
        }
        UnbindState();
    }

    // ── 상태 구독 ──────────────────────────────────────────────────

    private void OnLobbyStateReady(LobbyState state)
    {
        UnbindState();
        _state = state;
        _state.SlotsChanged += RefreshSlots;
        RefreshSlots();
    }

    private void UnbindState()
    {
        if (_state == null) return;
        _state.SlotsChanged -= RefreshSlots;
        _state = null;
    }

    // ── UI 갱신 ────────────────────────────────────────────────────

    private void RefreshSlots()
    {
        if (_state == null) return;

        int occupied = 0;
        for (int i = 0; i < slotViews.Length && i < LobbyState.MaxSlots; i++)
        {
            var s = _state.Slots[i];
            if (s.Occupied)
            {
                slotViews[i].SetOccupied(s.Name.ToString(), s.Ready, isHost: i == 0);
                occupied++;
            }
            else
            {
                slotViews[i].SetEmpty();
            }
        }

        if (playerCountText != null)
            playerCountText.text = $"{occupied} / {LobbyState.MaxSlots}";

        int mySlot = controller.GetMySlot();
        if (mySlot > 0 && readyButtonText != null)
        {
            bool myReady = _state.Slots[mySlot].Ready;
            readyButtonText.text = myReady ? "준비 취소" : "준비";
        }

        if (controller.IsHost && startButton != null)
            startButton.interactable = _state.CanStart();
    }

    // ── 버튼 핸들러 ────────────────────────────────────────────────

    private void OnClickShowCreateRoom()
    {
        initialPanel   .SetActive(false);
        createRoomPanel.SetActive(true);
    }

    private void OnClickBack()
    {
        createRoomPanel.SetActive(false);
        initialPanel   .SetActive(true);
    }

    private async void OnClickCreateRoom()
    {
        string room = roomNameInput != null ? roomNameInput.text.Trim() : "";

        SetInitialStatus("방 생성 중...");
        createRoomButton.interactable = false;

        var result = await controller.Host(room);
        if (result.Ok)
        {
            ShowLobbyRoomPanel();
            if (hostNicknameInput != null && !string.IsNullOrEmpty(hostNicknameInput.text))
                controller.SetMyName(hostNicknameInput.text.Trim());
        }
        else
        {
            SetInitialStatus($"방 생성 실패: {result.ShutdownReason}");
            createRoomButton.interactable = true;
        }
    }

    private async void OnClickJoin()
    {
        SetInitialStatus("참가 중...");
        joinButton.interactable = false;

        var result = await controller.Join("");
        if (result.Ok)
            ShowLobbyRoomPanel();
        else
        {
            SetInitialStatus($"참가 실패: {result.ShutdownReason}");
            joinButton.interactable = true;
        }
    }

    private void OnClickReady() => controller.ToggleMyReady();
    private void OnClickStart() => controller.StartGame();
    private void OnClickExit()  => ShowInitialPanel(); // TODO: bootstrap.Shutdown()

    // ── 패널 전환 ──────────────────────────────────────────────────

    private void ShowInitialPanel()
    {
        initialPanel   .SetActive(true);
        createRoomPanel.SetActive(false);
        lobbyRoomPanel .SetActive(false);

        joinButton      .interactable = true;
        createRoomButton.interactable = true;
        SetInitialStatus("");
        ResetAllSlots();
    }

    private void ShowLobbyRoomPanel()
    {
        initialPanel   .SetActive(false);
        createRoomPanel.SetActive(false);
        lobbyRoomPanel .SetActive(true);

        bool isHost = controller.IsHost;
        if (startButton != null) startButton.gameObject.SetActive(isHost);
        if (readyButton != null) readyButton.gameObject.SetActive(!isHost);
        if (startButton != null) startButton.interactable = false;
    }

    private void OnDisconnected()
    {
        UnbindState();
        ShowInitialPanel();
        SetInitialStatus("연결이 끊어졌습니다.");
    }

    // ── 유틸 ───────────────────────────────────────────────────────

    private void ResetAllSlots()
    {
        if (slotViews == null) return;
        foreach (var slot in slotViews) slot?.SetEmpty();
    }

    private void SetInitialStatus(string msg)
    {
        if (initialStatusText != null) initialStatusText.text = msg;
    }
}
