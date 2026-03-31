using Fusion;
using UnityEngine;

// INetworkInput을 상속받아야 퓨전 서버가 이 구조체를 택배로 인식
public struct NetworkInputData : INetworkInput
{
    public Vector3 direction; // 어느 방향으로 갈지 담는 변수
}