using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 씬에 1개만 존재. NetworkPlayer가 등장하면 로컬에서 PlayerPawnView를 만들어 매핑.
/// Spawn은 네트워크 동기화가 아닌 로컬 시각 표현이므로 Instantiate (NOT runner.Spawn).
/// </summary>
public class PawnSpawner : MonoBehaviour
{
    [SerializeField] private PlayerPawnView pawnViewPrefab;
    [SerializeField] private BoardManager   boardManager;

    private readonly Dictionary<NetworkPlayer, PlayerPawnView> _pawns = new();

    private void OnEnable()
    {
        NetworkPlayer.OnSpawnedStatic   += HandleSpawned;
        NetworkPlayer.OnDespawnedStatic += HandleDespawned;
    }

    private void OnDisable()
    {
        NetworkPlayer.OnSpawnedStatic   -= HandleSpawned;
        NetworkPlayer.OnDespawnedStatic -= HandleDespawned;
    }

    private void Start()
    {
        if (boardManager == null)
            boardManager = FindAnyObjectByType<BoardManager>();

        // 이미 Spawn된 NetworkPlayer가 있다면 보완적으로 처리
        foreach (var np in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
            HandleSpawned(np);
    }

    // ── NetworkPlayer 이벤트 처리 ──────────────────────────────────

    private void HandleSpawned(NetworkPlayer player)
    {
        if (player == null) return;
        if (_pawns.ContainsKey(player)) return;
        if (pawnViewPrefab == null)
        {
            Debug.LogError("[PawnSpawner] pawnViewPrefab이 인스펙터에 연결되지 않았습니다.");
            return;
        }
        if (boardManager == null)
        {
            Debug.LogError("[PawnSpawner] BoardManager를 찾을 수 없습니다.");
            return;
        }

        var view = Instantiate(pawnViewPrefab);
        view.name = $"PawnView_Slot{player.SlotIndex}_{player.PlayerName}";
        view.Initialize(player, boardManager);
        _pawns[player] = view;

        Debug.Log($"[PawnSpawner] PawnView 생성: Slot {player.SlotIndex}");
    }

    private void HandleDespawned(NetworkPlayer player)
    {
        if (player == null) return;
        if (!_pawns.TryGetValue(player, out var view)) return;
        if (view != null) Destroy(view.gameObject);
        _pawns.Remove(player);

        Debug.Log($"[PawnSpawner] PawnView 제거: Slot {player.SlotIndex}");
    }
}
