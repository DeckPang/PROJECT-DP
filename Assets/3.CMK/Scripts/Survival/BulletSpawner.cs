using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    public SpawnData data;
    public GameObject bullet;     

    [Header("생성 주기")]
    public float spawn_time = 0.0f;
    public float current_time = 1.5f;

    private int count = 0;

    void Update()
    {
        spawn_time += Time.deltaTime;

        //  중심 바라보기
        Vector3 center = Vector3.zero;
        Vector3 direction = center - transform.position;
        direction.y = 0.0f;

        // spawner의 시선처리
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        //  총알 생성 (Spawner 방향 그대로 사용)
        if (spawn_time >= data.bulletSpawnDelay && count == 0)
        {
            Instantiate(bullet, transform.position, transform.rotation);
            count++;
        }

        Destroy(gameObject, data.spawnerLifeTime);
    }
}