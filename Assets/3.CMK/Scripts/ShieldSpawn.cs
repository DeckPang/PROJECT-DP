using System.Collections;
using UnityEngine;

public class ShieldSpawn : MonoBehaviour
{
    [Header("Shield CoolTime")]
    public float CoolDown = 3.5f;
    public float SkillTime;

    [Header("Shield Object")]
    public GameObject shield_obj;

    private bool reflect = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SkillTime = 0.0f;
        shield_obj.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
       
        if (reflect && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(ShieldRoutine());

            reflect = false;
            SkillTime = 0.0f;
        }

        if (reflect == false && SkillTime < CoolDown)
        {
            SkillTime += Time.deltaTime;
            if (SkillTime > CoolDown)
            {
                reflect = true;
            }
        }
    }
    IEnumerator ShieldRoutine()
    {
        shield_obj.SetActive(true);

        yield return new WaitForSeconds(2f); // 2√  ¥Î±‚

        shield_obj.SetActive(false);
    }

}
