using System.Collections;
using UnityEngine;

public class ShieldSpawn : MonoBehaviour
{
    public ShieldData data;

    [Header("Shield Object")]
    public GameObject shield_obj;

    private bool canReflect = false;
    private float currentCoolDown = 0f; // SO 데이터를 직접 건드리지 않기 위한 내부 변수

    // UI나 다른 클래스에서 쿨타임 비율을 확인할 수 있게 프로퍼티 제공 (선택 사항)
    public float CoolDownProgress => Mathf.Clamp01(currentCoolDown / data.CoolDown);

    void Start()
    {
        currentCoolDown = data.CoolDown; // 시작 시 바로 사용 가능하게 설정
        canReflect = true;
        shield_obj.SetActive(false);
    }

    void Update()
    {
        // 1. 스킬 사용 로직
        if (canReflect && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(ShieldRoutine());
        }

        // 2. 쿨타임 계산 로직 (내부 변수 활용)
        if (!canReflect)
        {
            currentCoolDown += Time.deltaTime;
            if (currentCoolDown >= data.CoolDown)
            {
                canReflect = true;
            }
        }
    }

    IEnumerator ShieldRoutine()
    {
        canReflect = false;
        currentCoolDown = 0f;

        shield_obj.SetActive(true);
        yield return new WaitForSeconds(2f); // 쉴드 유지 시간
        shield_obj.SetActive(false);
    }
}