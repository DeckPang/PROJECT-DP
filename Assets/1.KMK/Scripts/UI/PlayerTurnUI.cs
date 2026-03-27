using TMPro;
using UnityEngine;

public class PlayerTurnUI : MonoBehaviour
{
    public TextMeshProUGUI playerTurnText;

    public void UpdatePlayerTurnDisplay(string playerName)
    {
        playerTurnText.text = $"CurrentPlayer: {playerName}";
    }
}
