using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    public GameObject spawnerPrefab;
    public float spawnInterval = 3f;

    public float radius = 6f; //  반지름

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnSpawner();
        }
    }

    void SpawnSpawner()
    {
        //  랜덤 방향 (XZ 평면)
        Vector2 randomDir = Random.insideUnitCircle.normalized;

        //  반지름 6 위치
        Vector3 spawnPos = new Vector3(randomDir.x, 0, randomDir.y) * radius;

        //  중심 기준으로 이동
        spawnPos += transform.position;

        Instantiate(spawnerPrefab, spawnPos, Quaternion.identity);
    }
}