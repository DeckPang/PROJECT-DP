using UnityEngine;

public class Player_Move : MonoBehaviour
{
    [Header("Move")]
    public float speed = 2.0f;
    
    private Rigidbody rb;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = (transform.forward * v + transform.right * h);

        rb.MovePosition(rb.position + move * speed * Time.deltaTime);
    }
}
