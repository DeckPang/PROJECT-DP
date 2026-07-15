using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RPS_LogUi : MonoBehaviour
{
    public TMP_Text logText;
    public int maxline = 12;

    private List<string> lines = new List<string>();
    private Coroutine clearRoutine;

    public void Log(string text)
    {
        lines.Add(text);
        if (lines.Count > maxline)
            lines.RemoveAt(0);

        logText.text = string.Join("\n", lines);
    }

    public void Clear()
    {
        lines.Clear();
        logText.text = "";
    }

    // delay초 뒤에 자동으로 Clear.
    // 호출 도중 또 호출되면 이전 대기는 취소하고 새로 시작 (마지막 Log 기준으로 delay초 후 삭제됨)
    public void ClearAfterDelay(float delay)
    {
        if (clearRoutine != null)
            StopCoroutine(clearRoutine);

        clearRoutine = StartCoroutine(ClearRoutine(delay));
    }

    private IEnumerator ClearRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Clear();
        clearRoutine = null;
    }
}