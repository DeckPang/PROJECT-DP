using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardHandPanelUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerHandController handController;
    [SerializeField] private SharedDeckManager sharedDeckManager;
    [SerializeField] private PlayerPawn playerPawn;

    [Header("UI")]
    [SerializeField] private CardItemTextUI cardItemPrefab;
    [SerializeField] private Transform cardItemRoot;
    [SerializeField] private CardDetailPanelUI detailPanel;
    [SerializeField] private Button useButton;
    [SerializeField] private TMP_Text summaryText;

    private int selectedIndex = -1;

    private void OnEnable()
    {
        if (handController != null)
            handController.OnHandChanged += RefreshAll;

        if (sharedDeckManager != null)
            sharedDeckManager.OnDeckStateChanged += RefreshAll;
    }

    private void OnDisable()
    {
        if (handController != null)
            handController.OnHandChanged -= RefreshAll;

        if (sharedDeckManager != null)
            sharedDeckManager.OnDeckStateChanged -= RefreshAll;
    }

    private void Start()
    {
        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(UseSelectedCard);
        }

        RefreshAll();
    }

    private void Update() //임시로 업데이트 넣어놓음. 나중에 이벤트 생성 및 호출방식으로 바꿀예정
    {
        RefreshUseButtonState();
    }

    private void RefreshAll()
    {
        RefreshSummary();
        RefreshHandList();
        RefreshDetail();
        RefreshUseButtonState();
    }

    private void RefreshSummary()
    {
        if (summaryText == null)
            return;

        int handCount = handController != null ? handController.HandCount : 0;
        int drawCount = sharedDeckManager != null ? sharedDeckManager.DrawPileCount : 0;
        int discardCount = sharedDeckManager != null ? sharedDeckManager.DiscardPileCount : 0;

        summaryText.text = $"Hand {handCount} | Deck {drawCount} | Discard {discardCount}";
    }

    private void RefreshHandList()
    {
        if (cardItemRoot == null || cardItemPrefab == null || handController == null)
            return;

        for (int i = cardItemRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(cardItemRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < handController.Hand.Count; i++)
        {
            CardDefinition card = handController.Hand[i];
            CardItemTextUI item = Instantiate(cardItemPrefab, cardItemRoot);
            item.Bind(card, i, OnCardClicked, i == selectedIndex);
        }

        if (selectedIndex >= handController.Hand.Count)
            selectedIndex = -1;
    }

    private void OnCardClicked(int index)
    {
        selectedIndex = index;
        RefreshHandList();
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (detailPanel == null || handController == null)
            return;

        if (selectedIndex < 0 || selectedIndex >= handController.Hand.Count)
        {
            detailPanel.ShowEmpty();
            return;
        }

        detailPanel.ShowCard(handController.Hand[selectedIndex]);
    }

    private void RefreshUseButtonState()
    {
        if (useButton == null)
            return;

        bool canUse = true;

        if (playerPawn != null)
        {
            if (playerPawn.IsMoving || playerPawn.IsWaitingForBranchChoice)
                canUse = false;
        }

        useButton.interactable = canUse;
    }

    private void UseSelectedCard()
    {
        if (handController == null)
            return;

        if (selectedIndex < 0 || selectedIndex >= handController.Hand.Count)
        {
            Debug.LogWarning("[CardUI] 먼저 카드를 선택하세요.");
            return;
        }

        bool used = handController.TryUseCardByIndex(selectedIndex);

        if (used)
            selectedIndex = -1;

        RefreshAll();
    }
}