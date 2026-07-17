using UnityEngine;
using System.Collections.Generic;

public class PlayerLifeManager : MonoBehaviour
{
    public static PlayerLifeManager Instance { get; private set; }

    private List<GameObject> alivePlayers = new List<GameObject>();

    public bool AllPlayersDead => alivePlayers.Count == 0;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterPlayer(GameObject player)
    {
        if (!alivePlayers.Contains(player))
            alivePlayers.Add(player);
    }

    public void OnPlayerDied(GameObject player)
    {
        alivePlayers.Remove(player);
    }
}
