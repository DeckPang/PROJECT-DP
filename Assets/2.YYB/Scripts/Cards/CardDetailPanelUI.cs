using TMPro;
using UnityEngine;

public class CardDetailPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text detailText;

    private void Start()
    {
        ShowEmpty();
    }

    public void ShowEmpty()
    {
        if (detailText == null)
            return;

        detailText.text = "카드를 선택하세요.";
    }

    public void ShowCard(CardDefinition card)
    {
        if (detailText == null || card == null)
            return;

        detailText.text =
            $"이름: {card.CardName}\n" +
            $"ID: {card.CardId}\n" +
            $"코스트: {card.Cost}\n" +
            $"타입: {card.CardType}\n" +
            $"공급: {card.CardPoolType}\n" +
            $"효과: {card.EffectType}\n" +
            $"값: {card.Value}\n" +
            $"설명: {card.Description}";
    }
}