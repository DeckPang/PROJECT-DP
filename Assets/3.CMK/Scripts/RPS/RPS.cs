using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RPS : MonoBehaviour
{
    public float radius = 5.0f;
    public RPS_ScoreUI scoreUI;
    public RPS_LogUi logUI;

    public float roundStartDelay = 2f;   // 라운드 시작 후 대기
    public float attackDelay = 60f;       // 공격 하나당 텀

    private List<RPS_Player> players = new List<RPS_Player>();
    private List<RPS_Input> allInputs = new List<RPS_Input>();
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
        RPS_Input[] inputs = FindObjectsOfType<RPS_Input>();

        int count = objs.Length;

        Debug.Log($"찾은 RPS_Input 개수: {inputs.Length}");
        foreach (var inp in inputs)
        {
            Debug.Log($"RPS_Input 오브젝트 이름: {inp.gameObject.name}");
        }

        List<int> orders = new List<int>();
        for (int i = 0; i < count; i++)
            orders.Add(i + 1);

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

            objs[i].transform.position = new Vector3(
                Mathf.Cos(rad) * radius, 1, Mathf.Sin(rad) * radius
            );

            players.Add(p);
            allPlayers.Add(p);

            // 이름으로 안전하게 매칭 (예: "Player1" <-> "Input_Player1")
            RPS_Input matchedInput = System.Array.Find(inputs, inp => inp.name.Contains(objs[i].name));

            if (matchedInput == null)
            {
                Debug.LogWarning($"{objs[i].name}에 매칭되는 RPS_Input을 찾지 못했습니다.");
                continue;
            }

            matchedInput.owner = p;
            matchedInput.rps = this;
            allInputs.Add(matchedInput);
        }

        players.Sort((a, b) => b.order.CompareTo(a.order));
    }

    IEnumerator MainLoop()
    {
        while (players.Count > 1)
        {
            logUI.Log("=== Round Start ===");
            logUI.ClearAfterDelay(roundStartDelay + attackDelay * players.Count);

            yield return new WaitForSeconds(roundStartDelay); // 라운드 시작 후 대기

            yield return StartCoroutine(GameLoop()); // 선택

            yield return StartCoroutine(ResolveDuelsRoutine()); // 전투 처리

            scoreUI.UpdateUI(allPlayers);

            yield return new WaitForSeconds(1f);
        }

        logUI.Log("Winner: " + players[0].name);
        logUI.ClearAfterDelay(2f);
        players[0].AddScore(100);
        scoreUI.UpdateUI(allPlayers);
    }

    IEnumerator GameLoop()
    {
        foreach (var player in players)
        {
            currentPlayer = player;
            isInputEnabled = true;

            foreach (var input in allInputs)
            {
                input.SetInteractable(input.owner == currentPlayer);
            }

            logUI.Log(player.name + "'s turn");
            logUI.ClearAfterDelay(2f);

            yield return StartCoroutine(player.PlayTurn());

            isInputEnabled = false;
        }

        

        logUI.Log("All choices complete");
        logUI.ClearAfterDelay(2f);
    }

    // ResolveDuels를 코루틴으로 변경 → 공격 하나 처리할 때마다 대기
    IEnumerator ResolveDuelsRoutine()
    {
        // 2명일 때 특수 처리
        if (players.Count == 2)
        {
            RPS_Player p1 = players[0];
            RPS_Player p2 = players[1];

            bool p1Win = IsWin(p1.choice, p2.choice);
            bool p2Win = IsWin(p2.choice, p1.choice);

            logUI.Log(p1.name + " -> " + p2.name + " attacks");
            logUI.ClearAfterDelay(attackDelay);
            yield return new WaitForSeconds(attackDelay);

            if (p1Win && !p2Win)
            {
                logUI.Log(p1.name + " -> " + p2.name + " wins");
                logUI.ClearAfterDelay(attackDelay);
                p1.AddScore(1);
                players.Remove(p2);
                p2.gameObject.SetActive(false);
            }
            else if (p2Win && !p1Win)
            {
                logUI.Log(p2.name + " -> " + p1.name + " wins");
                logUI.ClearAfterDelay(attackDelay);
                p2.AddScore(1);
                players.Remove(p1);
                p1.gameObject.SetActive(false);
            }
            else
            {
                logUI.Log("Draw -> round repeats");
                logUI.ClearAfterDelay(attackDelay);
            }

            yield break; // 여기서 끝 (아래 코드 실행 안함)
        }

        // 3명 이상
        Dictionary<int, RPS_Player> map = new Dictionary<int, RPS_Player>();
        foreach (var p in players)
            map[p.order] = p;

        int maxOrder = 0;
        foreach (var key in map.Keys)
            if (key > maxOrder) maxOrder = key;

        List<RPS_Player> toRemove = new List<RPS_Player>();

        // players 리스트를 복사해서 순회 (foreach 도중 순서 꼬임 방지)
        List<RPS_Player> attackers = new List<RPS_Player>(players);

        foreach (var attacker in attackers)
        {
            int targetOrder = (attacker.order == 1) ? maxOrder : attacker.order - 1;

            if (!map.ContainsKey(targetOrder))
                continue;

            RPS_Player target = map[targetOrder];

            if (toRemove.Contains(target))
                continue;

            logUI.Log(attacker.name + " -> " + target.name + " attacks");
            logUI.ClearAfterDelay(attackDelay);
            yield return new WaitForSeconds(attackDelay); // 공격 하나 처리 후 대기

            if (IsWin(attacker.choice, target.choice))
            {
                logUI.Log(attacker.name + " -> " + target.name + " wins");
                logUI.ClearAfterDelay(attackDelay);
                toRemove.Add(target);
            }
            else
            {
                logUI.Log(attacker.name + " -> " + target.name + " fails");
                logUI.ClearAfterDelay(attackDelay);
            }

            yield return new WaitForSeconds(attackDelay);
        }

        if (toRemove.Count == 0)
        {
            logUI.Log("No one eliminated -> round repeats");
            logUI.ClearAfterDelay(attackDelay);
            yield break;
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