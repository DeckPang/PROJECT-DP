using UnityEngine;

public class UIManager : MonoBehaviour
{
    private static UIManager instance = null;
    public static UIManager Instance
    {
        get
        {
            if (null == instance)
            {
                return null;
            }

            return instance;
        }
    }

    [Header("UI ¿¬°á")]
    public TimerUI timerUI;
    public PlayerTurnUI playerTurnUI;

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

    public void OnTimerUpdated(float currentTime)
    {
        timerUI.UpdateTimerDisplay(currentTime);
    }

    public void OnplayerTurnUpdated(string playerName)
    {
        playerTurnUI.UpdatePlayerTurnDisplay(playerName);
    }
}
