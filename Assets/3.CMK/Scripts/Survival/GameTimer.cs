using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public float maxTime = 60f;
    private float currentTime;

    public float RemainTime => Mathf.Clamp(maxTime - currentTime, 0f, maxTime);
    public bool IsTimeOver => currentTime >= maxTime;

    void Update()
    {
        currentTime += Time.deltaTime;
    }
}
