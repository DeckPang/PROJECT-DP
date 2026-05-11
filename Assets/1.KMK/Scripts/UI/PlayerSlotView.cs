using TMPro;
using UnityEngine;

public class PlayerSlotView : MonoBehaviour
{
    [Header("뷰 컨테이너")]
    [SerializeField] private GameObject emptyView;       // EmptyView GameObject
    [SerializeField] private GameObject occupiedView;    // OccupiedView GameObject

    [Header("OccupiedView 내부")]
    [SerializeField] private TextMeshProUGUI nameText;   // 점유 시 닉네임
    [SerializeField] private GameObject readyBadge;      // 체크 아이콘
    [SerializeField] private GameObject hostBadge;       // 왕관 아이콘
    [SerializeField] private GameObject meIndicator;     // "나" 표시 (테두리, 뱃지 등)

    public void SetOccupied(string playerName, bool isReady, bool isHost, bool isMe)
    {
        if (emptyView != null) emptyView.SetActive(false);
        if (occupiedView != null) occupiedView.SetActive(true);

        if (nameText != null) nameText.text = playerName;
        if (readyBadge != null) readyBadge.SetActive(isReady);
        if (hostBadge != null) hostBadge.SetActive(isHost);
        if (meIndicator != null) meIndicator.SetActive(isMe);
    }

    public void SetEmpty()
    {
        if (emptyView != null) emptyView.SetActive(true);
        if (occupiedView != null) occupiedView.SetActive(false);
        if (meIndicator != null) meIndicator.SetActive(false);
    }
}