using UnityEngine;


[CreateAssetMenu(fileName = "RoundSettings", menuName = "BallData/RoundSettings")]
public class RoundSettings : ScriptableObject
{
    public float radius = 5.0f;
    public int winnerBonus = 100;
    public float scoreTickInterval = 1f;
    public float time = 60.0f;
}
