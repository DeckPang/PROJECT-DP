using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardItemTextUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;

    private int boundIndex;
    private Action<int> clickCallback;

    public void Bind(CardDefinition card, int index, Action<int> onClick, bool isSelected)
    {
        boundIndex = index;
        clickCallback = onClick;

        if (button == null)
            button = GetComponent<Button>();

        string prefix = isSelected ? "¢º " : "";
        labelText.text = $"{prefix}| {card.CardName} |\n| {card.Cost}ÄÚ |\n| {card.EffectType} |" ;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(NotifyClicked);
    }

    private void NotifyClicked()
    {
        clickCallback?.Invoke(boundIndex);
    }
}