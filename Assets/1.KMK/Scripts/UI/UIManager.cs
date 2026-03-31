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
    public InfoUI infoUI;

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

    public void TimerTextUpdated(float currentTime)
    {
        timerUI.UpdateTimerDisplay(currentTime);
    }

    public void PlayerTurnTextUpdated(string playerName)
    {
        playerTurnUI.UpdatePlayerTurnDisplay(playerName);
    }

    public void InfoTextUpdated(string content)
    {
        infoUI.UpdateInfoText(content);
    }
}
