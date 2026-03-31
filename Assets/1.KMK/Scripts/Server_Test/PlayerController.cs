using UnityEngine;
using Fusion;

public class PlayerController : NetworkBehaviour // 팩트 체크: 멀티는 MonoBehaviour가 아니라 NetworkBehaviour!
{
    public float moveSpeed = 5f;

    // Update 대신 퓨전 전용 FixedUpdateNetwork를 써야 서버 틱(Tick)이 완벽하게 맞아떨어져
    public override void FixedUpdateNetwork()
    {
        // 방금 BasicSpawner에서 보낸 '택배(키보드 입력)'가 무사히 도착했는지 확인!
        if (GetInput(out NetworkInputData data))
        {
            // 방향키 데이터가 들어왔다면 그 방향으로 이동시켜!
            // Time.deltaTime 대신 서버의 절대 시간을 맞추는 Runner.DeltaTime을 무조건 사용해야 해.
            transform.position += data.direction * moveSpeed * Runner.DeltaTime;
        }
    }
}