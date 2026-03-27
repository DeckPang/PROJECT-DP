using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPawn : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private CharacterInfo characterInfo;
    [SerializeField] private BoardNode currentNode;

    [Header("Move Settings")]
    [SerializeField] private float verticalOffset = 0.75f;
    [SerializeField] private float moveStepDelay = 0.2f;

    [Header("Debug")]
    [SerializeField] private string debugPawnName = "P1";

    private bool isMoving;

    public string PawnName => characterInfo != null ? characterInfo.UserName : debugPawnName;
    public CharacterInfo CharacterInfo => characterInfo;
    public BoardNode CurrentNode => currentNode;
    public bool IsMoving => isMoving;

    private IEnumerator Start()
    {
        yield return null;

        if (characterInfo == null)
            characterInfo = GetComponent<CharacterInfo>();

        if (currentNode == null && boardManager != null)
            currentNode = boardManager.GetStartNode();

        SnapToCurrentNode();
    }

    public void SetCurrentNode(BoardNode node, bool snapImmediately = true)
    {
        currentNode = node;

        if (snapImmediately)
            SnapToCurrentNode();
    }

    public void SnapToCurrentNode()
    {
        if (currentNode == null)
        {
            Debug.LogWarning("[PlayerPawn] CurrentNode가 없습니다.");
            return;
        }

        transform.position = currentNode.transform.position + Vector3.up * verticalOffset;
        Debug.Log($"[{PawnName}] 현재 위치 스냅 -> Node {currentNode.NodeId}");
    }

    public void MoveSteps(int steps, List<int> branchChoices = null)
    {
        if (isMoving)
        {
            Debug.LogWarning($"[{PawnName}] 이미 이동 중입니다.");
            return;
        }

        if (currentNode == null)
        {
            Debug.LogWarning($"[{PawnName}] CurrentNode가 없어 이동할 수 없습니다.");
            return;
        }

        StartCoroutine(MoveRoutine(steps, branchChoices));
    }

    private IEnumerator MoveRoutine(int steps, List<int> branchChoices)
    {
        isMoving = true;

        List<BoardNode> path = BoardTraversalService.BuildPath(currentNode, steps, branchChoices);

        if (path.Count == 0)
        {
            currentNode.OnPlayerArrived(this);
            isMoving = false;
            yield break;
        }

        foreach (BoardNode nextNode in path)
        {
            currentNode = nextNode;
            transform.position = currentNode.transform.position + Vector3.up * verticalOffset;

            if (currentNode.NodeType == BoardNodeType.Start)
            {
                Debug.Log($"[{PawnName}] Start 노드를 통과/도착했습니다. (추후 최대 마나 증가 연결 가능)");
            }

            yield return new WaitForSeconds(moveStepDelay);
        }

        currentNode.OnPlayerArrived(this);
        isMoving = false;
    }
}