using System;
using Fusion;
using UnityEngine;

/// <summary>
/// 로비의 한 슬롯 정보. 4개가 NetworkArray로 묶여 동기화됩니다.
/// </summary>
public struct LobbySlot : INetworkStruct
{
    public NetworkBool        Occupied;
    public NetworkBool        Ready;
    public PlayerRef          Owner;
    public NetworkString<_16> Name;
}

/// <summary>
/// 로비의 모든 동기화 상태(4슬롯)를 보유합니다. Host가 1개만 Spawn.
/// 모든 클라이언트는 이 한 곳에서 슬롯 정보를 읽습니다 — 단일 진실(Source of Truth).
/// </summary>
public class LobbyState : NetworkBehaviour
{
    public const int MaxSlots = 4;

    [Networked, Capacity(MaxSlots), OnChangedRender(nameof(OnSlotsChanged))]
    public NetworkArray<LobbySlot> Slots => default;

    /// <summary>슬롯 데이터가 변경되어 UI를 다시 그려야 함.</summary>
    public event Action SlotsChanged;

    public override void Spawned()
    {
        // 내가 등장했음을 LobbyController에게 알림 (Client에서 특히 중요)
        var ctrl = FindAnyObjectByType<LobbyController>();
        if (ctrl != null) ctrl.RegisterLobbyState(this);

        // 늦게 들어온 클라가 즉시 UI를 그릴 수 있도록 한 번 강제 발생
        SlotsChanged?.Invoke();
    }

    // ── Host 전용 API ────────────────────────────────────────────────

    public bool TryAssignSlot(PlayerRef player, out int assignedSlot)
    {
        assignedSlot = -1;
        if (!HasStateAuthority) return false;

        for (int i = 0; i < MaxSlots; i++)
        {
            if (Slots[i].Occupied) continue;
            Slots.Set(i, new LobbySlot
            {
                Occupied = true,
                Ready    = i == 0,            // 호스트(0번 슬롯)는 항상 Ready
                Owner    = player,
                Name     = $"Player {i + 1}",
            });
            assignedSlot = i;
            return true;
        }
        return false;
    }

    public void ReleaseSlot(PlayerRef player)
    {
        if (!HasStateAuthority) return;

        for (int i = 0; i < MaxSlots; i++)
        {
            if (!Slots[i].Occupied) continue;
            if (Slots[i].Owner != player) continue;
            Slots.Set(i, default);
            return;
        }
    }

    public bool CanStart()
    {
        int count = 0;
        for (int i = 0; i < MaxSlots; i++)
        {
            if (!Slots[i].Occupied) continue;
            count++;
            if (!Slots[i].Ready) return false;
        }
        return count >= 2;
    }

    // ── 조회 ─────────────────────────────────────────────────────────

    public int FindSlotOf(PlayerRef player)
    {
        for (int i = 0; i < MaxSlots; i++)
            if (Slots[i].Occupied && Slots[i].Owner == player) return i;
        return -1;
    }

    // ── RPC: Client → Host ──────────────────────────────────────────

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetReady(int slot, NetworkBool ready, RpcInfo info = default)
    {
        if (slot < 0 || slot >= MaxSlots) return;
        if (!Slots[slot].Occupied)        return;
        if (Slots[slot].Owner != info.Source) return; // 자기 슬롯만 변경 가능
        if (slot == 0)                    return;     // 호스트는 항상 Ready 고정

        var s = Slots[slot];
        s.Ready = ready;
        Slots.Set(slot, s);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetName(int slot, string name, RpcInfo info = default)
    {
        if (slot < 0 || slot >= MaxSlots) return;
        if (!Slots[slot].Occupied)        return;
        if (Slots[slot].Owner != info.Source) return;

        var s = Slots[slot];
        s.Name = name;
        Slots.Set(slot, s);
    }

    // ── 변경 감지 (모든 클라이언트에서 호출됨) ────────────────────────

    private void OnSlotsChanged()
    {
        SlotsChanged?.Invoke();
    }
}
