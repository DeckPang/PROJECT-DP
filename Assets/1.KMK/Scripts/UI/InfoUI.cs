using TMPro;
using UnityEngine;

public class InfoUI : MonoBehaviour
{
    public TextMeshProUGUI infoText;

    public void UpdateInfoText(string content)
    {
        infoText.text = content;
    }
}
