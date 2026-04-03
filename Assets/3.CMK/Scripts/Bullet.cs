using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 생성된 순간 앞으로 발사
        rb.AddForce(transform.forward * speed, ForceMode.Impulse);

        Destroy(gameObject, 2.5f);
    }
}