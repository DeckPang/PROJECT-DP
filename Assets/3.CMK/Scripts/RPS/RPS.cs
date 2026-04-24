using System.Collections.Generic;
using UnityEngine;

public class RPS : MonoBehaviour
{
    void Start()
    {
        // 1. 플레이어 찾기
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        int player_count = players.Length;

        // 2. 순서 리스트 생성 (1 ~ N)
        List<int> orders = new List<int>();
        for (int i = 0; i < player_count; i++)
        {
            orders.Add(i + 1);
        }

        // 3. 셔플 (Fisher-Yates)
        for (int i = 0; i < orders.Count; i++)
        {
            int randomIndex = Random.Range(i, orders.Count);

            int temp = orders[i];
            orders[i] = orders[randomIndex];
            orders[randomIndex] = temp;
        }

        // 4. 플레이어에게 순서 부여 + 확인 출력
        for (int i = 0; i < players.Length; i++)
        {
            RPS_Player p = players[i].GetComponent<RPS_Player>();
            p.order = orders[i];

            Debug.Log(players[i].name + "의 순서: " + p.order);
        }
    }
}