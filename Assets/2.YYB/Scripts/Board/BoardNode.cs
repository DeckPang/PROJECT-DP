using System.Collections.Generic;
using UnityEngine;

public class BoardNode : MonoBehaviour
{
    [Header("Node Info")]
    [SerializeField] private int nodeId;
    [SerializeField] private BoardNodeType nodeType = BoardNodeType.Resource;

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

    public void OnPlayerArrived(PlayerPawn pawn)
    {
        Debug.Log($"[Node {nodeId}] {pawn.PawnName} 도착 | Type = {nodeType}");

        LogNodeEffect(pawn);
        ResolveTrap(pawn);
    }

    private void LogNodeEffect(PlayerPawn pawn)
    {
        switch (nodeType)
        {
            case BoardNodeType.Start:
                Debug.Log($"[Node {nodeId}] Start 칸 도착/통과 처리 위치");
                break;
            case BoardNodeType.Resource:
                Debug.Log($"[Node {nodeId}] 재화 획득/손실 처리 위치");
                break;
            case BoardNodeType.CardDraw:
                Debug.Log($"[Node {nodeId}] 카드 드로우 처리 위치");
                break;
            case BoardNodeType.Battle:
                Debug.Log($"[Node {nodeId}] 전투/공격 처리 위치");
                break;
            case BoardNodeType.Trap:
                Debug.Log($"[Node {nodeId}] 함정 칸 처리 위치");
                break;
            case BoardNodeType.Event:
                Debug.Log($"[Node {nodeId}] 랜덤 이벤트 처리 위치");
                break;
            case BoardNodeType.Shop:
                Debug.Log($"[Node {nodeId}] 상점 처리 위치");
                break;
            case BoardNodeType.Jail:
                Debug.Log($"[Node {nodeId}] 감옥 처리 위치");
                break;
            case BoardNodeType.Roulette:
                Debug.Log($"[Node {nodeId}] 룰렛 처리 위치");
                break;
        }
    }

    private void ResolveTrap(PlayerPawn pawn)
    {
        if (installedTrap == TrapType.None)
            return;

        switch (installedTrap)
        {
            case TrapType.BananaPeel:
                Debug.Log($"[Trap] {pawn.PawnName} 이(가) 바나나 껍질 발동. 추후 후퇴 처리 연결 예정");
                break;

            case TrapType.FakeTrophy:
                Debug.Log($"[Trap] {pawn.PawnName} 이(가) 가짜 트로피 발동. 추후 코인 감소 처리 연결 예정");
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