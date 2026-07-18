using UnityEngine;

[CreateAssetMenu(menuName = "SurvivalData/SpawnerData")]
public class SpawnData : ScriptableObject
{
    [Header("SpawnerManager")]
    public float minSpawnInterval = 2.5f;
    public float maxSpawnInterval = 4.5f;

    public int minspawnCount = 1;
    public int maxSpawnCount = 5;

    public float radius = 6.0f; //  반지름
    public float minDistance = 2.0f; // 생성되었을 때 2.0f 거리만큼에 오브젝트가 있는지

    [Header("BulletSpawner")]
    public float spawnerLifeTime = 2.0f;
    public float bulletSpawnDelay = 1.5f;

}
