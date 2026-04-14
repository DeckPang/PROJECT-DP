using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BranchSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private TMP_Text[] choiceButtonTexts;

    private PlayerPawn currentPawn;

    private void Start()
    {
        HideChoices();
    }

    public void ShowChoices(PlayerPawn pawn, List<BoardNode> nextNodes)
    {
        currentPawn = pawn;

        if (rootPanel != null)
            rootPanel.SetActive(true);

        if (titleText != null)
            titleText.text = "갈림길 선택";

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool isValid = nextNodes != null && i < nextNodes.Count && nextNodes[i] != null;

            choiceButtons[i].gameObject.SetActive(isValid);
            choiceButtons[i].onClick.RemoveAllListeners();

            if (!isValid)
                continue;

            int capturedIndex = i;
            BoardNode targetNode = nextNodes[i];

            if (choiceButtonTexts != null && i < choiceButtonTexts.Length && choiceButtonTexts[i] != null)
            {
                choiceButtonTexts[i].text = $"{i + 1}번 길 (Node {targetNode.NodeId})";
            }

            choiceButtons[i].onClick.AddListener(() => OnClickChoice(capturedIndex));
        }
    }

    public void HideChoices()
    {
        currentPawn = null;

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    private void OnClickChoice(int index)
    {
        if (currentPawn == null)
            return;

        currentPawn.SelectBranch(index);
    }
}