using UnityEngine;
using Fusion;
using System.Linq; // Count() 함수 사용을 위해 추가

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [Networked] public int CurrentPlayerIndex { get; set; }
    [Networked] public TickTimer TurnTimer { get; set; }

    public float baseTurnTime = 10f;

    // 턴이 바뀌었는지 감지하기 위해 이전 턴 값을 저장해 두는 변수입니다.
    private int _previousPlayerIndex = -1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void Spawned()
    {
        // 1. 매니저가 서버에 정상적으로 스폰되었는지 확인
        Debug.Log($"[NetworkGameManager] Spawned() 실행됨! / 방장 권한(HasStateAuthority): {HasStateAuthority}");

        if (HasStateAuthority)
        {
            CurrentPlayerIndex = 0;
            StartNewTurn();
        }
    }


    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority) // 방장만 시간 체크해서 턴 넘김
        {
            if (TurnTimer.Expired(Runner))
            {
                // [개선 포인트] 실제 접속 중인 플레이어 수에 맞춰서 턴 계산!
                int playerCount = Mathf.Max(1, Runner.ActivePlayers.Count());
                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % playerCount;
                StartNewTurn();
            }
        }
    }

    // [개선 포인트] 화면이 그려질 때마다 호출되는 Render 함수를 활용해 UI를 최적화합니다.
    public override void Render()
    {
        // 매 프레임 UI를 바꾸는 게 아니라, 턴(CurrentPlayerIndex)이 바뀌었을 때 단 한 번만 실행합니다.
        if (_previousPlayerIndex != CurrentPlayerIndex)
        {
            _previousPlayerIndex = CurrentPlayerIndex;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateTurnUI(CurrentPlayerIndex + 1);
            }
        }
    }

    private void StartNewTurn()
    {
        // 2. 타이머가 몇 초로 세팅되는지 확인
        Debug.Log($"[NetworkGameManager] StartNewTurn() 실행됨! / 설정된 턴 시간: {baseTurnTime}초");

        TurnTimer = TickTimer.CreateFromSeconds(Runner, baseTurnTime);

        // 3. 타이머가 정상적으로 켜졌는지 확인
        Debug.Log($"[NetworkGameManager] 타이머 켜짐 여부(IsRunning): {TurnTimer.IsRunning}");

        RpcSendSystemMessage($"Player{CurrentPlayerIndex + 1}’s turn.");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSendSystemMessage(string message)
    {
        Debug.Log($"[서버 방송] {message}");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowMessage(message);
        }
    }
}