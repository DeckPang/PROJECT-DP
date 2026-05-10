using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 슬롯 1칸의 UI를 담당합니다. 표시 전용(View).
/// </summary>
public class PlayerSlotView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject      readyBadge;     // Ready 표시 (체크 아이콘 등)
    [SerializeField] private GameObject      hostBadge;      // 호스트 왕관 등
    [SerializeField] private GameObject      emptyOverlay;   // "비어있음" 표시

    public void SetOccupied(string playerName, bool isReady, bool isHost)
    {
        if (nameText      != null) nameText.text = playerName;
        if (readyBadge    != null) readyBadge.SetActive(isReady);
        if (hostBadge     != null) hostBadge.SetActive(isHost);
        if (emptyOverlay  != null) emptyOverlay.SetActive(false);
    }

    public void SetEmpty()
    {
        if (nameText      != null) nameText.text = "비어있음";
        if (readyBadge    != null) readyBadge.SetActive(false);
        if (hostBadge     != null) hostBadge.SetActive(false);
        if (emptyOverlay  != null) emptyOverlay.SetActive(true);
    }
}
