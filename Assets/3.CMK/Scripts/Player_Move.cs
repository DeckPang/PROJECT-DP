using UnityEngine;

public class Player_Move : MonoBehaviour
{
    [Header("Move")]
    public float speed = 2.0f;
    public int moveRange = 10;

    [Header("Look Rotate")]
    public float rotateSpeed = 10f;
    public float stopDistance = 0.5f;

    private bool gameOver;
    private Rigidbody rb;

    void Start()
    {
        gameOver = false;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (gameOver != true)
        {
            Move();
            Look();
        }
        
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Bullet"))
        {
            gameOver = true;
            Vector3 dir = transform.position - collision.transform.position;
            dir = dir.normalized;

            dir.y = 0.5f;
            float force = 30f;
            rb.AddForce(dir * force, ForceMode.Impulse);
        }
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 pos = transform.position; // 현재 위치

        if (pos.magnitude > moveRange) // 원점으로부터 moveRange를 넘었을 때
        {
            transform.position = pos.normalized * moveRange;
        }


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