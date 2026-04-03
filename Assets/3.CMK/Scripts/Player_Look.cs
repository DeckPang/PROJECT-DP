using UnityEngine;

public class Player_Look : MonoBehaviour
{
    [Header("Look Rotate")]
    public float rotateSpeed = 10f;   // 회전 속도
    public float stopDistance = 0.5f; // 너무 가까우면 회전 멈춤

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 target = hit.point;

            Vector3 direction = target - transform.position;
            direction.y = 0f;

            //  1. 거리 너무 가까우면 회전 안 함 (튐 방지)
            if (direction.sqrMagnitude < stopDistance * stopDistance)
                return;

            //  2. 목표 회전 만들기
            Quaternion targetRot = Quaternion.LookRotation(direction);

            //  3. 부드럽게 회전
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
        }
    }
}