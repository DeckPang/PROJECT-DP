using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 4.5f;
    private Rigidbody rb;

    void Start()
    {

        Physics.IgnoreLayerCollision( LayerMask.NameToLayer("Bullet"),LayerMask.NameToLayer("Bullet"),true); // Bullet끼리는 통과되게 함

        rb = GetComponent<Rigidbody>();

        rb.linearVelocity = transform.forward * speed;

        Destroy(gameObject, 2.5f);
    }


    public void Reflect(Vector3 dir)
    {
        rb.linearVelocity = dir.normalized * speed;
    }
}