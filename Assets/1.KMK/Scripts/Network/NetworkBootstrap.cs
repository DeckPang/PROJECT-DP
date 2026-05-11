using System.Threading.Tasks;
using Fusion;
using UnityEngine;

/// <summary>
/// NetworkRunner의 생성/시작/종료만 책임집니다.
/// 다른 시스템은 Runner를 여기서 가져갑니다.
/// </summary>
public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string defaultSession = "DeckPang";

    public NetworkRunner Runner { get; private set; }
    public bool IsRunning => Runner != null && Runner.IsRunning;

    public Task<StartGameResult> StartHost(string sessionName)
    {
        return StartGameInternal(GameMode.Host, sessionName);
    }

    public Task<StartGameResult> StartClient(string sessionName)
    {
        return StartGameInternal(GameMode.Client, sessionName);
    }

    public async Task Shutdown()
    {
        if (Runner == null) return;
        await Runner.Shutdown();
        Runner = null;
    }

    private async Task<StartGameResult> StartGameInternal(GameMode mode, string sessionName)
    {
        if (Runner != null)
        {
            Debug.LogWarning("[NetworkBootstrap] Runner가 이미 실행 중입니다.");
            return default;
        }

        Runner = Instantiate(runnerPrefab);
        Runner.ProvideInput = true;

        string session = string.IsNullOrEmpty(sessionName) ? defaultSession : sessionName;
        return await Runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = session,
            PlayerCount = maxPlayers,
            SceneManager = Runner.GetComponent<NetworkSceneManagerDefault>(),
        });
    }
}
