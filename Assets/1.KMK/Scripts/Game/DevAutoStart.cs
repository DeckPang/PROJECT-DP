using Fusion;
using UnityEngine;

/// <summary>
/// 개발 편의용 — Game 씬에서 직접 Play 했을 때 로비를 우회하고 즉시 Host로 시작합니다.
/// Runner가 이미 떠있으면(=로비에서 진입) 아무 일도 하지 않음.
///
/// ⚠️ 빌드 전 비활성화 또는 GameObject 자체를 비활성화하세요.
/// </summary>
public class DevAutoStart : MonoBehaviour
{
    [Header("개발 편의 — 빌드 전 끄세요")]
    [SerializeField] private bool   autoStart   = true;
    [SerializeField] private string mockSession = "DevSession";

    [Tooltip("솔로 테스트용 슬롯 수. 모든 슬롯의 InputAuthority가 나(호스트)가 되어 모두를 컨트롤 가능.")]
    [SerializeField, Range(1, 4)] private int soloSlotCount = 4;

    private async void Start()
    {
        if (!autoStart) return;

        // 이미 Runner가 떠있다면 로비를 거쳐 온 것 — 우회 불필요
        var existing = FindAnyObjectByType<NetworkRunner>();
        if (existing != null && existing.IsRunning) return;

        var bootstrap = NetworkBootstrap.Instance;
        if (bootstrap == null)
        {
            Debug.LogError("[DevAutoStart] NetworkBootstrap을 찾을 수 없습니다. NetworkRoot가 Game 씬에도 있는지 확인하세요.");
            return;
        }

        Debug.LogWarning("[DevAutoStart] 로비 우회 — 즉시 Host로 시작합니다.");

        var result = await bootstrap.StartHost(mockSession);
        if (!result.Ok)
        {
            Debug.LogError($"[DevAutoStart] Host 시작 실패: {result.ShutdownReason}");
            return;
        }

        // soloSlotCount만큼 슬롯을 호스트 본인으로 채움 → 호스트가 모든 플레이어를 컨트롤 가능
        var local = bootstrap.Runner.LocalPlayer;
        for (int i = 0; i < NetworkBootstrap.MaxSlots; i++)
        {
            if (i < soloSlotCount)
            {
                bootstrap.LobbySlotOwners[i] = local;
                bootstrap.LobbySlotNames[i]  = $"DevP{i + 1}";
            }
            else
            {
                bootstrap.LobbySlotOwners[i] = PlayerRef.None;
                bootstrap.LobbySlotNames[i]  = string.Empty;
            }
        }

        // GameController에게 Runner 준비됐다고 알림 → 초기화 진행
        var gameController = FindAnyObjectByType<GameController>();
        if (gameController != null)
        {
            gameController.OnRunnerReady();
        }
        else
        {
            Debug.LogWarning("[DevAutoStart] GameController를 찾지 못해 자동 초기화를 못 했습니다.");
        }
    }
}
