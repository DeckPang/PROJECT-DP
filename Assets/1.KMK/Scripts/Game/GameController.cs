using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// 게임 씬의 진입점. 살아있는 Runner를 찾고, Host면 GameSession을 Spawn합니다.
/// 로비의 LobbyController와 동일한 패턴.
/// </summary>
public class GameController : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkObject gameSessionPrefab;
    [SerializeField] private NetworkObject networkPlayerPrefab;
    [SerializeField] private NetworkObject gameDeckPrefab;

    private NetworkRunner _runner;
    private GameSession   _session;
    private GameDeck      _deck;

    /// <summary>GameSession이 발견/생성된 직후. UI는 여기서 구독을 시작.</summary>
    public event Action<GameSession> SessionReady;

    /// <summary>GameDeck이 발견/생성된 직후.</summary>
    public event Action<GameDeck> DeckReady;

    // ── 외부 API ─────────────────────────────────────────────────────

    public GameSession   Session => _session;
    public GameDeck      Deck    => _deck;
    public NetworkRunner Runner  => _runner;
    public bool          IsHost  => _runner != null && _runner.IsServer;

    // ── 생명주기 ─────────────────────────────────────────────────────

    private async void Start()
    {
        // Lobby를 통해 진입한 경우 Runner가 이미 있음 → 바로 초기화
        // Game 씬에서 직접 Play 한 경우 → DevAutoStart가 나중에 OnRunnerReady() 호출
        if (FindAnyObjectByType<NetworkRunner>() != null)
            await InitializeAsync();
    }

    /// <summary>외부에서 Runner 준비 완료를 알릴 때 호출 (DevAutoStart 등).</summary>
    public async void OnRunnerReady()
    {
        if (_runner != null) return;   // 이미 초기화됨
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _runner = FindAnyObjectByType<NetworkRunner>();
        if (_runner == null)
        {
            Debug.LogError("[GameController] NetworkRunner를 찾을 수 없습니다.");
            return;
        }

        _runner.AddCallbacks(this);

        await TryFindOrSpawnSession();
    }

    // ── GameSession 바인딩 ──────────────────────────────────────────

    private async Task TryFindOrSpawnSession()
    {
        if (_session != null) return;

        // Client: 이미 도착해 있으면 바로 바인딩
        var found = FindAnyObjectByType<GameSession>();
        if (found != null)
        {
            BindSession(found);

            var foundDeck = FindAnyObjectByType<GameDeck>();
            if (foundDeck != null) BindDeck(foundDeck);

            // 미니게임에서 돌아온 Host만 보상 지급과 다음 라운드 시작을 수행합니다.
            if (_runner.IsServer)
                found.ResumeBoardAfterMiniGame();
            return;
        }

        // Host: 비동기 Spawn (Fusion이 프리팹 로딩을 비동기로 처리하는 경우 안전)
        if (_runner.IsServer && gameSessionPrefab != null)
        {
            var obj = await _runner.SpawnAsync(gameSessionPrefab);
            if (obj != null) BindSession(obj.GetComponent<GameSession>());

            // GameSession 등장 후 → GameDeck Spawn
            await SpawnGameDeckAsync();

            // 그 다음 → 슬롯마다 NetworkPlayer Spawn
            await SpawnNetworkPlayersAsync();
        }
    }

    private async Task SpawnGameDeckAsync()
    {
        if (!_runner.IsServer || gameDeckPrefab == null) return;
        if (_deck != null) return;

        var obj = await _runner.SpawnAsync(gameDeckPrefab);
        if (obj != null) BindDeck(obj.GetComponent<GameDeck>());
    }

    /// <summary>GameDeck.Spawned()에서 호출 (Client 동기화 안전판).</summary>
    public void RegisterDeck(GameDeck deck)
    {
        BindDeck(deck);
    }

    private void BindDeck(GameDeck deck)
    {
        if (_deck == deck) return;
        _deck = deck;
        DeckReady?.Invoke(deck);
    }

    /// <summary>Host 전용 — NetworkBootstrap에 보관된 슬롯 정보로 NetworkPlayer를 Spawn.</summary>
    private async Task SpawnNetworkPlayersAsync()
    {
        if (!_runner.IsServer) return;
        if (networkPlayerPrefab == null)
        {
            Debug.LogError("[GameController] networkPlayerPrefab이 인스펙터에 연결되지 않았습니다.");
            return;
        }

        var bootstrap = NetworkBootstrap.Instance;
        if (bootstrap == null)
        {
            Debug.LogError("[GameController] NetworkBootstrap.Instance를 찾을 수 없습니다.");
            return;
        }

        for (int i = 0; i < NetworkBootstrap.MaxSlots; i++)
        {
            var owner = bootstrap.LobbySlotOwners[i];
            if (owner == PlayerRef.None) continue;

            int    slotIndex = i;
            string slotName  = bootstrap.LobbySlotNames[i];

            var obj = await _runner.SpawnAsync(
                networkPlayerPrefab,
                inputAuthority: owner,
                onBeforeSpawned: (runner, no) =>
                {
                    var np = no.GetComponent<NetworkPlayer>();
                    np.SlotIndex  = slotIndex;
                    np.PlayerName = slotName;
                }
            );

            if (obj == null)
                Debug.LogWarning($"[GameController] 슬롯 {slotIndex} NetworkPlayer Spawn 실패");
        }
    }

    /// <summary>GameSession이 자신의 Spawned()에서 직접 호출. Client 동기화 안전판.</summary>
    public void RegisterSession(GameSession session)
    {
        BindSession(session);
    }

    private void BindSession(GameSession session)
    {
        if (_session == session) return;
        _session = session;
        SessionReady?.Invoke(session);
    }

    // ── INetworkRunnerCallbacks (이번 단계에서는 거의 빈 채로) ────────

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
