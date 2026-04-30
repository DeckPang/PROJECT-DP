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

    private bool hasChosen = false;

    public IEnumerator PlayTurn()
    {
        hasChosen = false;

        Debug.Log(name + " 선택 대기 중...");

        // 선택할 때까지 대기
        while (!hasChosen)
        {
            yield return null;
        }

        Debug.Log(name + " 선택 완료: " + choice);
    }

    //  RPS에서 호출됨
    public void SetChoice(RPSChoice c)
    {
        choice = c;
        hasChosen = true;
    }
}