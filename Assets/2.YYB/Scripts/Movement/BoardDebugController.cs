using UnityEngine;

public class BoardDebugController : MonoBehaviour
{
    [SerializeField] private BoardGenerator boardGenerator;
    [SerializeField] private PlayerPawn playerPawn;
    [SerializeField] private int minRandomStep = 0;
    [SerializeField] private int maxRandomStep = 10;

    private void Start()
    {
        Debug.Log("=== Board Debug Controller ===");
        Debug.Log("1 : 1Ä­ ÀÌµ¿");
        Debug.Log("3 : 3Ä­ ÀÌµ¿");
        Debug.Log("5 : 5Ä­ ÀÌµ¿");
        Debug.Log("Space : ·£´ý ÀÌµ¿");
        Debug.Log("B : ´ÙÀ½ Ä­¿¡ BananaPeel ¼³Ä¡");
        Debug.Log("F : ´ÙÀ½ Ä­¿¡ FakeTrophy ¼³Ä¡");
        Debug.Log("C : ´ÙÀ½ Ä­ ÇÔÁ¤ Á¦°Å");
    }

    private void Update()
    {
        if (boardGenerator == null || playerPawn == null)
            return;

        if (playerPawn.IsMoving)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            playerPawn.MoveBySteps(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            playerPawn.MoveBySteps(3);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            playerPawn.MoveBySteps(5);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            int randomStep = Random.Range(minRandomStep, maxRandomStep + 1);
            Debug.Log($"[Debug] ·£´ý ÀÌµ¿°ª = {randomStep}");
            playerPawn.MoveBySteps(randomStep);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            InstallTrapOnNextNode(TrapType.BananaPeel);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            InstallTrapOnNextNode(TrapType.FakeTrophy);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearTrapOnNextNode();
        }
    }

    private void InstallTrapOnNextNode(TrapType trapType)
    {
        if (boardGenerator.NodeCount == 0)
            return;

        int targetIndex = BoardMovementService.GetDestinationIndex(playerPawn.CurrentNodeIndex, 1, boardGenerator.NodeCount);
        BoardNode targetNode = boardGenerator.GetNode(targetIndex);

        if (targetNode == null)
            return;

        targetNode.InstallTrap(trapType, playerPawn.PawnName);
    }

    private void ClearTrapOnNextNode()
    {
        if (boardGenerator.NodeCount == 0)
            return;

        int targetIndex = BoardMovementService.GetDestinationIndex(playerPawn.CurrentNodeIndex, 1, boardGenerator.NodeCount);
        BoardNode targetNode = boardGenerator.GetNode(targetIndex);

        if (targetNode == null)
            return;

        targetNode.ClearTrap();
    }
}