using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RPS_ScoreUI : MonoBehaviour
{
    public TMP_Text scoreText;

    public void UpdateUI(List<RPS_Player> players)
    {
        players.Sort((a, b) => a.name.CompareTo(b.name  ));

        string text = "SCORE BOARD\n\n";

        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];

            text += $"{p.name} : {p.score}\n";
        }

        scoreText.text = text;
    }
}