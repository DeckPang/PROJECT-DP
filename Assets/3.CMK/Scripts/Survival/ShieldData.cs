using UnityEngine;

[CreateAssetMenu(menuName = "SurvivalData/ShieldData")]
public class ShieldData : ScriptableObject
{
    [Header("Shield Settings")]
    public float CoolDown = 3.5f;
    public float Duration = 2.0f; // 쉴드 지속 시간
}