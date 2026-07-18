using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class KeyWord : MonoBehaviour
{

    [SerializeField]
    private KeyWordSettings setting;

    [Header("Player")]
    public Transform player;            //Player 위치

    [Header("UI")]
    public GameObject KeyWordPrefabs;   //화면에 나타날 단어 UI 
    public Transform uiDistance;        //기준 위치


    public int CurrentCount = 0;
    private int index = 0;

    Queue<KeyCode> keyQueue = new Queue<KeyCode>();
    Queue<GameObject> uiQueue = new Queue<GameObject>();
    KeyCode[] keys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < setting.StartCount; i++)
        {
            AddKey();
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (keyQueue.Count == 0) return;

        if (Input.GetKeyDown(keyQueue.Peek()))
        {
            RemoveKey();

            MoveForward();

            CurrentCount++;
            if (CurrentCount + setting.StartCount <= setting.TotalCount)
            {
                AddKey();
            }
            else if (CurrentCount >= setting.TotalCount)
            {
                Debug.Log("Win");
            }
        }
    }



    void AddKey() // 뒤에 붙일 Key
    {

        KeyCode new_key = keys[Random.Range(0, keys.Length)]; //WASD중 랜덤하게 저장
        keyQueue.Enqueue(new_key); // 그 값을 뒤에 붙임

        GameObject obj = Instantiate(KeyWordPrefabs); // UIPrefab을 Distance만큼 거리를 주고 생성
        obj.transform.SetParent(uiDistance, false);
        obj.GetComponent<TMP_Text>().text = new_key.ToString();       // 어떤 글자인지 가져오기

        obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(index * setting.Spacing, 0);

        index++;

        uiQueue.Enqueue(obj); // 생성한 UI도 뒤에 붙임

    }

    void RemoveKey() // Player가 맞춘 KeyWord를 제거
    {
        keyQueue.Dequeue(); // 앞에 단어 제거

        GameObject obj = uiQueue.Dequeue();
        Destroy(obj); // UI도 제거

    }

    void MoveForward()
    {
        player.position += Vector3.right * setting.moveDistance;
    }

}
