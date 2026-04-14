using UnityEngine;

[CreateAssetMenu(menuName = "SurvivalData/ShieldData")]
public class ShieldData : ScriptableObject
{
    [Header("Shield CoolTime")]
    public float CoolDown = 3.5f;
    public float SkillTime;
}
