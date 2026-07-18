using UnityEngine;
using System.Collections;

public class SpawnerManager : MonoBehaviour
{
    public GameObject spawnerPrefab;
    public SpawnData data;
    public GameTimer timer; //  여기서 시간 가져옴

    private float spawnInterval;
    private float timerLocal = 0f;

    void Start()
    {
        spawnInterval = Random.Range(data.minSpawnInterval, data.maxSpawnInterval);
    }

    void Update()
    {
        if (timer.IsTimeOver) return; //  시간 끝나면 종료
        if (PlayerLifeManager.Instance != null && PlayerLifeManager.Instance.AllPlayersDead) return; // 모든 플레이어가 죽으면 종료

        timerLocal += Time.deltaTime;

        float gameTime = timer.maxTime - timer.RemainTime;

        float difficulty = gameTime * 0.1f;

        float minInterval = Mathf.Max(0.5f, data.minSpawnInterval - difficulty);
        float maxInterval = Mathf.Max(1.0f, data.maxSpawnInterval - difficulty);

        if (timerLocal >= spawnInterval)
        {
            timerLocal = 0f;

            spawnInterval = Random.Range(minInterval, maxInterval);

            int spawnCount = Random.Range(
                data.minspawnCount,
                data.maxSpawnCount + (int)(gameTime / 20f)
            );

            StartCoroutine(SpawnDelay(spawnCount));
        }
    }

    IEnumerator SpawnDelay(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (timer.IsTimeOver) yield break;
            if (PlayerLifeManager.Instance != null && PlayerLifeManager.Instance.AllPlayersDead) yield break;

            SpawnSpawner();
            yield return new WaitForSeconds(0.3f);
        }
    }

    void SpawnSpawner()
    {
        int maxTry = 10;

        for (int i = 0; i < maxTry; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = new Vector3(randomDir.x, 0, randomDir.y) * data.radius;
            spawnPos += transform.position;

            int layerMask = LayerMask.GetMask("Spawner");
            Collider[] cols = Physics.OverlapSphere(spawnPos, data.minDistance, layerMask);

            if (cols.Length == 0)
            {
                Instantiate(spawnerPrefab, spawnPos, Quaternion.identity);
                return;
            }
        }
    }
}