using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

public class Ball_Player : MonoBehaviour
{
    [Header("Player Force")]
    [SerializeField]
    private float force = 1.0f;
    private float power;
    private float distance;

    private Renderer rend;

    public Material outlineMaterial;
    private Material originalMaterial;

    bool isDragging;

    private Vector3 EndPoint;

    Rigidbody rb;



    void Start()
    {
        rend = GetComponent<Renderer>();
        originalMaterial = rend.material;

        rb = GetComponent<Rigidbody>();
        isDragging = false;
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        RaycastHit hit;


        if (Input.GetMouseButtonDown(0)) // 홀드 중일 때
        {

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    isDragging = true;
                }

            }
            rend.material = outlineMaterial;
        }

        if (!isDragging) // 홀드 중이 아닐 때
        {
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Player"))
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

                rb.linearVelocity = Vector3.zero;

                rb.AddForce(dir * force, ForceMode.Impulse);

                isDragging = false;

                rend.material = originalMaterial;
            }
        }

        
    }
}
