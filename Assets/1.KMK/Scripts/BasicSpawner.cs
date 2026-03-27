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
    [Header("스폰할 플레이어 프리팹을 여기에 넣으세요")]
    public NetworkPrefabRef playerPrefab;

    async void Start()
    {
        _runner = gameObject.GetComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // 퓨전 서버야, 무슨 일 생기면 이 스크립트(this)한테 알려줘! 라고 등록
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
        // 클라이언트가 맘대로 스폰하면 핵(해킹)이 발생함. 무조건 방장(서버)만 스폰 권한을 가짐
        if (runner.IsServer)
        {
            Debug.Log($"플레이어 {player.PlayerId} 님을 맵에 소환합니다!");
            Vector3 spawnPosition = new Vector3(0, posY++, 0); // 스폰 위치            

            // 유니티의 Instantiate 대신, 퓨전 전용 Spawn 함수를 사용해야 남들 화면에도 보임
            runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
        }
    }

    #region 안 쓰지만 필수로 적어둬야 하는 퓨전 규격(콜백) 함수들
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = new NetworkInputData();

        // 1. 여기서 내 키보드 WASD 입력을 받아
        float x = Input.GetAxisRaw("Horizontal"); // A, D (좌우)
        float z = Input.GetAxisRaw("Vertical");   // W, S (상하)

        // 2. 방향을 계산해서 택배 상자에 담고
        data.direction = new Vector3(x, 0, z).normalized;

        // 3. 서버로 택배 발송!
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