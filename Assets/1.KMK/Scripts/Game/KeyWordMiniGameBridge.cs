using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// CMK KeyWord 미니게임의 화면/입력과 KMK 네트워크 상태를 연결합니다.
/// KeyWord 원본에는 Fusion 의존성을 추가하지 않습니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class KeyWordMiniGameBridge : MonoBehaviour
{
    private const float LaneSpacing = 1.2f;

    private static readonly KeyCode[] Keys =
    {
        KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D,
    };

    private static readonly Color[] SlotColors =
    {
        Color.red, Color.blue, Color.green, Color.yellow,
    };

    private readonly List<NetworkPlayer> _players = new();
    private readonly Dictionary<int, Transform> _visuals = new();
    private readonly List<GameObject> _sequenceItems = new();

    private KeyWord _view;
    private GameSession _session;
    private NetworkPlayer _activePlayer;
    private Transform _followCamera;
    private Vector3 _cameraLocalPosition;
    private Quaternion _cameraLocalRotation;
    private Vector3 _cameraLocalScale;
    private Vector3 _startPosition;
    private int _shownSlot = -1;
    private int _shownProgress = -1;
    private GamePhase _shownPhase = (GamePhase)byte.MaxValue;

    private void Awake()
    {
        _view = GetComponent<KeyWord>();
        if (_view == null)
        {
            enabled = false;
            return;
        }

        // 통합 실행에서는 기존 로컬 랜덤 진행을 멈추고 이 브리지가 화면을 제어합니다.
        _view.enabled = false;
    }

    private void Start()
    {
        _session = FindAnyObjectByType<GameSession>();
        if (_session == null)
        {
            // 미니게임 씬을 단독 Play한 경우 기존 CMK 로컬 테스트를 유지합니다.
            _view.enabled = true;
            enabled = false;
            return;
        }

        RefreshPlayers();
        BuildPlayerVisuals();

        if (_session.HasStateAuthority)
            _session.BeginMiniGameRound();
    }

    private void Update()
    {
        if (_session == null) return;

        SelectActiveOwnedPlayer();
        FollowActivePlayer();

        if (_session.Phase == GamePhase.MiniGame)
            HandleInput();

        RefreshSequenceUI();
        RefreshPlayerVisuals();
    }

    private void RefreshPlayers()
    {
        _players.Clear();
        _players.AddRange(FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None));
        _players.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
    }

    private void BuildPlayerVisuals()
    {
        if (_view.player == null || _players.Count == 0) return;

        _startPosition = _view.player.position;
        Camera mainCamera = _view.player.GetComponentInChildren<Camera>();
        if (mainCamera != null)
        {
            _followCamera = mainCamera.transform;
            _cameraLocalPosition = _followCamera.localPosition;
            _cameraLocalRotation = _followCamera.localRotation;
            _cameraLocalScale = _followCamera.localScale;
        }

        for (int i = 0; i < _players.Count; i++)
        {
            NetworkPlayer networkPlayer = _players[i];
            Transform visual = i == 0
                ? _view.player
                : Instantiate(_view.player.gameObject, _view.player.parent).transform;

            if (i > 0)
            {
                foreach (Camera camera in visual.GetComponentsInChildren<Camera>(true))
                    camera.enabled = false;
                foreach (AudioListener listener in visual.GetComponentsInChildren<AudioListener>(true))
                    listener.enabled = false;
            }

            visual.name = $"KeyWordPlayer_Slot{networkPlayer.SlotIndex}";
            var collider = visual.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            var renderer = visual.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = SlotColors[networkPlayer.SlotIndex % SlotColors.Length];

            _visuals[networkPlayer.SlotIndex] = visual;
        }
    }

    private void FollowActivePlayer()
    {
        if (_followCamera == null || _activePlayer == null) return;
        if (!_visuals.TryGetValue(_activePlayer.SlotIndex, out Transform visual)) return;
        if (_followCamera.parent == visual) return;

        _followCamera.SetParent(visual, false);
        _followCamera.localPosition = _cameraLocalPosition;
        _followCamera.localRotation = _cameraLocalRotation;
        _followCamera.localScale = _cameraLocalScale;
    }

    private void SelectActiveOwnedPlayer()
    {
        if (_activePlayer != null &&
            _activePlayer.HasInputAuthority &&
            _activePlayer.MiniGameRank <= 0)
            return;

        _activePlayer = null;
        foreach (var player in _players)
        {
            if (player != null && player.HasInputAuthority && player.MiniGameRank <= 0)
            {
                _activePlayer = player;
                break;
            }
        }
    }

    private void HandleInput()
    {
        if (_activePlayer == null) return;

        for (int keyIndex = 0; keyIndex < Keys.Length; keyIndex++)
        {
            if (!Input.GetKeyDown(Keys[keyIndex])) continue;
            _activePlayer.RPC_SubmitMiniGameKey(keyIndex);
            return;
        }
    }

    private void RefreshSequenceUI()
    {
        int slot = _activePlayer != null ? _activePlayer.SlotIndex : -1;
        int progress = _activePlayer != null ? _activePlayer.MiniGameProgress : -1;
        GamePhase phase = _session.Phase;
        if (_shownSlot == slot && _shownProgress == progress && _shownPhase == phase) return;

        _shownSlot = slot;
        _shownProgress = progress;
        _shownPhase = phase;
        ClearSequenceUI();

        if (_activePlayer == null || _session.Phase != GamePhase.MiniGame) return;
        if (_view.KeyWordPrefabs == null || _view.uiDistance == null) return;

        for (int offset = 0; offset < _view.StartCount; offset++)
        {
            int sequenceIndex = progress + offset;
            if (sequenceIndex >= GameSession.MiniGameTargetCount) break;

            int keyIndex = _session.GetExpectedMiniGameKey(slot, sequenceIndex);
            GameObject item = Instantiate(_view.KeyWordPrefabs, _view.uiDistance, false);
            item.GetComponent<TMP_Text>().text = Keys[keyIndex].ToString();
            item.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(sequenceIndex * _view.Spacing, 0f);
            _sequenceItems.Add(item);
        }
    }

    private void ClearSequenceUI()
    {
        foreach (var item in _sequenceItems)
            if (item != null) Destroy(item);
        _sequenceItems.Clear();
    }

    private void RefreshPlayerVisuals()
    {
        float center = (_players.Count - 1) * 0.5f;
        for (int i = 0; i < _players.Count; i++)
        {
            NetworkPlayer networkPlayer = _players[i];
            if (!_visuals.TryGetValue(networkPlayer.SlotIndex, out Transform visual)) continue;

            Vector3 laneOffset = Vector3.forward * ((i - center) * LaneSpacing);
            Vector3 progressOffset = Vector3.right * (networkPlayer.MiniGameProgress * _view.moveDistance);
            visual.position = _startPosition + laneOffset + progressOffset;
        }
    }

    private void OnGUI()
    {
        if (_session == null) return;

        GUILayout.BeginArea(new Rect(20f, 20f, 430f, 260f), GUI.skin.box);
        GUILayout.Label("KEYWORD NETWORK RACE");

        if (_session.Phase == GamePhase.MiniGame)
            GUILayout.Label($"남은 시간: {_session.MiniGameSecondsRemaining:0.0}초");
        else if (_session.Phase == GamePhase.ShowingMiniGameResult)
            GUILayout.Label("미니게임 종료 - 보드로 돌아갑니다.");
        else
            GUILayout.Label("미니게임 준비 중...");

        if (_activePlayer != null && _session.Phase == GamePhase.MiniGame)
        {
            string devText = CountOwnedPlayers() > 1 ? " (단독 테스트 순차 조작)" : string.Empty;
            GUILayout.Label($"조작: Slot {_activePlayer.SlotIndex}{devText}");
            GUILayout.Label("화면에 표시된 WASD를 순서대로 누르세요.");
        }

        GUILayout.Space(8f);
        foreach (var player in _players)
        {
            string rank = player.MiniGameRank > 0 ? $"{player.MiniGameRank}등" : "진행 중";
            GUILayout.Label(
                $"Slot {player.SlotIndex}  {player.MiniGameProgress}/{GameSession.MiniGameTargetCount}  {rank}");
        }

        GUILayout.EndArea();
    }

    private int CountOwnedPlayers()
    {
        int count = 0;
        foreach (var player in _players)
            if (player != null && player.HasInputAuthority) count++;
        return count;
    }
}
