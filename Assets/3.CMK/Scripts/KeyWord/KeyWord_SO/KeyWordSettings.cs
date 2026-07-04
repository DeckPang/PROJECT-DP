using UnityEngine;


[CreateAssetMenu(fileName = "KeyWordSettings", menuName = "KeyWordSet/KeyWordSettings")]
public class KeyWordSettings : ScriptableObject
{
    [Header("Player")]
    public float moveDistance = 1.0f; //Player의 이동거리

    [Header("UI")]
    public float Spacing = 100.0f;      //UI 간격

    [Header("Setting")]
    public int StartCount = 4;
    public int TotalCount = 20;
}
