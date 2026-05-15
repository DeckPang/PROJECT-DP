using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RPS : MonoBehaviour
{
    public float radius = 5.0f;
    public RPS_ScoreUI scoreUI;

    private List<RPS_Player> players = new List<RPS_Player>();
    private List<RPS_Player> allPlayers = new List<RPS_Player>(); //UI용
    public RPS_Player currentPlayer;

    public bool isInputEnabled = false;

    void Start()
    {
        SetupPlayers();

        scoreUI.UpdateUI(allPlayers);

        StartCoroutine(MainLoop());
    }

    void SetupPlayers()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Player");
        int count = objs.Length;

        List<int> orders = new List<int>();
        for (int i = 0; i < count; i++)
            orders.Add(i + 1);

        // 셔플
        for (int i = 0; i < orders.Count; i++)
        {
            int rand = Random.Range(i, orders.Count);
            (orders[i], orders[rand]) = (orders[rand], orders[i]);
        }

        for (int i = 0; i < objs.Length; i++)
        {
            RPS_Player p = objs[i].GetComponent<RPS_Player>();
            p.order = orders[i];

            float angle = (360f / count) * (p.order - 1);
            float rad = angle * Mathf.Deg2Rad;

            Vector3 pos = new Vector3(
                Mathf.Cos(rad) * radius,
                1,
                Mathf.Sin(rad) * radius
            );

            objs[i].transform.position = pos;

            players.Add(p);
            allPlayers.Add(p);
        }

        // order 기준 정렬 (큰 -> 작은)
        players.Sort((a, b) => b.order.CompareTo(a.order));
        
    }

    IEnumerator MainLoop()
    {
        while (players.Count > 1)
        {
            Debug.Log("=== 라운드 시작 ===");

            yield return StartCoroutine(GameLoop()); // 선택

            ResolveDuels(); // 전투 처리

            scoreUI.UpdateUI(allPlayers);

            yield return new WaitForSeconds(1f);
        }

        Debug.Log("우승자: " + players[0].name);
        players[0].AddScore(100);
        scoreUI.UpdateUI(allPlayers);
    }

    IEnumerator GameLoop()
    {
        foreach (var player in players)
        {
            currentPlayer = player;
            isInputEnabled = true;

            Debug.Log(player.name + " 차례");

            yield return StartCoroutine(player.PlayTurn());

            isInputEnabled = false;
        }

        Debug.Log("모든 선택 완료");
    }

    void ResolveDuels()
    {
        // 2명일 때 특수 처리
        if (players.Count == 2)
        {
            RPS_Player p1 = players[0];
            RPS_Player p2 = players[1];

            bool p1Win = IsWin(p1.choice, p2.choice);
            bool p2Win = IsWin(p2.choice, p1.choice);

            if (p1Win && !p2Win)
            {
                Debug.Log(p1.name + " -> " + p2.name + " 승리");
                p1.AddScore(1);
                players.Remove(p2);
                p2.gameObject.SetActive(false);
            }
            else if (p2Win && !p1Win)
            {
                Debug.Log(p2.name + " -> " + p1.name + " 승리");
                p2.AddScore(1);
                players.Remove(p1);
                p1.gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("무승부 → 다시 라운드");
                return; // 아무도 제거 안함
            }

            return; // 여기서 끝 (아래 코드 실행 안함)
        }

        //  3명 이상
        Dictionary<int, RPS_Player> map = new Dictionary<int, RPS_Player>();
        foreach (var p in players)
            map[p.order] = p;

        int maxOrder = 0;
        foreach (var key in map.Keys)
            if (key > maxOrder) maxOrder = key;

        List<RPS_Player> toRemove = new List<RPS_Player>();

        foreach (var attacker in players)
        {
            int targetOrder = (attacker.order == 1) ? maxOrder : attacker.order - 1;

            if (!map.ContainsKey(targetOrder))
                continue;

            RPS_Player target = map[targetOrder];

            if (toRemove.Contains(target))
                continue;

            if (IsWin(attacker.choice, target.choice))
            {
                Debug.Log(attacker.name + " -> " + target.name + " 승리");
                toRemove.Add(target);

            }
            else
            {
                Debug.Log(attacker.name + " -> " + target.name + " 실패");
            }
        }

        if (toRemove.Count == 0)
        {
            Debug.Log("아무도 탈락 안함 → 다시 라운드");
            return;
        }

        foreach (var p in toRemove)
        {
            players.Remove(p);
            p.gameObject.SetActive(false);
        }

        // 생존자 전원 +1점
        foreach (var survivor in players)
        {
            survivor.AddScore(1);
        }
    }

    bool IsWin(RPSChoice a, RPSChoice b)
    {
        if (a == RPSChoice.Rock && b == RPSChoice.Scissors) return true;
        if (a == RPSChoice.Scissors && b == RPSChoice.Paper) return true;
        if (a == RPSChoice.Paper && b == RPSChoice.Rock) return true;

        return false;
    }

    
}