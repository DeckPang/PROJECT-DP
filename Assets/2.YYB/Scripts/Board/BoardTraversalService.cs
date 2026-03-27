using System.Collections.Generic;
using UnityEngine;

public static class BoardTraversalService
{
    public static List<BoardNode> BuildPath(BoardNode startNode, int steps, IReadOnlyList<int> branchChoices = null)
    {
        List<BoardNode> path = new List<BoardNode>();

        if (startNode == null || steps <= 0)
            return path;

        BoardNode current = startNode;
        int branchChoiceCursor = 0;

        for (int i = 0; i < steps; i++)
        {
            if (current.NextNodes == null || current.NextNodes.Count == 0)
            {
                Debug.LogWarning($"[Traversal] Node {current.NodeId} 에 다음 노드 연결이 없습니다.");
                break;
            }

            int selectedBranchIndex = 0;

            if (current.NextNodes.Count > 1)
            {
                if (branchChoices != null && branchChoiceCursor < branchChoices.Count)
                {
                    selectedBranchIndex = branchChoices[branchChoiceCursor];
                }

                branchChoiceCursor++;
            }

            selectedBranchIndex = Mathf.Clamp(selectedBranchIndex, 0, current.NextNodes.Count - 1);

            BoardNode next = current.NextNodes[selectedBranchIndex];

            if (next == null)
            {
                Debug.LogWarning($"[Traversal] Node {current.NodeId} 의 다음 노드 참조가 null 입니다.");
                break;
            }

            path.Add(next);
            current = next;
        }

        return path;
    }
}