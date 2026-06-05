using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallManager : MonoBehaviour
{
    public float radius = 5.0f;
    private List<Ball_Player> players = new List<Ball_Player>();

    void Start()
    {
        SetupPlayers();
    }

    void Update()
    {
        
    }

    void SetupPlayers() // 위치 세팅
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Player"); // 플레이어의 태그를 가진 오브젝트 찾기
        int count = objs.Length; //찾은 오브젝트 수

        List<int> orders = new List<int>(); // 빈 리스트
        for (int i = 0; i < count; i++) // 빈 리스트에 찾은 오브젝트 수만큼 넣기
            orders.Add(i);

        for (int i = 0; i < objs.Length; i++)
        {
            Ball_Player player = objs[i].GetComponent<Ball_Player>();
            player.order = orders[i]; //각각의 플레이어에게 순서를 부여

            // 순서에 따른 플레이어들의 위치 배정
            float angle = (360f / count) * (player.order - 1); 
            float rad = angle * Mathf.Deg2Rad;

            Vector3 pos = new Vector3(
                Mathf.Cos(rad) * radius,
                1,
                Mathf.Sin(rad) * radius
            );

            objs[i].transform.position = pos; // 위치 확정

            players.Add(player); // 리스트에 순서대로
        }
        players.Sort((a, b) => a.order.CompareTo(b.order)); // 뒤죽박죽인 순서를 정렬

    }
}
