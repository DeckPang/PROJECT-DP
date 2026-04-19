using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    private static GameUIManager instance = null;
    public static GameUIManager Instance
    {
        get
        {
            if (null == instance) return null;
            return instance;
        }
    }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI playerTurnText;
    [SerializeField] private TextMeshProUGUI infoText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        TurnManager.OnTurnUpdated   += UpdateCurrentPlayerTurnText;
        TimerManager.OnTimerUpdated += UpdateTimerText;
    }

    private void OnDisable()
    {
        TurnManager.OnTurnUpdated   -= UpdateCurrentPlayerTurnText;
        TimerManager.OnTimerUpdated -= UpdateTimerText;
    }

    public void UpdateTimerText(float currentTime)
    {
        timerText.text = $"남은 시간: {currentTime:F0}";
    }

    public void UpdateCurrentPlayerTurnText(int playerNumber)
    {
        playerTurnText.text = $"현재 턴: {playerNumber}";
    }

    public void UpdateInfoText(string content)
    {
        infoText.text = content;
    }
}
