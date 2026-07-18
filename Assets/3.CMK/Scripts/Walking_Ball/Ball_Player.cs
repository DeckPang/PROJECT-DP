using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.Build;
using UnityEditorInternal;
using UnityEngine;

public class Ball_Player : MonoBehaviour
{
    [Header("Player Force")]
    [SerializeField]
    private BallPhysicsSettings settings;

    private float distance;
    public int order;
    public int score;

    private Renderer rend;

    public Material outlineMaterial;
    private Material originalMaterial;

    bool isDragging;

    private Vector3 EndPoint;
    private BallManager ballManager;

    Rigidbody rb;



    void Start()
    {
        rend = GetComponent<Renderer>();
        originalMaterial = rend.material;

        rb = GetComponent<Rigidbody>();
        isDragging = false;

        ballManager = FindFirstObjectByType<BallManager>();
    }

    void FixedUpdate()
    {
        ApplyFriction();
    }

    void ApplyFriction()
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(vel.x, 0, vel.z);
        float speed = horizontalVel.magnitude;
        if (speed > 0.0f)
        {
            if (speed < settings.stop_p)
            {
                rb.linearVelocity = new Vector3(0f, vel.y, 0f);
            }
            else
            {
                float decel = settings.friction * Mathf.Abs(Physics.gravity.y);
                Vector3 FrictionForce = -horizontalVel * decel * rb.mass;
                rb.AddForce(FrictionForce);
            }
        }
    }
    public void AddScore(int amout)
    {
        score += amout;
    }

    void Update()
    {
        DragBall();
        
    }

    void DragBall()
    {

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        RaycastHit hit;


        if (Input.GetMouseButtonDown(0)) // 홀드 중일 때
        {
            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == this.gameObject)
            {
                isDragging = true;
                rend.material = outlineMaterial;
            }
        }

        if (!isDragging) // 홀드 중이 아닐 때
        {
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == this.gameObject)
                {
                    rend.material = outlineMaterial;
                }
                else
                {
                    rend.material = originalMaterial;
                }
            }
            else
            {
                rend.material = originalMaterial;
            }
        }


        if (isDragging)
        {
            if (Input.GetMouseButton(0)) // 마우스의 위치를 실시간으로 저장
            {
                if (groundPlane.Raycast(ray, out distance))
                {
                    // 실제 바닥 위치 얻기
                    EndPoint = ray.GetPoint(distance);

                    // 높이 고정
                    EndPoint.y = transform.position.y;
                }

            }

            if (Input.GetMouseButtonUp(0)) // 발사
            {
                Vector3 dir = transform.position - EndPoint;

                dir.y = 0.0f;

                dir = Vector3.ClampMagnitude(dir, settings.maxDistance);

                rb.linearVelocity = Vector3.zero;

                rb.AddForce(dir * settings.force, ForceMode.Impulse);

                isDragging = false;

                rend.material = originalMaterial;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathZone"))
        {
            ballManager.EliminatePlayer(this);
            gameObject.SetActive(false);
        }
            
    }
}
