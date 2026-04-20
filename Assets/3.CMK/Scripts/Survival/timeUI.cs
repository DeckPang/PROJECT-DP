using UnityEngine;
using TMPro;

public class timeUI : MonoBehaviour
{
    public GameTimer timer;
    public TMP_Text text;

    void Update()
    {
        text.text = timer.RemainTime.ToString("F1");
    }
}