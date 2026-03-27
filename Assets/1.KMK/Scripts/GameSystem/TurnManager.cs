using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public int totalPlayers = 4;
    public int currentPlayerIndex = 0; // 0=1P, 1=2P, 2=3P, 3=4P

    // 게임 맨 처음 시작할 때 초기화
    public void InitGame()
    {
        currentPlayerIndex = 0;
        Debug.Log("--- Game Start ---");
    }

    // 다음 턴으로 넘기기
    public void NextTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % totalPlayers;
        Debug.Log($"\n[ {currentPlayerIndex + 1}P 의 턴 시작! ]");

        // TODO: 나중에 여기에 이벤트 추가하기
    }

    // 지금 누구 턴인지 알려주는 함수
    public int GetCurrentPlayer()
    {
        return currentPlayerIndex;
    }
}