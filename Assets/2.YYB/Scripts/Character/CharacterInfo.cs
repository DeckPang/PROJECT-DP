using System;
using UnityEngine;

public class CharacterInfo : MonoBehaviour
{
    [SerializeField]
    private CharacterInfoData data = new CharacterInfoData
    {
        playerId = "P1",
        userName = "Player1",
        hp = 5,
        coins = 0,
        trophies = 0,
        maxMana = 5,
        currentMana = 5
    };

    public event Action OnStatsChanged;

    public CharacterInfoData Data => data;

    public string PlayerId => data.playerId;
    public string UserName => data.userName;
    public int Hp => data.hp;
    public int Coins => data.coins;
    public int Trophies => data.trophies;
    public int MaxMana => data.maxMana;
    public int CurrentMana => data.currentMana;

    private void Awake()
    {
        data.currentMana = Mathf.Clamp(data.currentMana, 0, data.maxMana);
        if (data.currentMana == 0)
            data.currentMana = data.maxMana;

        NotifyChanged();
    }

    public void SetUserName(string newName)
    {
        data.userName = newName;
        NotifyChanged();
    }

    public void ChangeHp(int amount)
    {
        CharacterInfoService.ChangeHp(data, amount);
        NotifyChanged();
    }

    public void ChangeCoins(int amount)
    {
        CharacterInfoService.ChangeCoins(data, amount);
        NotifyChanged();
    }

    public void AddTrophy(int amount = 1)
    {
        CharacterInfoService.AddTrophy(data, amount);
        NotifyChanged();
    }

    public void IncreaseMaxMana(int amount)
    {
        CharacterInfoService.IncreaseMaxMana(data, amount);
        NotifyChanged();
    }

    public void RestoreManaToFull()
    {
        CharacterInfoService.RestoreManaToFull(data);
        NotifyChanged();
    }

    public void ChangeMana(int amount)
    {
        CharacterInfoService.ChangeMana(data, amount);
        NotifyChanged();
    }

    public bool TrySpendMana(int cost)
    {
        bool success = CharacterInfoService.TrySpendMana(data, cost);

        if (success)
            NotifyChanged();

        return success;
    }

    private void NotifyChanged()
    {
        OnStatsChanged?.Invoke();
    }
}