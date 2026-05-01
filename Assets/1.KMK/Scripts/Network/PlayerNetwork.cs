using Fusion;
using UnityEngine;

/// <summary>
/// 플레이어당 하나씩 스폰되는 네트워크 오브젝트입니다.
/// </summary>
public class PlayerNetwork : NetworkBehaviour
{
    // 플레이어 번호 (1~4), 모든 클라이언트에 동기화
    [Networked] public int PlayerNumber { get; private set; }

    // 닉네임, 모든 클라이언트에 동기화
    [Networked] public NetworkString<_16> PlayerName { get; private set; }

    // 접속 상태, 모든 클라이언트에 동기화
    [Networked] public PlayerState State { get; private set; }

    public bool IsAI => State == PlayerState.Disconnected;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            PlayerNumber = GetPlayerNumber();
            State = PlayerState.Connected;
        }

        if (HasInputAuthority)
        {
            RPC_SetPlayerName("Player " + PlayerNumber);
        }
    }

    // ── Host에서 호출 ────────────────────────────────────────────────────────

    public void SetDisconnected()
    {
        if (!HasStateAuthority) return;
        State = PlayerState.Disconnected;
        Debug.Log($"[PlayerNetwork] {PlayerNumber}P 팅김 → AI 대기");
    }

    public void SetReconnecting()
    {
        if (!HasStateAuthority) return;
        State = PlayerState.Reconnecting;
    }

    public void SetConnected()
    {
        if (!HasStateAuthority) return;
        State = PlayerState.Connected;
        Debug.Log($"[PlayerNetwork] {PlayerNumber}P 재접속 완료");
    }

    // ── RPC ─────────────────────────────────────────────────────────────────

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string name)
    {
        PlayerName = name;
    }

    private int GetPlayerNumber()
    {
        var allPlayers = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        return allPlayers.Length;
    }
}
