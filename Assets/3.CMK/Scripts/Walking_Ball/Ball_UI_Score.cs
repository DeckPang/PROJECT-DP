using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Ball_UI_Score : MonoBehaviour
{
    public TMP_Text Ui_Score_Text;

    public void BallUpdateUI(List<Ball_Player> players)
    {
        players.Sort((a, b) => a.name.CompareTo(b.name));
        string text = "SCORE BOARD\n\n";
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            text += $"{p.name} : {p.score}\n";
        }
        Ui_Score_Text.text = text;
    }
}