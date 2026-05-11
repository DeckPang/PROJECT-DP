using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Runner 콜백을 받아 LobbyState를 조작하고, UI 입력을 Host에게 RPC로 중계합니다.
/// </summary>
public class LobbyController : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkBootstrap bootstrap;
    [SerializeField] private NetworkObject    lobbyStatePrefab;
    [SerializeField] private int              gameSceneIndex = 1;

    private NetworkRunner _runner;
    private LobbyState    _state;

    // ── 이벤트 ───────────────────────────────────────────────────────

    /// <summary>LobbyState가 처음으로 발견된 직후. UI는 여기서 구독을 시작.</summary>
    public event Action<LobbyState> LobbyStateReady;

    /// <summary>연결이 끊어졌을 때 (사용자/호스트/네트워크 어떤 이유든).</summary>
    public event Action LobbyDisconnected;

    // ── 외부 API ─────────────────────────────────────────────────────

    public LobbyState    State  => _state;
    public NetworkRunner Runner => _runner;
    public bool          IsHost => _runner != null && _runner.IsServer;
    public string        SessionName => _runner?.SessionInfo?.Name ?? string.Empty;

    public int GetMySlot()
    {
        if (_state == null || _runner == null) return -1;
        return _state.FindSlotOf(_runner.LocalPlayer);
    }

    public async Task<StartGameResult> Host(string roomName)
    {
        var result = await bootstrap.StartHost(roomName);
        if (result.Ok) AttachToRunner(bootstrap.Runner);
        return result;
    }

    public async Task<StartGameResult> Join(string roomName)
    {
        var result = await bootstrap.StartClient(roomName);
        if (result.Ok) AttachToRunner(bootstrap.Runner);
        return result;
    }

    public void ToggleMyReady()
    {
        int slot = GetMySlot();
        if (slot <= 0 || _state == null) return;
        bool now = _state.Slots[slot].Ready;
        _state.RPC_SetReady(slot, !now);
    }

    public void SetMyName(string name)
    {
        int slot = GetMySlot();
        if (slot < 0 || _state == null || string.IsNullOrEmpty(name)) return;
        _state.RPC_SetName(slot, name);
    }

    public void StartGame()
    {
        if (!IsHost || _state == null) return;
        if (!_state.CanStart()) return;

        _runner.SessionInfo.IsOpen    = false;
        _runner.SessionInfo.IsVisible = false;
        _runner.LoadScene(SceneRef.FromIndex(gameSceneIndex));
    }

    // ── 내부 ────────────────────────────────────────────────────────

    private void AttachToRunner(NetworkRunner runner)
    {
        _runner = runner;
        runner.AddCallbacks(this);
    }

    private void EnsureLobbyStateBound()
    {
        if (_state != null) return;
        var found = FindAnyObjectByType<LobbyState>();
        if (found != null) BindLobbyState(found);
    }

    /// <summary>LobbyState가 자신의 Spawned()에서 직접 호출. Client 동기화 타이밍 안전판.</summary>
    public void RegisterLobbyState(LobbyState state)
    {
        BindLobbyState(state);
    }

    private void BindLobbyState(LobbyState state)
    {
        if (_state == state) return;
        _state = state;
        LobbyStateReady?.Invoke(state);
    }

    // ── INetworkRunnerCallbacks ──────────────────────────────────────

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Host: 첫 입장 시 LobbyState Spawn
            if (_state == null && lobbyStatePrefab != null)
            {
                var obj = runner.Spawn(lobbyStatePrefab);
                BindLobbyState(obj.GetComponent<LobbyState>());
            }
            _state?.TryAssignSlot(player, out _);
        }
        else
        {
            EnsureLobbyStateBound();
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer) _state?.ReleaseSlot(player);
    }

    public void OnConnectedToServer(NetworkRunner runner)   => EnsureLobbyStateBound();
    public void OnSceneLoadDone   (NetworkRunner runner)    => EnsureLobbyStateBound();

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        LobbyDisconnected?.Invoke();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _runner = null;
        _state  = null;
        LobbyDisconnected?.Invoke();
    }

    // 이번 단계에서는 사용하지 않는 콜백
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
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
