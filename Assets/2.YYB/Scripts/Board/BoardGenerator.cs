using System.Collections.Generic;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int nodeCount = 16;
    [SerializeField] private float radius = 8f;
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private Transform nodeParent;
    [SerializeField] private bool autoGenerateOnStart = true;
    [SerializeField] private bool clearBeforeGenerate = true;

    [Header("Generated Nodes")]
    [SerializeField] private List<BoardNode> nodes = new List<BoardNode>();

    public int NodeCount => nodes.Count;

    private static readonly BoardNodeType[] DefaultPattern =
    {
        BoardNodeType.Resource,
        BoardNodeType.CardDraw,
        BoardNodeType.Battle,
        BoardNodeType.Trap,
        BoardNodeType.Event,
        BoardNodeType.Shop,
        BoardNodeType.Resource,
        BoardNodeType.Battle,
        BoardNodeType.Trap,
        BoardNodeType.CardDraw,
        BoardNodeType.Resource,
        BoardNodeType.Jail,
        BoardNodeType.Event,
        BoardNodeType.Roulette
    };

    private void Start()
    {
        if (autoGenerateOnStart)
        {
            GenerateBoard();
        }
    }

    [ContextMenu("Generate Board")]
    public void GenerateBoard()
    {
        if (nodePrefab == null)
        {
            Debug.LogError("[BoardGenerator] Node Prefab이 비어 있습니다.");
            return;
        }

        if (nodeParent == null)
        {
            GameObject parentObj = new GameObject("Nodes");
            parentObj.transform.SetParent(transform);
            nodeParent = parentObj.transform;
        }

        if (clearBeforeGenerate)
        {
            ClearBoard();
        }

        nodes.Clear();

        float angleStep = 360f / nodeCount;

        for (int i = 0; i < nodeCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 localPos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            GameObject nodeObj = Instantiate(nodePrefab, nodeParent);
            nodeObj.transform.localPosition = localPos;
            nodeObj.transform.localRotation = Quaternion.identity;

            BoardNode node = nodeObj.GetComponent<BoardNode>();
            if (node == null)
            {
                node = nodeObj.AddComponent<BoardNode>();
            }

            BoardNodeType nodeType = GetNodeTypeByIndex(i);
            node.Initialize(i, nodeType);

            nodes.Add(node);
        }

        Debug.Log($"[BoardGenerator] 보드 생성 완료 | Node Count = {nodes.Count}");
    }

    public BoardNode GetNode(int index)
    {
        if (nodes == null || nodes.Count == 0)
            return null;

        index = BoardMovementService.NormalizeIndex(index, nodes.Count);
        return nodes[index];
    }

    private BoardNodeType GetNodeTypeByIndex(int index)
    {
        if (index == 0)
            return BoardNodeType.Start;

        return DefaultPattern[(index - 1) % DefaultPattern.Length];
    }

    private void ClearBoard()
    {
        if (nodeParent == null)
            return;

        for (int i = nodeParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = nodeParent.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }
}