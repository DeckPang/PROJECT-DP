using UnityEngine;

public class ShieldSkill : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Bullet"))
        {
            Bullet bullet = col.GetComponent<Bullet>();

            if (bullet != null)
            {
                //  마우스 위치  Ray 생성
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                RaycastHit hit;
                Vector3 targetPoint;

                //  레이 맞은 지점 구하기
                if (Physics.Raycast(ray, out hit))
                {
                    targetPoint = hit.point;
                }
                else
                {
                    // 아무것도 안 맞으면 멀리 쏘기
                    targetPoint = ray.origin + ray.direction * 100f;
                }

                //  방향 계산 (쉴드  마우스 위치)
                Vector3 dir = (targetPoint - transform.position).normalized;

                bullet.Reflect(dir);
            }
        }
    }
}
