using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallManager : MonoBehaviour
{
    [SerializeField]
    private RoundSettings round_set;

    [SerializeField] private Ball_UI_Score ui_Score;
    [SerializeField] private Ball_timer ball_timer;

    private List<Ball_Player> players = new List<Ball_Player>();
    private List<Ball_Player> allplayers = new List<Ball_Player>();

    private bool roundEnded = false;

    void Start()
    {
        SetupPlayers();
        ui_Score.BallUpdateUI(allplayers);
        StartCoroutine(ScoreLoop());
    }

    void SetupPlayers()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Player");
        int count = objs.Length;
        List<int> orders = new List<int>();
        for (int i = 0; i < count; i++)
            orders.Add(i);

        for (int i = 0; i < objs.Length; i++)
        {
            Ball_Player player = objs[i].GetComponent<Ball_Player>();
            player.order = orders[i];

            float angle = (360f / count) * player.order;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 pos = new Vector3(Mathf.Cos(rad) * round_set.radius, 1, Mathf.Sin(rad) * round_set.radius);
            objs[i].transform.position = pos;

            players.Add(player);
            allplayers.Add(player);
        }

        players.Sort((a, b) => a.order.CompareTo(b.order));
    }

    IEnumerator ScoreLoop()
    {
        while (!roundEnded)
        {
            yield return new WaitForSeconds(round_set.scoreTickInterval);

            // 타이머가 0이 아니고(60초 안 지남), 2명 이상 남아있을 때만 점수 지급
            if (!ball_timer.IsTimeUp && players.Count > 1)
            {
                foreach (var p in players)
                    p.AddScore(1);

                ui_Score.BallUpdateUI(allplayers);
            }

            // 1명만 남으면 즉시 승자 처리하고 라운드 종료
            if (players.Count == 1)
            {
                EndRound(players[0]);
            }
        }
    }

    // 탈락 판정이 일어나는 곳(맵 밖 추락 등)에서 이 함수를 호출
    public void EliminatePlayer(Ball_Player player)
    {
        if (roundEnded || !players.Contains(player)) return;

        players.Remove(player);
        Debug.Log(player.name + " 탈락");

        if (players.Count == 1)
            EndRound(players[0]);
    }

    void EndRound(Ball_Player winner)
    {
        if (roundEnded) return;
        roundEnded = true;

        winner.AddScore(round_set.winnerBonus);
        Debug.Log("우승자: " + winner.name + " (+100점)");
        ui_Score.BallUpdateUI(allplayers);
    }
}