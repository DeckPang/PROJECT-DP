using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private Transform nodeRoot;
    [SerializeField] private bool autoCollectOnAwake = true;
    [SerializeField] private List<BoardNode> nodes = new List<BoardNode>();

    public List<BoardNode> Nodes => nodes;

    private void Awake()
    {
        if (autoCollectOnAwake)
        {
            RefreshNodeCache();
        }
    }

    [ContextMenu("Refresh Node Cache")]
    public void RefreshNodeCache()
    {
        nodes.Clear();

        if (nodeRoot == null)
        {
            Debug.LogWarning("[BoardManager] NodeRoot가 비어 있습니다.");
            return;
        }

        nodes = nodeRoot.GetComponentsInChildren<BoardNode>(true)
                        .OrderBy(n => n.NodeId)
                        .ToList();

        Debug.Log($"[BoardManager] Node Cache Refresh 완료 | Count = {nodes.Count}");
    }

    public BoardNode GetNodeById(int nodeId)
    {
        return nodes.Find(n => n.NodeId == nodeId);
    }

    public BoardNode GetStartNode()
    {
        return nodes.Find(n => n.NodeType == BoardNodeType.Start);
    }
}