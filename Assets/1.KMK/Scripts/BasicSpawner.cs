using UnityEngine;
using Fusion;
using System.Threading.Tasks;
using System.Collections.Generic;
using Fusion.Sockets;

// INetworkRunnerCallbacks를 추가해서 서버의 각종 이벤트를 수신할 수 있게 만듦
public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    private int posY = 0;

    [Header("스폰할 네트워크 매니저 프리팹을 여기에 넣으세요")]
    public NetworkPrefabRef gameManagerPrefab;

    [Header("스폰할 플레이어 프리팹을 여기에 넣으세요")]
    public NetworkPrefabRef playerPrefab;

    async void Start()
    {
        _runner = gameObject.GetComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        await StartSimulation();
    }

    async Task StartSimulation()
    {
        Debug.Log("서버 접속 시도 중...");
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "TestRoom",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };
        await _runner.StartGame(startGameArgs);
        Debug.Log("서버 접속 완료!");
    }

    // 누군가 파티룸에 입장하면 자동으로 실행되는 핵심 함수
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            if (player == runner.LocalPlayer)
            {
                runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
                Debug.Log("서버가 NetworkGameManager를 성공적으로 스폰했습니다!");
            }

            Vector3 spawnPosition = new Vector3(0, posY++, 0);
            runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

            // [수정된 부분] NetworkGameManager가 네트워크 상에 완전히 준비(초기화)되었는지 확인!
            if (NetworkGameManager.Instance != null &&
                NetworkGameManager.Instance.Object != null &&
                NetworkGameManager.Instance.Object.IsValid)
            {
                NetworkGameManager.Instance.RpcSendSystemMessage($"Player {player.PlayerId} has joined.");
            }
            else
            {
                // 방장이 처음 방을 만들고 입장하는 바로 그 찰나에는 매니저가 초기화 중일 수 있습니다.
                // 이럴 때는 에러를 내지 않고 유니티 콘솔에만 조용히 기록을 남깁니다.
                Debug.Log($"Player {player.PlayerId} has joined.");
            }
        }
    }

    #region 안 쓰지만 필수로 적어둬야 하는 퓨전 규격(콜백) 함수들
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = new NetworkInputData();

        float x = Input.GetAxisRaw("Horizontal"); 
        float z = Input.GetAxisRaw("Vertical");   
        
        data.direction = new Vector3(x, 0, z).normalized;

        input.Set(data);
    }
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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}