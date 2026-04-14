using UnityEngine;

public class Player_Move : MonoBehaviour
{
    public PlayerData data;
   
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

            // 위쪽 힘 적용
            dir.y = data.knockbackUpForce;

            // 데이터에서 값 가져오기
            rb.AddForce(dir * data.knockbackForce, ForceMode.Impulse);
        }
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 pos = transform.position; // 현재 위치

        if (pos.magnitude > data.moveRange) // 원점으로부터 moveRange를 넘었을 때
        {
            transform.position = pos.normalized * data.moveRange;
        }


        // 월드 좌표 기준 이동
        Vector3 move = new Vector3(h, 0, v);

        rb.MovePosition(rb.position + move * data.speed * Time.deltaTime);
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
            if (direction.sqrMagnitude < data.stopDistance * data.stopDistance)
                return;

            Quaternion targetRot = Quaternion.LookRotation(direction);

            // 부드럽게 회전
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRot,
                data.rotateSpeed * Time.deltaTime
            );
        }
    }
}