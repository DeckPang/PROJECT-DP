using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RPS : MonoBehaviour
{
    public float radius = 5.0f;

    private List<RPS_Player> players = new List<RPS_Player>();
    public RPS_Player currentPlayer; // 현재 턴 플레이어

    void Start()
    {
        GameObject[] playerObjs = GameObject.FindGameObjectsWithTag("Player");
        int player_count = playerObjs.Length;

        // 순서 생성
        List<int> orders = new List<int>();
        for (int i = 0; i < player_count; i++)
            orders.Add(i + 1);

        // 셔플
        for (int i = 0; i < orders.Count; i++)
        {
            int rand = Random.Range(i, orders.Count);
            int temp = orders[i];
            orders[i] = orders[rand];
            orders[rand] = temp;
        }

        // 플레이어 세팅
        for (int i = 0; i < playerObjs.Length; i++)
        {
            RPS_Player p = playerObjs[i].GetComponent<RPS_Player>();
            p.order = orders[i];

            float angle = (360f / player_count) * (p.order - 1);
            float rad = angle * Mathf.Deg2Rad;

            Vector3 pos = new Vector3(
                Mathf.Cos(rad) * radius,
                1,
                Mathf.Sin(rad) * radius
            );

            playerObjs[i].transform.position = pos;

            players.Add(p);
        }

        // 정렬 (큰 순서부터)
        players.Sort((a, b) => b.order.CompareTo(a.order));

        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        foreach (var player in players)
        {
            currentPlayer = player;

            Debug.Log(player.name + " 차례");

            yield return StartCoroutine(player.PlayTurn());
        }

        Debug.Log("모든 플레이어 선택 완료");
    }

    //  UI 버튼에서 호출
    public void OnClickRock()
    {
        currentPlayer.SetChoice(RPSChoice.Rock);
    }

    public void OnClickPaper()
    {
        currentPlayer.SetChoice(RPSChoice.Paper);
    }

    public void OnClickScissors()
    {
        currentPlayer.SetChoice(RPSChoice.Scissors);
    }
}