using UnityEngine;
using UnityEngine.InputSystem;

public class DeckDebugController : MonoBehaviour
{
    [SerializeField] private DeckController deckController;
    [SerializeField] private CharacterInfo characterInfo;

    private void Start()
    {
        Debug.Log("=== Deck Debug Controller ===");
        Debug.Log("R : 카드 1장 드로우");
        Debug.Log("T : 카드 3장 드로우");
        Debug.Log("H : 손패 출력");
        Debug.Log("U / I / O : 손패 0 / 1 / 2 사용");
        Debug.Log("M : 마나 전체 회복");
    }

    private void Update()
    {
        if (Keyboard.current == null || deckController == null)
            return;

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            deckController.DrawCard();
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            deckController.DrawMany(3);
        }

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            deckController.PrintHand();
        }

        if (Keyboard.current.uKey.wasPressedThisFrame)
        {
            deckController.UseCard(0);
        }

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            deckController.UseCard(1);
        }

        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            deckController.UseCard(2);
        }

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            if (characterInfo != null)
                characterInfo.RestoreManaToFull();
        }
    }
}