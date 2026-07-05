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

        Debug.Log(name + " 선택 대기");

        while (!hasChosen)
        {
            yield return null;
        }

        Debug.Log(name + " 선택: " + choice);
    }

    public void SetChoice(RPSChoice c)
    {
        choice = c;
        hasChosen = true;
    }

    public void AddScore(int amount)
    {
        score += amount;
    }

}