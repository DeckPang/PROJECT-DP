using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardDebugController : MonoBehaviour
{
    [SerializeField] private PlayerPawn playerPawn;
    [SerializeField] private int selectedBranchIndex = 0;

    private void Start()
    {
        Debug.Log("=== Board Debug Controller ===");
        Debug.Log("1 / 3 / 5 : 이동");
        Debug.Log("Q / W / E : 다음 분기 선택 (0 / 1 / 2)");
        Debug.Log("B : 선택된 다음 노드에 BananaPeel 설치");
        Debug.Log("F : 선택된 다음 노드에 FakeTrophy 설치");
        Debug.Log("C : 선택된 다음 노드 함정 제거");
        Debug.Log("I : 현재 노드 정보 출력");
    }

    private void Update()
    {
        if (Keyboard.current == null || playerPawn == null || playerPawn.IsMoving)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            selectedBranchIndex = 0;
            Debug.Log("[Debug] 다음 분기 선택 = 0");
        }

        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            selectedBranchIndex = 1;
            Debug.Log("[Debug] 다음 분기 선택 = 1");
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            selectedBranchIndex = 2;
            Debug.Log("[Debug] 다음 분기 선택 = 2");
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            playerPawn.MoveSteps(1, new List<int> { selectedBranchIndex });
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            playerPawn.MoveSteps(3, new List<int> { selectedBranchIndex });
        }

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            playerPawn.MoveSteps(5, new List<int> { selectedBranchIndex });
        }

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            BoardNode targetNode = GetSelectedNextNode();
            if (targetNode != null)
                targetNode.InstallTrap(TrapType.BananaPeel, playerPawn.PawnName);
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            BoardNode targetNode = GetSelectedNextNode();
            if (targetNode != null)
                targetNode.InstallTrap(TrapType.FakeTrophy, playerPawn.PawnName);
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            BoardNode targetNode = GetSelectedNextNode();
            if (targetNode != null)
                targetNode.ClearTrap();
        }

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            PrintCurrentNodeInfo();
        }
    }

    private BoardNode GetSelectedNextNode()
    {
        BoardNode currentNode = playerPawn.CurrentNode;
        if (currentNode == null || currentNode.NextNodes == null || currentNode.NextNodes.Count == 0)
        {
            Debug.LogWarning("[Debug] 현재 노드에 연결된 다음 노드가 없습니다.");
            return null;
        }

        int clampedIndex = Mathf.Clamp(selectedBranchIndex, 0, currentNode.NextNodes.Count - 1);
        return currentNode.NextNodes[clampedIndex];
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