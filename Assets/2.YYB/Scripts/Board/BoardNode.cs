using UnityEngine;

public class BoardNode : MonoBehaviour
{
    [Header("Node Data")]
    [SerializeField] private int index;
    [SerializeField] private BoardNodeType nodeType;
    [SerializeField] private TrapType installedTrap = TrapType.None;

    public int Index => index;
    public BoardNodeType NodeType => nodeType;
    public TrapType InstalledTrap => installedTrap;

    public void Initialize(int newIndex, BoardNodeType newType)
    {
        index = newIndex;
        nodeType = newType;
        installedTrap = TrapType.None;

        gameObject.name = $"Node_{index:D2}_{nodeType}";
    }

    public void InstallTrap(TrapType trapType, string installerName)
    {
        installedTrap = trapType;
        Debug.Log($"[Node {index}] {installerName} 이(가) {trapType} 함정을 설치했습니다.");
    }

    public void ClearTrap()
    {
        installedTrap = TrapType.None;
        Debug.Log($"[Node {index}] 설치된 함정을 제거했습니다.");
    }

    public void OnPlayerArrived(PlayerPawn pawn)
    {
        Debug.Log($"[Node {index}] {pawn.PawnName} 도착 | NodeType = {nodeType}");

        LogNodeEffect(pawn);
        ResolveTrap(pawn);
    }

    private void LogNodeEffect(PlayerPawn pawn)
    {
        switch (nodeType)
        {
            case BoardNodeType.Start:
                Debug.Log($"[Node {index}] 출발지 도착/통과 처리 위치입니다. (예: 최대 마나 +1)");
                break;

            case BoardNodeType.Resource:
                Debug.Log($"[Node {index}] 재화 획득/손실 처리 위치입니다.");
                break;

            case BoardNodeType.CardDraw:
                Debug.Log($"[Node {index}] 카드 드로우 처리 위치입니다.");
                break;

            case BoardNodeType.Battle:
                Debug.Log($"[Node {index}] 전투/공격 이벤트 처리 위치입니다.");
                break;

            case BoardNodeType.Trap:
                Debug.Log($"[Node {index}] 함정 관련 칸입니다.");
                break;

            case BoardNodeType.Event:
                Debug.Log($"[Node {index}] 랜덤 이벤트 처리 위치입니다.");
                break;

            case BoardNodeType.Shop:
                Debug.Log($"[Node {index}] 상점 처리 위치입니다.");
                break;

            case BoardNodeType.Jail:
                Debug.Log($"[Node {index}] 감옥 처리 위치입니다. (예: 다음 턴 스킵)");
                break;

            case BoardNodeType.Roulette:
                Debug.Log($"[Node {index}] 룰렛/랜덤 강제 이벤트 처리 위치입니다.");
                break;
        }
    }

    private void ResolveTrap(PlayerPawn pawn)
    {
        if (installedTrap == TrapType.None)
            return;

        switch (installedTrap)
        {
            case TrapType.BananaPeel:
                int backStep = Random.Range(3, 5); // 3~4칸
                Debug.Log($"[Trap] {pawn.PawnName} 이(가) 바나나 껍질을 밟았습니다. Debug 기준으로 {backStep}칸 후퇴 처리 자리입니다.");
                break;

            case TrapType.FakeTrophy:
                Debug.Log($"[Trap] {pawn.PawnName} 이(가) 가짜 트로피를 밟았습니다. Debug 기준으로 코인 5개 강탈 처리 자리입니다.");
                break;
        }

        installedTrap = TrapType.None;
        Debug.Log($"[Node {index}] 함정은 1회 발동 후 제거되었습니다.");
    }
}