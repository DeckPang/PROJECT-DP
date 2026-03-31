using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (NetworkGameManager.Instance == null ||
                    NetworkGameManager.Instance.Object == null ||
                    !NetworkGameManager.Instance.Object.IsValid)
            return;

        // UI 매니저가 아직 준비 안 됐으면 에러 방지용으로 대기
        if (UIManager.Instance == null) return;

        // 타이머는 실시간으로 화면에 줄어드는 걸 보여줘야 하므로 Update에서 처리
        if (NetworkGameManager.Instance.TurnTimer.IsRunning)
        {
            float remainTime = NetworkGameManager.Instance.TurnTimer.RemainingTime(NetworkGameManager.Instance.Runner) ?? 0f;
            UIManager.Instance.TimerTextUpdated(remainTime);
        }

    }

    // 턴 텍스트는 NetworkGameManager가 "턴 바뀌었어!"라고 알려줄 때만 바꿉니다.
    public void UpdateTurnUI(int currentPlayerNumber)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PlayerTurnTextUpdated(currentPlayerNumber.ToString());
        }
    }

    public void ShowMessage(string msg)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.InfoTextUpdated(msg);
        }
    }
}