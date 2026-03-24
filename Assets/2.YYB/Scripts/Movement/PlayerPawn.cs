using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPawn : MonoBehaviour
{
    [Header("Pawn Info")]
    [SerializeField] private string pawnName = "P1";
    [SerializeField] private BoardGenerator boardGenerator;
    [SerializeField] private int currentNodeIndex = 0;

    [Header("Move Settings")]
    [SerializeField] private float verticalOffset = 0.75f;
    [SerializeField] private float moveStepDelay = 0.15f;

    [Header("Debug Status")]
    [SerializeField] private int coins = 0;
    [SerializeField] private int hp = 5;

    private bool isMoving;

    public string PawnName => pawnName;
    public int CurrentNodeIndex => currentNodeIndex;
    public bool IsMoving => isMoving;

    private IEnumerator Start()
    {
        yield return null;
        SnapToCurrentNode();
    }

    public void SnapToCurrentNode()
    {
        if (boardGenerator == null)
        {
            Debug.LogWarning("[PlayerPawn] BoardGenerator reference가 없습니다.");
            return;
        }

        if (boardGenerator.NodeCount == 0)
        {
            Debug.LogWarning("[PlayerPawn] 아직 생성된 노드가 없습니다.");
            return;
        }

        currentNodeIndex = BoardMovementService.NormalizeIndex(currentNodeIndex, boardGenerator.NodeCount);
        BoardNode node = boardGenerator.GetNode(currentNodeIndex);

        if (node == null)
            return;

        transform.position = node.transform.position + Vector3.up * verticalOffset;
        Debug.Log($"[{pawnName}] 현재 위치를 Node {currentNodeIndex} 로 스냅했습니다.");
    }

    public void MoveBySteps(int steps)
    {
        if (isMoving)
        {
            Debug.LogWarning($"[{pawnName}] 이미 이동 중입니다.");
            return;
        }

        if (boardGenerator == null || boardGenerator.NodeCount == 0)
        {
            Debug.LogWarning($"[{pawnName}] 이동할 보드가 준비되지 않았습니다.");
            return;
        }

        StartCoroutine(MoveRoutine(steps));
    }

    private IEnumerator MoveRoutine(int steps)
    {
        isMoving = true;

        List<int> path = BoardMovementService.GetPathIndices(currentNodeIndex, steps, boardGenerator.NodeCount);

        if (path.Count == 0)
        {
            Debug.Log($"[{pawnName}] {steps}칸 이동 -> 제자리 유지");
            boardGenerator.GetNode(currentNodeIndex)?.OnPlayerArrived(this);
            isMoving = false;
            yield break;
        }

        int previousIndex = currentNodeIndex;

        foreach (int nextIndex in path)
        {
            currentNodeIndex = nextIndex;

            BoardNode nextNode = boardGenerator.GetNode(currentNodeIndex);
            if (nextNode != null)
            {
                transform.position = nextNode.transform.position + Vector3.up * verticalOffset;
            }

            if (steps > 0 && previousIndex > currentNodeIndex)
            {
                Debug.Log($"[{pawnName}] 출발지(Start)를 통과했습니다. (최대 마나 증가 처리 위치)");
            }

            previousIndex = currentNodeIndex;
            yield return new WaitForSeconds(moveStepDelay);
        }

        boardGenerator.GetNode(currentNodeIndex)?.OnPlayerArrived(this);

        isMoving = false;
    }

    public void ChangeCoins(int amount)
    {
        coins += amount;
        Debug.Log($"[{pawnName}] 코인 변화: {amount} | 현재 코인: {coins}");
    }

    public void ChangeHp(int amount)
    {
        hp += amount;
        Debug.Log($"[{pawnName}] 체력 변화: {amount} | 현재 체력: {hp}");
    }
}