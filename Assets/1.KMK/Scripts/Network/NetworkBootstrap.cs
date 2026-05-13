using System.Threading.Tasks;
using Fusion;
using UnityEngine;

/// <summary>
/// NetworkRunner의 생성/시작/종료만 책임집니다.
/// 다른 시스템은 Runner를 여기서 가져갑니다.
/// </summary>
public class NetworkBootstrap : MonoBehaviour
{
    public static NetworkBootstrap Instance { get; private set; }

    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string defaultSession = "DeckPang";

    public NetworkRunner Runner { get; private set; }
    public bool IsRunning => Runner != null && Runner.IsRunning;

    // ── Lobby → Game 전환 시 슬롯 정보 스냅샷 ───────────────────────
    // LobbyState는 씬 전환 시 사라지므로, 게임 씬에서 사용할 슬롯 정보를 여기 잠시 보관.

    public const int MaxSlots = 4;

    public PlayerRef[] LobbySlotOwners { get; } = new PlayerRef[MaxSlots];
    public string[]    LobbySlotNames  { get; } = new string[MaxSlots];

    /// <summary>Host가 게임 시작 직전에 호출. LobbyState의 슬롯을 영구 메모리에 복사.</summary>
    public void SnapshotLobbySlots(LobbyState state)
    {
        if (state == null) return;
        for (int i = 0; i < MaxSlots; i++)
        {
            if (state.Slots[i].Occupied)
            {
                LobbySlotOwners[i] = state.Slots[i].Owner;
                LobbySlotNames[i]  = state.Slots[i].Name.ToString();
            }
            else
            {
                LobbySlotOwners[i] = PlayerRef.None;
                LobbySlotNames[i]  = string.Empty;
            }
        }
    }

    private void Awake()
    {
        // 중복 방지: 이미 다른 씬에서 살아있는 Bootstrap이 있으면 나는 소멸
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

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
