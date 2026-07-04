using UnityEngine;


[CreateAssetMenu(fileName = "BallPhysicsSettings", menuName = "BallData/BallPhysicsSettings")]
public class BallPhysicsSettings : ScriptableObject
{
    [Header("Launch")]
    public float force = 1.0f;
    public float maxDistance = 15.0f;

    [Header("Friction")]
    public float stop_p = 0.025f;
    public float friction = 0.05f;
}
