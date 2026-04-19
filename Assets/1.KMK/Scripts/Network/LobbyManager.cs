using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner  runnerPrefab;
    [SerializeField] private NetworkObject  lobbyStatePrefab; // LobbyState 프리팹

    private const int    MaxPlayers         = 4;
    private const string DefaultSessionName = "DeckPang";

    private NetworkRunner _runner;

    public bool IsHost => _runner != null && _runner.IsServer;

    // ── 이벤트 ──────────────────────────────────────────────────────────────

    public event Action<int>               OnPlayerCountChanged;

    // ── 연결 ────────────────────────────────────────────────────────────────

    public async Task<StartGameResult> StartHost(string roomName, string nickname)
    {
        _runner = CreateRunner();
        var sessionName = string.IsNullOrEmpty(roomName) ? DefaultSessionName : roomName;

        return await _runner.StartGame(new StartGameArgs
        {
            GameMode     = GameMode.Host,
            SessionName  = sessionName,
            PlayerCount  = MaxPlayers,
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>(),
        });
    }

    public async Task<StartGameResult> StartClient(string nickname)
    {
        _runner = CreateRunner();

        return await _runner.StartGame(new StartGameArgs
        {
            GameMode     = GameMode.Client,
            SessionName  = DefaultSessionName,
            PlayerCount  = MaxPlayers,
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>(),
        });
    }

    public void StartGame()
    {
        if (!IsHost) return;
        _runner.LoadScene(SceneRef.FromIndex(1));
    }

    // ── Ready 시스템 ─────────────────────────────────────────────────────

    public void ToggleReady()
    {
        int slot = GetMySlot();
        if (slot < 0)
        {
            Debug.LogWarning("[LobbyManager] ToggleReady 실패: 내 슬롯을 찾을 수 없음");
            return;
        }
        if (LobbyState.Instance == null)
        {
            Debug.LogWarning("[LobbyManager] ToggleReady 실패: LobbyState.Instance가 null");
            return;
        }

        bool current = LobbyState.Instance.IsSlotReady(slot);
        Debug.Log($"[LobbyManager] ToggleReady → slot={slot}, {current} → {!current}");
        LobbyState.Instance.RPC_SetReady(slot, !current);
    }

    public bool IsMySlotReady()
    {
        int slot = GetMySlot();
        return slot >= 0 && LobbyState.Instance != null && LobbyState.Instance.IsSlotReady(slot);
    }

    /// <summary>PlayerRef.PlayerId(1-based)로 슬롯을 결정합니다. (Host=0, Client1=1, ...)</summary>
    public int GetMySlot()
    {
        var local = _runner?.LocalPlayer ?? PlayerRef.None;
        if (local == PlayerRef.None) return -1;
        return local.PlayerId - 1;
    }

    // ── 내부 ────────────────────────────────────────────────────────────────

    private NetworkRunner CreateRunner()
    {
        var runner = Instantiate(runnerPrefab);
        runner.AddCallbacks(this);
        return runner;
    }

    private static int SlotOf(PlayerRef player) => player == PlayerRef.None ? -1 : player.PlayerId - 1;

    // ── INetworkRunnerCallbacks ──────────────────────────────────────────────

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // ① Host만: 최초 입장 시 LobbyState 스폰
        if (runner.IsServer && LobbyState.Instance == null && lobbyStatePrefab != null)
        {
            runner.Spawn(lobbyStatePrefab);
            Debug.Log("[LobbyManager] LobbyState 스폰");
        }

        int slot = SlotOf(player);
        if (slot < 0 || slot >= MaxPlayers) return;

        bool isHost = (slot == 0);
        string name = $"Player_{slot}";

        Debug.Log($"[LobbyManager] OnPlayerJoined → player={player}, slot={slot}, isHost={isHost}, isServer={runner.IsServer}");

        // ② Host만: 네트워크 상태 갱신
        if (runner.IsServer)
            LobbyState.Instance?.SetOccupied(slot, true);

        // ③ 모든 클라이언트: 인원수 UI 갱신
        OnPlayerCountChanged?.Invoke(runner.SessionInfo.PlayerCount);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        int slot = SlotOf(player);
        if (slot < 0 || slot >= MaxPlayers) return;

        Debug.Log($"[LobbyManager] OnPlayerLeft → slot={slot}");

        if (runner.IsServer)
            LobbyState.Instance?.SetOccupied(slot, false);

        OnPlayerCountChanged?.Invoke(runner.SessionInfo.PlayerCount);
    }

    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
