using System.Collections.Generic;
using UnityEngine;

public class BoardNode : MonoBehaviour
{
    [Header("Node Info")]
    [SerializeField] private int nodeId;
    [SerializeField] private BoardNodeType nodeType = BoardNodeType.Blue;

    [Header("Connections")]
    [SerializeField] private List<BoardNode> nextNodes = new List<BoardNode>();

    [Header("Trap")]
    [SerializeField] private TrapType installedTrap = TrapType.None;

    public int NodeId => nodeId;
    public BoardNodeType NodeType => nodeType;
    public List<BoardNode> NextNodes => nextNodes;
    public TrapType InstalledTrap => installedTrap;

    public void SetNodeId(int id)
    {
        nodeId = id;
    }

    public void InstallTrap(TrapType trapType, string installerName)
    {
        installedTrap = trapType;
        Debug.Log($"[Node {nodeId}] {installerName} 이(가) {trapType} 설치");
    }

    public void ClearTrap()
    {
        installedTrap = TrapType.None;
        Debug.Log($"[Node {nodeId}] 설치된 함정 제거");
    }

    /// <summary>
    /// 플레이어가 이 노드에 도착했을 때 호출.
    /// 나중에 강제이동/일반이동 분리할 수 있게 triggerTileEffect 플래그 추가.
    /// </summary>
    public void OnPlayerArrived(PlayerPawn pawn, bool triggerTileEffect = true)
    {
        if (pawn == null)
            return;

        Debug.Log($"[Node {nodeId}] {pawn.PawnName} 도착 | Type = {nodeType}");

        // 설치형 함정은 우선 발동
        ResolveTrap(pawn);

        if (!triggerTileEffect)
            return;

        switch (nodeType)
        {
            case BoardNodeType.Start:
                HandleStart(pawn);
                break;

            case BoardNodeType.Blue:
                HandleBlue(pawn);
                break;

            case BoardNodeType.Red:
                HandleRed(pawn);
                break;

            case BoardNodeType.CardDraw:
                HandleCardDraw(pawn);
                break;

            case BoardNodeType.Battle:
                HandleBattle(pawn);
                break;

            case BoardNodeType.Trap:
                HandleTrapNode(pawn);
                break;

            case BoardNodeType.Event:
                HandleEvent(pawn);
                break;

            case BoardNodeType.Shop:
                HandleShop(pawn);
                break;

            case BoardNodeType.RedCard:
                HandleRedCard(pawn);
                break;

            case BoardNodeType.Roulette:
                HandleRoulette(pawn);
                break;

            case BoardNodeType.Branch:
                HandleBranch(pawn);
                break;

            case BoardNodeType.Heal:
                HandleHeal(pawn);
                break;

            case BoardNodeType.LunchBox:
                HandleLunchBox(pawn);
                break;

            case BoardNodeType.Rest:
                HandleRest(pawn);
                break;
        }
    }

    private void HandleStart(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] START 도착/통과. 추후 최대 마나 증가 연결 위치");
    }

    private void HandleBlue(PlayerPawn pawn)
    {
        ApplyCoinChange(pawn, +5, "Blue");
    }

    private void HandleRed(PlayerPawn pawn)
    {
        ApplyCoinChange(pawn, -5, "Red");
    }

    private void HandleCardDraw(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] CardDraw 도착 - 추후 공용 덱 1장 드로우 연결 위치");
    }

    private void HandleBattle(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] Battle 도착 - 추후 전투/공격 처리 연결 위치");
    }

    private void HandleTrapNode(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] Trap 칸 도착 - 추후 맵 패널티 처리 연결 위치");
    }

    private void HandleEvent(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] Event 도착 - 추후 랜덤 이벤트 처리 연결 위치");
    }

    private void HandleShop(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] Shop 도착 - 추후 상점 UI/구매 처리 연결 위치");
    }

    private void HandleRedCard(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] RedCard 도착 - 다음 턴 전체 스킵 연결 필요");
    }

    private void HandleRoulette(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] Roulette 도착 - 추후 룰렛 처리 연결 위치");
    }

    private void HandleBranch(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] Branch 도착 - nextNodes 2개 이상이면 분기 처리");
    }

    private void HandleHeal(PlayerPawn pawn)
    {
        ApplyHpChange(pawn, +1, "Heal");
    }

    private void HandleLunchBox(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] LunchBox 도착 - 추후 고밸류 카드 1장 획득 연결 위치");
    }

    private void HandleRest(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] Rest 도착 - 다음 턴 행동 제한 연결 필요");
    }

    private void ApplyCoinChange(PlayerPawn pawn, int amount, string source)
    {
        if (pawn == null)
            return;

        var info = pawn.CharacterInfo;
        if (info == null)
        {
            Debug.LogWarning($"[Node {nodeId}] {source} 처리 실패 - CharacterInfo가 없습니다.");
            return;
        }

        info.ChangeCoins(amount);
        Debug.Log($"[Node {nodeId}] {source} 처리 -> 코인 {(amount >= 0 ? "+" : "")}{amount} | 현재 코인: {info.Coins}");
    }

    private void ApplyHpChange(PlayerPawn pawn, int amount, string source)
    {
        if (pawn == null)
            return;

        var info = pawn.CharacterInfo;
        if (info == null)
        {
            Debug.LogWarning($"[Node {nodeId}] {source} 처리 실패 - CharacterInfo가 없습니다.");
            return;
        }

        info.ChangeHp(amount);
        Debug.Log($"[Node {nodeId}] {source} 처리 -> HP {(amount >= 0 ? "+" : "")}{amount} | 현재 HP: {info.Hp}");
    }

    private void ResolveTrap(PlayerPawn pawn)
    {
        if (installedTrap == TrapType.None)
            return;

        switch (installedTrap)
        {
            case TrapType.BananaPeel:
                Debug.Log($"[Trap] {pawn.PawnName} 이(가) 바나나 껍질 발동. 추후 3~4칸 후퇴 처리 연결 예정");
                break;

            case TrapType.FakeTrophy:
                Debug.Log($"[Trap] {pawn.PawnName} 이(가) 가짜 트로피 발동.");
                ApplyCoinChange(pawn, -5, "FakeTrophy Trap");
                break;
        }

        installedTrap = TrapType.None;
        Debug.Log($"[Node {nodeId}] 함정 1회 발동 후 제거");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.2f, 0.15f);

        if (nextNodes == null)
            return;

        Gizmos.color = Color.cyan;
        foreach (BoardNode next in nextNodes)
        {
            if (next == null) continue;
            Gizmos.DrawLine(transform.position, next.transform.position);
        }
    }
}