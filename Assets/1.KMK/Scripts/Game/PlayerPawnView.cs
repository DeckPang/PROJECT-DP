using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// NetworkPlayer를 따라가는 시각 전용 폰(말). 네트워크 권한/RPC 없음.
/// 같은 노드에 여러 폰이 있을 때 자동으로 원형 배치합니다.
/// </summary>
public class PlayerPawnView : MonoBehaviour
{
    [Header("Move Settings")]
    [SerializeField] private float verticalOffset = 0.75f;
    [SerializeField] private float moveDuration   = 0.4f;

    [Header("Co-occupancy Layout")]
    [Tooltip("같은 노드에 모일 때 중심에서 떨어지는 거리")]
    [SerializeField] private float clusterRadius  = 0.4f;

    [Header("Display (선택)")]
    [SerializeField] private TextMeshPro nameLabel;
    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private Color[] slotColors = {
        Color.red, Color.blue, Color.green, Color.yellow
    };
    //카메라 필드 추가했음 -여영부
    [Header("Camera")]
    [SerializeField] private Transform cameraTarget;

    // ── 정적 레지스트리 (같은 노드 폰 검색용) ──────────────────
    private static readonly List<PlayerPawnView> _all = new();

    private NetworkPlayer _networkPlayer;
    private BoardManager  _boardMgr;
    private Coroutine     _activeMove;
    private int           _lastNodeId = -1;

    //프로퍼티도 추가 -여영부
    public int SlotIndex =>
    _networkPlayer != null ? _networkPlayer.SlotIndex : -1;

    public Transform CameraTarget =>
        cameraTarget != null ? cameraTarget : transform;

    // ── 초기화/해제 ───────────────────────────────────────────────

    public void Initialize(NetworkPlayer player, BoardManager boardMgr)
    {
        _networkPlayer = player;
        _boardMgr      = boardMgr;
        _all.Add(this);

        if (_networkPlayer != null)
            _networkPlayer.PositionChanged += OnPositionChanged;

        ApplyVisuals();

        _lastNodeId = _networkPlayer != null ? _networkPlayer.CurrentNodeId : -1;
        SnapToNodeCenter();

        // 새로 도착했으니 같은 노드의 모든 폰 재정렬 (기존 폰들이 옆으로 이동)
        RelayoutPawnsAt(_lastNodeId);
    }

    private void OnDestroy()
    {
        if (_networkPlayer != null)
            _networkPlayer.PositionChanged -= OnPositionChanged;

        int leftNode = _lastNodeId;
        _all.Remove(this);

        // 떠난 노드의 남은 폰들도 재정렬
        if (leftNode >= 0) RelayoutPawnsAt(leftNode);
    }

    // ── 위치 변경 감지 ─────────────────────────────────────────────

    private void OnPositionChanged()
    {
        if (_networkPlayer == null) return;

        int oldNodeId = _lastNodeId;
        int newNodeId = _networkPlayer.CurrentNodeId;
        _lastNodeId   = newNodeId;

        // 두 노드 모두 재정렬: 옛 노드(남은 폰들 빈자리 채움) + 새 노드(새 폰 합류)
        if (oldNodeId != newNodeId) RelayoutPawnsAt(oldNodeId);
        RelayoutPawnsAt(newNodeId);
    }

    // ── 노드별 폰 재정렬 ───────────────────────────────────────────

    private static void RelayoutPawnsAt(int nodeId)
    {
        if (nodeId < 0) return;

        // 같은 노드 폰들 수집 (SlotIndex 순)
        var atNode = new List<PlayerPawnView>();
        foreach (var p in _all)
        {
            if (p == null || p._networkPlayer == null) continue;
            if (p._networkPlayer.CurrentNodeId == nodeId) atNode.Add(p);
        }
        atNode.Sort((a, b) => a._networkPlayer.SlotIndex.CompareTo(b._networkPlayer.SlotIndex));

        // 각 폰에게 자기 자리를 알려줌
        for (int i = 0; i < atNode.Count; i++)
        {
            Vector3 offset = ComputeLayoutOffset(i, atNode.Count, atNode[i].clusterRadius);
            atNode[i].TweenToOffset(offset);
        }
    }

    /// <summary>원형 배치: 인원이 1명이면 중앙, 여러 명이면 균등 분산.</summary>
    private static Vector3 ComputeLayoutOffset(int index, int total, float radius)
    {
        if (total <= 1) return Vector3.zero;

        // 시작 각도를 위(z+)에서 시작, 시계 방향
        float angle = (Mathf.PI * 2f * index / total) + Mathf.PI / 2f;
        return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }

    // ── Tween ────────────────────────────────────────────────────

    private void TweenToOffset(Vector3 localOffset)
    {
        if (_boardMgr == null) return;
        var node = _boardMgr.GetNodeById(_lastNodeId);
        if (node == null) return;

        Vector3 target = node.transform.position + Vector3.up * verticalOffset + localOffset;
        if (_activeMove != null) StopCoroutine(_activeMove);
        _activeMove = StartCoroutine(TweenTo(target));
    }

    private IEnumerator TweenTo(Vector3 end)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / moveDuration);
            transform.position = Vector3.Lerp(start, end, k);
            yield return null;
        }
        transform.position = end;
        _activeMove = null;
    }

    private void SnapToNodeCenter()
    {
        if (_boardMgr == null) return;
        var node = _boardMgr.GetNodeById(_lastNodeId);
        if (node == null) return;
        transform.position = node.transform.position + Vector3.up * verticalOffset;
    }

    // ── 비주얼 ─────────────────────────────────────────────────

    private void ApplyVisuals()
    {
        if (_networkPlayer == null) return;

        if (nameLabel != null)
            nameLabel.text = $"{_networkPlayer.PlayerName}";

        if (bodyRenderer != null && slotColors != null && slotColors.Length > 0)
        {
            int idx = Mathf.Clamp(_networkPlayer.SlotIndex, 0, slotColors.Length - 1);
            var mpb = new MaterialPropertyBlock();
            bodyRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", slotColors[idx]);   // URP
            mpb.SetColor("_Color",     slotColors[idx]);   // Built-in
            bodyRenderer.SetPropertyBlock(mpb);
        }
    }
}
