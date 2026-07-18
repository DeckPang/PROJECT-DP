using UnityEngine;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public GameTimer timer;
    public TMP_Text gameOverText; // 처음엔 비활성화 해두기
    private bool isGameOver = false;

    void Start()
    {
        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isGameOver) return;

        bool timeOver = timer.IsTimeOver;
        bool allDead = PlayerLifeManager.Instance != null && PlayerLifeManager.Instance.AllPlayersDead;

        if (timeOver || allDead)
        {
            TriggerGameOver(allDead);
        }
    }

    void TriggerGameOver(bool byPlayerDeath)
    {
        isGameOver = true;

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = byPlayerDeath ? "GAME OVER" : "TIME OVER";
        }

    }
}