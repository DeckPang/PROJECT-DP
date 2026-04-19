using TMPro;
using UnityEngine;

/// <summary>
/// 로비의 플레이어 슬롯 하나를 담당합니다.
/// </summary>
public class PlayerSlotUI : MonoBehaviour
{
    [Header("빈 슬롯")]
    [SerializeField] private GameObject emptyView;

    [Header("점유된 슬롯")]
    [SerializeField] private GameObject occupiedView;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject hostBadge;   // "방장" 표시
    [SerializeField] private GameObject readyBadge;  // "준비됨" 표시

    public void SetEmpty()
    {
        emptyView.SetActive(true);
        occupiedView.SetActive(false);
    }

    public void SetOccupied(string playerName, bool isHost)
    {
        emptyView.SetActive(false);
        occupiedView.SetActive(true);
        playerNameText.text = playerName;
        hostBadge.SetActive(isHost);
        readyBadge.SetActive(isHost); // 방장은 항상 준비됨 표시
    }

    public void SetReady(bool isReady)
    {
        if (occupiedView.activeSelf)
            readyBadge.SetActive(isReady);
    }
}
