using UnityEngine;
using VInspector;

public class GameManager : MonoBehaviour
{
    [Header("매니저 연결 (인스펙터에서 드래그)")]
    public TurnManager turnManager;
    public TimerManager timerManager;

    public float baseTurnTime = 10f; // 기본 10초 턴

    void Start()
    {
        // 게임 세팅
        turnManager.InitGame();

        // 첫 턴 시작
        StartNewTurn();
    }

    void Update()
    {        
        timerManager.TickTimer(Time.deltaTime);

        // 시간이 다 됐는지 확인
        if (timerManager.IsTimeUp())
        {
            // 다음 순서
            turnManager.NextTurn();

            // 새로운 턴 타이머 다시 시작
            StartNewTurn();
        }

        UIManager.Instance.OnTimerUpdated(timerManager.GetCurrentTime());        

    }

    void StartNewTurn()
    {
        Debug.Log($"{turnManager.GetCurrentPlayer() + 1}P 턴, {baseTurnTime}초 카운트다운 시작!");
        UIManager.Instance.OnplayerTurnUpdated((turnManager.GetCurrentPlayer()+1).ToString());

        timerManager.StartTimer(baseTurnTime);
    }

    [Button]
    // 테스트 함수 (카드 사용 시 10초 연장)
    public void TestUseCardButton()
    {
        Debug.Log("카드를 사용했습니다 (시간 초기화)");
        timerManager.ResetTimer(baseTurnTime);
    }
}