using UnityEngine;

public class Player_Move : MonoBehaviour
{
    [Header("Move")]
    public float speed = 2.0f;

    [Header("Look Rotate")]
    public float rotateSpeed = 10f;
    public float stopDistance = 0.5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Move();
        Look();
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 월드 좌표 기준 이동
        Vector3 move = new Vector3(h, 0, v);

        rb.MovePosition(rb.position + move * speed * Time.deltaTime);
    }

    void Look()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 target = hit.point;

            Vector3 direction = target - transform.position;
            direction.y = 0f;

            // 너무 가까우면 회전 안함
            if (direction.sqrMagnitude < stopDistance * stopDistance)
                return;

            Quaternion targetRot = Quaternion.LookRotation(direction);

            // 부드럽게 회전
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
        }
    }
}