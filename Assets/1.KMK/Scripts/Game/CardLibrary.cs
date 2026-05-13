using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CardDefinition들을 CardId 문자열로 조회하는 정적 헬퍼.
/// 네트워크로는 CardId만 흘리고, 실제 데이터는 각 클라이언트가 로컬 조회.
/// </summary>
public static class CardLibrary
{
    private static readonly Dictionary<string, CardDefinition> _byId        = new();
    private static readonly List<CardDefinition>               _turnGranted = new();
    private static bool _initialized;

    public static bool IsInitialized => _initialized;
    public static int  Count         => _byId.Count;

    /// <summary>외부에서 명시적으로 등록 (GameDeck.Spawned에서 호출).</summary>
    public static void RegisterAll(IEnumerable<CardDefinition> defs)
    {
        if (defs == null) return;

        foreach (var d in defs)
        {
            if (d == null || string.IsNullOrEmpty(d.CardId)) continue;
            _byId[d.CardId] = d;
        }

        // 카테고리별 캐시 갱신
        _turnGranted.Clear();
        foreach (var d in _byId.Values)
        {
            if (d.CardPoolType == CardPoolType.TurnGranted)
                _turnGranted.Add(d);
        }

        _initialized = true;
    }

    public static CardDefinition GetById(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return null;
        _byId.TryGetValue(cardId, out var def);
        return def;
    }

    /// <summary>매턴 자동 지급되어야 하는 카드들 (예: basic_walk).</summary>
    public static IReadOnlyList<CardDefinition> TurnGrantedCards => _turnGranted;

    public static void Clear()
    {
        _byId.Clear();
        _turnGranted.Clear();
        _initialized = false;
    }
}
