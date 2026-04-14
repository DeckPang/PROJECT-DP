using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPawn : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private CharacterInfo characterInfo;
    [SerializeField] private BoardNode currentNode;
    [SerializeField] private BranchSelectionUI branchSelectionUI;

    [Header("Move Settings")]
    [SerializeField] private float verticalOffset = 0.75f;
    [SerializeField] private float moveStepDelay = 0.2f;

    [Header("Debug")]
    [SerializeField] private string debugPawnName = "P1";

    private bool isMoving;
    private int pendingBranchIndex = -1;
    private bool isWaitingForBranchChoice = false;

    public string PawnName => characterInfo != null ? characterInfo.UserName : debugPawnName;
    public CharacterInfo CharacterInfo => characterInfo;
    public BoardNode CurrentNode => currentNode;
    public bool IsMoving => isMoving;
    public bool IsWaitingForBranchChoice => isWaitingForBranchChoice;

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

    public bool MoveSteps(int steps)
    {
        if (isMoving)
        {
            Debug.LogWarning($"[{PawnName}] 이미 이동 중입니다.");
            return false;
        }

        if (isWaitingForBranchChoice)
        {
            Debug.LogWarning($"[{PawnName}] 현재 갈림길 선택 대기 중입니다.");
            return false;
        }

        if (currentNode == null)
        {
            Debug.LogWarning($"[{PawnName}] CurrentNode가 없어 이동할 수 없습니다.");
            return false;
        }

        StartCoroutine(MoveRoutine(steps));
        return true;
    }

    private IEnumerator MoveRoutine(int steps)
    {
        isMoving = true;

        if (steps <= 0)
        {
            currentNode.OnPlayerArrived(this);
            isMoving = false;
            yield break;
        }

        int remainingSteps = steps;

        while (remainingSteps > 0)
        {
            if (currentNode.NextNodes == null || currentNode.NextNodes.Count == 0)
            {
                Debug.LogWarning($"[{PawnName}] Node {currentNode.NodeId} 에 다음 노드가 없습니다. 이동 종료.");
                break;
            }

            BoardNode nextNode = null;

            if (currentNode.NextNodes.Count == 1)
            {
                nextNode = currentNode.NextNodes[0];
            }
            else
            {
                yield return StartCoroutine(WaitForBranchChoice());

                if (pendingBranchIndex < 0 || pendingBranchIndex >= currentNode.NextNodes.Count)
                {
                    Debug.LogWarning($"[{PawnName}] 잘못된 분기 선택값입니다. 이동 종료.");
                    break;
                }

                nextNode = currentNode.NextNodes[pendingBranchIndex];
                pendingBranchIndex = -1;
            }

            if (nextNode == null)
            {
                Debug.LogWarning($"[{PawnName}] 선택된 다음 노드가 null 입니다. 이동 종료.");
                break;
            }

            currentNode = nextNode;
            transform.position = currentNode.transform.position + Vector3.up * verticalOffset;

            if (currentNode.NodeType == BoardNodeType.Start)
            {
                Debug.Log($"[{PawnName}] Start 노드를 통과/도착했습니다. (추후 최대 마나 증가 연결 가능)");
            }

            remainingSteps--;
            yield return new WaitForSeconds(moveStepDelay);
        }

        currentNode.OnPlayerArrived(this);
        isMoving = false;
    }

    private IEnumerator WaitForBranchChoice()
    {
        isWaitingForBranchChoice = true;
        pendingBranchIndex = -1;

        Debug.Log($"[{PawnName}] 갈림길 도착. 분기를 선택하세요.");

        if (branchSelectionUI != null)
        {
            branchSelectionUI.ShowChoices(this, currentNode.NextNodes);
        }

        yield return new WaitUntil(() => pendingBranchIndex >= 0);

        if (branchSelectionUI != null)
        {
            branchSelectionUI.HideChoices();
        }

        isWaitingForBranchChoice = false;
    }

    public void SelectBranch(int branchIndex)
    {
        if (!isWaitingForBranchChoice)
        {
            Debug.LogWarning($"[{PawnName}] 현재는 분기 선택 상태가 아닙니다.");
            return;
        }

        pendingBranchIndex = branchIndex;
        Debug.Log($"[{PawnName}] 분기 선택 완료 -> {branchIndex}");
    }
}