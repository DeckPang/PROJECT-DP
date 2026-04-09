using System.Collections;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    public GameObject spawnerPrefab;
    private float spawnInterval;

    public float radius = 6.0f; //  반지름
    public float minDistance = 2.0f; // 생성되었을 때 2.0f 거리만큼에 오브젝트가 있는지
    private int SpawnCount;     // BulletSpawner 개수

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        spawnInterval = Random.Range(2.5f, 4.5f);  // 2.5 ~ 4.5초 사이에 랜덤한 값 지정
        SpawnCount = Random.Range(1, 5);           // 1 ~ 5개 중 랜덤한 값 지정

        if (timer >= spawnInterval) // timer가 랜덤한 값보다 크거나 같으면 코루틴 시작
        {
            timer = 0f;

            StartCoroutine(SpawnDelay());
            
        }
    }

    IEnumerator SpawnDelay() // spawner 생성 
    {
        for (int i = 0; i < SpawnCount; i++) // 랜덤한 spawnCount만큼 Spawner 생성
        {
            SpawnSpawner();

            yield return new WaitForSeconds(0.3f); // 생성할 때 텀을 주어 생성
        }
    }
    void SpawnSpawner() // spawner
    {
        int maxTry = 10; // 무한루프 방지

        for (int i = 0; i < maxTry; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;               // 원 안에 랜덤 방향 정하기
            Vector3 spawnPos = new Vector3(randomDir.x, 0, randomDir.y) * radius; // 방향 * 반지름 원의 둘레 위치
            spawnPos += transform.position;   // Spawner를 기준으로 이동

            int layerMask = LayerMask.GetMask("Spawner");

            Collider[] cols = Physics.OverlapSphere(spawnPos, minDistance, layerMask); // Spawner layer를 가진 오브젝트들을 찾기

            if (cols.Length == 0) // 오브젝트를 찾고 겹치는 오브젝트가 없다면 생성
            {
                Instantiate(spawnerPrefab, spawnPos, Quaternion.identity);
                return;
            }
        }
    }
}