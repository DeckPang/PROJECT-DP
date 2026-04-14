using System;
using UnityEngine;

public class CharacterInfo : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string playerId = "P1";
    [SerializeField] private string userName = "Player1";

    [Header("Stats")]
    [SerializeField] private int hp = 5;
    [SerializeField] private int coins = 0;
    [SerializeField] private int trophies = 0;

    [Header("Mana")]
    [SerializeField] private int maxMana = 5;
    [SerializeField] private int currentMana = 5;

    public string PlayerId => playerId;
    public string UserName => userName;
    public int Hp => hp;
    public int Coins => coins;
    public int Trophies => trophies;
    public int MaxMana => maxMana;
    public int CurrentMana => currentMana;

    public event Action OnStatsChanged;

    private void Awake()
    {
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        if (currentMana == 0)
            currentMana = maxMana;

        NotifyChanged();
    }

    public void SetUserName(string newName)
    {
        userName = newName;
        Debug.Log($"[CharacterInfo] 이름 변경: {userName}");
        NotifyChanged();
    }

    public void ChangeHp(int amount)
    {
        hp += amount;
        hp = Mathf.Max(0, hp);
        Debug.Log($"[CharacterInfo] HP 변화: {amount} | 현재 HP: {hp}");
        NotifyChanged();
    }

    public void ChangeCoins(int amount)
    {
        coins += amount;
        coins = Mathf.Max(0, coins);
        Debug.Log($"[CharacterInfo] Coin 변화: {amount} | 현재 Coin: {coins}");
        NotifyChanged();
    }

    public void AddTrophy(int amount = 1)
    {
        trophies += amount;
        trophies = Mathf.Max(0, trophies);
        Debug.Log($"[CharacterInfo] Trophy 변화: +{amount} | 현재 Trophy: {trophies}");
        NotifyChanged();
    }

    public void IncreaseMaxMana(int amount)
    {
        maxMana += amount;
        maxMana = Mathf.Max(0, maxMana);
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        Debug.Log($"[CharacterInfo] MaxMana 증가: +{amount} | 현재 MaxMana: {maxMana}");
        NotifyChanged();
    }

    public void RestoreManaToFull()
    {
        currentMana = maxMana;
        Debug.Log($"[CharacterInfo] 마나 전부 회복 | 현재 Mana: {currentMana}/{maxMana}");
        NotifyChanged();
    }

    public void ChangeMana(int amount)
    {
        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        Debug.Log($"[CharacterInfo] Mana 변화: {amount} | 현재 Mana: {currentMana}/{maxMana}");
        NotifyChanged();
    }

    public bool TrySpendMana(int cost)
    {
        if (currentMana < cost)
        {
            Debug.LogWarning($"[CharacterInfo] 마나 부족 | 필요: {cost}, 현재: {currentMana}");
            return false;
        }

        currentMana -= cost;
        Debug.Log($"[CharacterInfo] 마나 사용: -{cost} | 현재 Mana: {currentMana}/{maxMana}");
        NotifyChanged();
        return true;
    }

    private void NotifyChanged()
    {
        OnStatsChanged?.Invoke();
    }
}