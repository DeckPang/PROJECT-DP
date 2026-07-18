using UnityEngine;

[CreateAssetMenu(menuName = "SurvivalData/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("Move")]
    public float speed = 2.0f;
    public int moveRange = 10;

    [Header("Look Rotate")]
    public float rotateSpeed = 10f;
    public float stopDistance = 0.5f;

    [Header("Combat")]
    public float knockbackForce = 30f;
    public float knockbackUpForce = 0.5f;
}
