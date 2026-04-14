using UnityEngine;

public static class CharacterInfoService
{
    public static void ChangeHp(CharacterInfoData data, int amount)
    {
        data.hp += amount;
        data.hp = Mathf.Max(0, data.hp);
    }

    public static void ChangeCoins(CharacterInfoData data, int amount)
    {
        data.coins += amount;
        data.coins = Mathf.Max(0, data.coins);
    }

    public static void AddTrophy(CharacterInfoData data, int amount = 1)
    {
        data.trophies += amount;
        data.trophies = Mathf.Max(0, data.trophies);
    }

    public static void IncreaseMaxMana(CharacterInfoData data, int amount)
    {
        data.maxMana += amount;
        data.maxMana = Mathf.Max(0, data.maxMana);
        data.currentMana = Mathf.Clamp(data.currentMana, 0, data.maxMana);
    }

    public static void RestoreManaToFull(CharacterInfoData data)
    {
        data.currentMana = data.maxMana;
    }

    public static void ChangeMana(CharacterInfoData data, int amount)
    {
        data.currentMana += amount;
        data.currentMana = Mathf.Clamp(data.currentMana, 0, data.maxMana);
    }

    public static bool TrySpendMana(CharacterInfoData data, int cost)
    {
        if (data.currentMana < cost)
            return false;

        data.currentMana -= cost;
        return true;
    }
}