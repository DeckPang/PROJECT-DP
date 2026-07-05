using System;
using System.Collections.Generic;
using UnityEngine;

public class H_A_S_Player : MonoBehaviour
{
    [SerializeField]
    private float speed = 10.0f;         // Player 기본 이동속도
    private float baggage = 1.5f;        // 짐을 들었을 때 이동속도 감소량
    private bool isMoving = false;       // 게임 시작할 때 텀을 주고 시작하기 위해서와 게임이 끝났을 때 이동하지 못하게 하기 위함
    private Vector3 moveDir;
    private Dictionary<KeyCode, Vector3> Keyvectors; //Dictionary를 이용한 Key 관리 (GetKey)
    private Dictionary<KeyCode, Action> KeyActions;
    private Rigidbody rb;

    private void Awake()
    {
        Keyvectors = new Dictionary<KeyCode, Vector3>() //Player가 사용할 Key
        {
            { KeyCode.LeftArrow, Vector3.left },
            { KeyCode.RightArrow, Vector3.right },
            { KeyCode.UpArrow, Vector3.forward },
            { KeyCode.DownArrow, Vector3.back },


        };

        KeyActions = new Dictionary<KeyCode, Action>()
        { 
            {KeyCode.F,  interaction},
        };

    }

    void Start()
    {
        isMoving = true;
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {
            moveDir = Vector3.zero;

            foreach (var entry in Keyvectors)
            {
                if (Input.GetKey(entry.Key))
                {
                    moveDir += entry.Value;
                }
            }

            foreach (var entry in KeyActions)
            {
                if (Input.GetKeyDown(entry.Key))
                {
                    entry.Value.Invoke();
                }
            }

        }
        
    }

    void FixedUpdate()
    {
        // 실제 이동은 FixedUpdate에서 (물리 연산이니까)
        if (isMoving)
        {
            Vector3 pos = transform.position + moveDir * speed * Time.fixedDeltaTime;
            rb.MovePosition(pos);
        }
    }

    void interaction() // 짐 옮기는 상호작용 함수
    {

    }
}

