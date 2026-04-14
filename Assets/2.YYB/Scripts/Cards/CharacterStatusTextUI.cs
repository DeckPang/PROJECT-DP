using TMPro;
using UnityEngine;

public class CharacterStatusTextUI : MonoBehaviour
{
    [SerializeField] private CharacterInfo characterInfo;
    [SerializeField] private TMP_Text statusText;

    private void OnEnable()
    {
        if (characterInfo != null)
            characterInfo.OnStatsChanged += Refresh;
    }

    private void OnDisable()
    {
        if (characterInfo != null)
            characterInfo.OnStatsChanged -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (characterInfo == null || statusText == null)
            return;

        statusText.text =
            $"이름: {characterInfo.UserName}\n" +
            $"HP: {characterInfo.Hp}\n" +
            $"코인: {characterInfo.Coins}\n" +
            $"트로피: {characterInfo.Trophies}\n" +
            $"마나: {characterInfo.CurrentMana}/{characterInfo.MaxMana}";
    }
}