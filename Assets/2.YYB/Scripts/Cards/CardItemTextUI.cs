using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 한 장의 UI. 동적으로 Instantiate되어 사용.
/// (Phase 1.6c에서 활용 예정 — 현재는 정의만 유지)
/// </summary>
public class CardItemTextUI : MonoBehaviour
{
    [SerializeField] private Button   button;
    [SerializeField] private TMP_Text labelText;

    private int boundIndex;
    private Action<int> clickCallback;

    public void Bind(CardDefinition card, int index, Action<int> onClick, bool isSelected = false, bool interactable = true)
    {
        boundIndex    = index;
        clickCallback = onClick;

        if (button == null) button = GetComponent<Button>();

        string prefix = isSelected ? "★ " : "";
        if (labelText != null)
            labelText.text = $"{prefix}{card.CardName}\n[{card.EffectType} {card.Value}]";

        if (button != null)
        {
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(NotifyClicked);
        }
    }

    private void NotifyClicked()
    {
        clickCallback?.Invoke(boundIndex);
    }
}
