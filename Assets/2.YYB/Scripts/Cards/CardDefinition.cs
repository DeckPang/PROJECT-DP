using UnityEngine;

[CreateAssetMenu(menuName = "Deckpang/Card Definition")]
public class CardDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string cardId;
    [SerializeField] private string cardName;
    [TextArea][SerializeField] private string description;

    [Header("Category")]
    [SerializeField] private CardType cardType;
    [SerializeField] private CardPoolType cardPoolType = CardPoolType.SharedDeck;
    [SerializeField] private int cost = 0;

    [Header("Effect")]
    [SerializeField] private CardEffectType effectType = CardEffectType.None;
    [SerializeField] private int value = 0;
    [SerializeField] private TrapType trapType = TrapType.None;

    public string CardId => cardId;
    public string CardName => string.IsNullOrWhiteSpace(cardName) ? name : cardName;
    public string Description => description;
    public CardType CardType => cardType;
    public CardPoolType CardPoolType => cardPoolType;
    public int Cost => cost;
    public CardEffectType EffectType => effectType;
    public int Value => value;
    public TrapType TrapType => trapType;

    public string SummaryText => $"{CardName} | {Cost}ÄÚ | {EffectType}";
}