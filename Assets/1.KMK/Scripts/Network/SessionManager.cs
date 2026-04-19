using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// 게임 씬에서 플레이어 스폰/디스폰 및 네트워크 이벤트를 처리합니다.
/// </summary>
public class SessionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static SessionManager Instance { get; private set; }

    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    // 현재 접속 중인 플레이어
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    // 팅긴 플레이어 (슬롯 번호 → NetworkObject) — 재접속 대기
    private readonly Dictionary<int, NetworkObject> _disconnectedSlots = new();

    private NetworkRunner _runner;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        _runner = FindAnyObjectByType<NetworkRunner>();
        if (_runner == null)
        {
            Debug.LogError("[SessionManager] NetworkRunner를 찾을 수 없습니다.");
            return;
        }

        _runner.AddCallbacks(this);
    }

    // ── INetworkRunnerCallbacks ──────────────────────────────────────────────

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        if (_spawnedPlayers.ContainsKey(player)) return;

        // 게임 진행 중이면 재접속으로 처리
        if (TurnManager.Instance != null && TurnManager.Instance.IsGameRunning)
        {
            HandleReconnection(runner, player);
            return;
        }

        SpawnPlayer(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        if (!_spawnedPlayers.TryGetValue(player, out var networkObject)) return;

        var playerNetwork = networkObject.GetComponent<PlayerNetwork>();

        // 게임 진행 중이면 오브젝트 유지 + AI 전환
        if (TurnManager.Instance != null && TurnManager.Instance.IsGameRunning)
        {
            playerNetwork?.SetDisconnected();
            _disconnectedSlots[playerNetwork?.PlayerNumber ?? 0] = networkObject;
            _spawnedPlayers.Remove(player);
            Debug.Log($"[SessionManager] {playerNetwork?.PlayerNumber}P 팅김 → AI 대기");
        }
        else
        {
            // 게임 시작 전 퇴장이면 즉시 Despawn
            runner.Despawn(networkObject);
            _spawnedPlayers.Remove(player);
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        foreach (var player in runner.ActivePlayers)
        {
            if (!_spawnedPlayers.ContainsKey(player))
                SpawnPlayer(runner, player);
        }
    }

    // ── 내부 로직 ────────────────────────────────────────────────────────────

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        var spawnPos = GetSpawnPoint(_spawnedPlayers.Count);
        var networkPlayer = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
        _spawnedPlayers[player] = networkPlayer;

        Debug.Log($"[SessionManager] 플레이어 스폰: {player} ({_spawnedPlayers.Count}/{runner.SessionInfo.PlayerCount})");

        if (_spawnedPlayers.Count >= runner.SessionInfo.PlayerCount)
        {
            LockSession(runner);
            TurnManager.Instance?.StartGame(_spawnedPlayers.Count);
        }
    }

    private void HandleReconnection(NetworkRunner runner, PlayerRef player)
    {
        if (_disconnectedSlots.Count == 0)
        {
            Debug.LogWarning("[SessionManager] 재접속 슬롯 없음 — 새 플레이어로 처리");
            return;
        }

        // 비어있는 첫 번째 슬롯에 재할당
        // TODO: 추후 플레이어 고유 ID 기반 매칭으로 개선
        foreach (var slot in _disconnectedSlots)
        {
            var networkObject = slot.Value;
            runner.SetPlayerObject(player, networkObject);
            networkObject.GetComponent<PlayerNetwork>()?.SetConnected();

            _spawnedPlayers[player] = networkObject;
            _disconnectedSlots.Remove(slot.Key);

            Debug.Log($"[SessionManager] {player} → 슬롯 {slot.Key} 재접속 완료");
            break;
        }
    }

    private void LockSession(NetworkRunner runner)
    {
        // 게임 시작 후 새 플레이어 참가 차단
        runner.SessionInfo.IsOpen = false;
        runner.SessionInfo.IsVisible = false;
        Debug.Log("[SessionManager] 세션 잠금 완료");
    }

    private Vector3 GetSpawnPoint(int index)
    {
        if (spawnPoints != null && index < spawnPoints.Length)
            return spawnPoints[index].position;

        return Vector3.zero;
    }

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
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
