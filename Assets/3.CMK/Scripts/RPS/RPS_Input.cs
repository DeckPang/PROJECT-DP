using UnityEngine;

public class RPS_Input : MonoBehaviour
{
    public RPS rps;

    public void SetChoice(RPSChoice choice)
    {
        if (rps == null) return;
        if (rps.currentPlayer == null) return;
        if (!rps.isInputEnabled) return;

        rps.currentPlayer.SetChoice(choice);
    }

    public void OnClickRock()
    {
        SetChoice(RPSChoice.Rock);
    }

    public void OnClickPaper()
    {
        SetChoice(RPSChoice.Paper);
    }

    public void OnClickScissors()
    {
        SetChoice(RPSChoice.Scissors);
    }
}