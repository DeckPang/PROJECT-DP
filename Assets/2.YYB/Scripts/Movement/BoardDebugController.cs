using UnityEngine;
using UnityEngine.InputSystem;

public class BoardDebugController : MonoBehaviour
{
    [SerializeField] private PlayerPawn playerPawn;

    private void Start()
    {
        Debug.Log("=== Board Debug Controller ===");
        Debug.Log("1 / 3 / 5 : 이동");
        Debug.Log("분기 선택 중일 때 1 / 2 / 3 : 갈림길 선택");
        Debug.Log("B : 첫 번째 다음 노드에 BananaPeel 설치");
        Debug.Log("F : 첫 번째 다음 노드에 FakeTrophy 설치");
        Debug.Log("C : 첫 번째 다음 노드 함정 제거");
        Debug.Log("I : 현재 노드 정보 출력");
    }

    private void Update()
    {
        if (Keyboard.current == null || playerPawn == null)
            return;

        if (playerPawn.IsWaitingForBranchChoice)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                playerPawn.SelectBranch(0);

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                playerPawn.SelectBranch(1);

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                playerPawn.SelectBranch(2);

            return;
        }

        if (playerPawn.IsMoving)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            playerPawn.MoveSteps(1);
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            playerPawn.MoveSteps(3);
        }

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            playerPawn.MoveSteps(5);
        }

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            BoardNode targetNode = GetFirstNextNode();
            if (targetNode != null)
                targetNode.InstallTrap(TrapType.BananaPeel, playerPawn.PawnName);
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            BoardNode targetNode = GetFirstNextNode();
            if (targetNode != null)
                targetNode.InstallTrap(TrapType.FakeTrophy, playerPawn.PawnName);
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            BoardNode targetNode = GetFirstNextNode();
            if (targetNode != null)
                targetNode.ClearTrap();
        }

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            PrintCurrentNodeInfo();
        }
    }

    private BoardNode GetFirstNextNode()
    {
        BoardNode currentNode = playerPawn.CurrentNode;
        if (currentNode == null || currentNode.NextNodes == null || currentNode.NextNodes.Count == 0)
        {
            Debug.LogWarning("[Debug] 현재 노드에 연결된 다음 노드가 없습니다.");
            return null;
        }

        return currentNode.NextNodes[0];
    }

    private void PrintCurrentNodeInfo()
    {
        BoardNode currentNode = playerPawn.CurrentNode;
        if (currentNode == null)
        {
            Debug.LogWarning("[Debug] CurrentNode가 없습니다.");
            return;
        }

        Debug.Log($"[Debug] 현재 노드 = {currentNode.NodeId}, Type = {currentNode.NodeType}, NextCount = {currentNode.NextNodes.Count}");
    }
}