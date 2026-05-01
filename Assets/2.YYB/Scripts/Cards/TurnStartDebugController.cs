using UnityEngine;

public class TurnStartDebugController : MonoBehaviour
{
    [SerializeField] private SharedDeckManager sharedDeckManager;
    [SerializeField] private PlayerHandController playerHandController;
    [SerializeField] private CardDefinition debugRewardCard;

    public void InitializeSharedDeck()
    {
        sharedDeckManager?.InitializeDeck();
    }

    public void StartLocalTurn()
    {
        playerHandController?.StartTurnLocal();
    }

    public void DrawOneCard()
    {
        playerHandController?.DrawFromSharedDeck(1);
    }

    public void GiveRewardCard()
    {
        if (debugRewardCard == null)
        {
            Debug.LogWarning("[TurnDebug] debugRewardCard가 비어 있습니다.");
            return;
        }

        playerHandController?.AddRewardCardToHand(debugRewardCard);
    }

    public void PrintHand()
    {
        playerHandController?.PrintHandDebug();
    }
}