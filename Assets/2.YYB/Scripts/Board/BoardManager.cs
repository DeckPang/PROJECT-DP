using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private Transform nodeRoot;
    [SerializeField] private bool autoCollectOnAwake = true;
    [SerializeField] private List<BoardNode> nodes = new();

    public IReadOnlyList<BoardNode> Nodes => nodes;

    private void Awake()
    {
        if (autoCollectOnAwake)
            RefreshNodeCache();
    }

    [ContextMenu("Refresh Node Cache")]
    public void RefreshNodeCache()
    {
        nodes.Clear();

        if (nodeRoot == null)
        {
            Debug.LogWarning("[BoardManager] NodeRoot가 비어있음");
            return;
        }

        nodes = nodeRoot.GetComponentsInChildren<BoardNode>(true)
                        .OrderBy(n => n.NodeId)
                        .ToList();

        Debug.Log($"[BoardManager] Node Cache Refresh 완료 | Count = {nodes.Count}");
    }

    public BoardNode GetNodeById(int nodeId)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].NodeId == nodeId)
                return nodes[i];
        }
        return null;
    }

    public BoardNode GetStartNode()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].NodeType == BoardNodeType.Start)
                return nodes[i];
        }
        return null;
    }
}