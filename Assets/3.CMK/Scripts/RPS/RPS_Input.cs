using UnityEngine;
using UnityEngine.UI;

public class RPS_Input : MonoBehaviour
{
    public RPS rps;
    public RPS_Player owner;

    [SerializeField] private Button rockButton;
    [SerializeField] private Button paperButton;
    [SerializeField] private Button scissorsButton;

    public void SetInteractable(bool value)
    {
        rockButton.interactable = value;
        paperButton.interactable = value;
        scissorsButton.interactable = value;
    }

    public void SetChoice(RPSChoice choice)
    {
        if (rps == null || owner == null) return;
        if (rps.currentPlayer == null) return;
        if (!rps.isInputEnabled) return;
        if (rps.currentPlayer != owner) return;

        owner.SetChoice(choice);
    }

    public void OnClickRock() => SetChoice(RPSChoice.Rock);
    public void OnClickPaper() => SetChoice(RPSChoice.Paper);
    public void OnClickScissors() => SetChoice(RPSChoice.Scissors);
}