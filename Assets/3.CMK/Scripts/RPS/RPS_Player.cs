using System.Collections;
using UnityEngine;

public enum RPSChoice
{
    Rock,
    Paper,
    Scissors
}

public class RPS_Player : MonoBehaviour
{
    public int order;
    public RPSChoice choice;
    public int score;
    private bool hasChosen = false;

    public IEnumerator PlayTurn()
    {
        hasChosen = false;
        Debug.Log(name + " waiting for choice");
        while (!hasChosen)
        {
            yield return null;
        }
        Debug.Log(name + " chose: " + choice);
    }

    public void SetChoice(RPSChoice c)
    {
        // 이미 선택했다면 중복 호출 방지
        if (hasChosen)
            return;

        choice = c;
        hasChosen = true;
    }

    public void AddScore(int amount)
    {
        score += amount;
    }
}