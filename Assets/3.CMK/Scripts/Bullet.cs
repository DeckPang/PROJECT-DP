using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.linearVelocity = transform.forward * speed;

        Destroy(gameObject, 2.5f);
    }

    public void Reflect(Vector3 dir)
    {
        rb.linearVelocity = dir.normalized * speed;
    }
}