using UnityEngine;

public class DoorScript : MonoBehaviour
{
    public Transform openTransform;
    public Transform closedTransform;

    public bool isClosing = false;
    public bool isOpening = false;

    void Start()
    {
        // 게임 시작 시, 문을 열린 위치로 설정
        transform.position = openTransform.position;
    }

    void Update()
    {
        // isClosing이 true일 때만 문을 닫는 로직 실행
        if (isClosing)
        {
            transform.position = Vector3.MoveTowards(transform.position, closedTransform.position, Time.deltaTime * 10f);

            // 문이 목표 위치에 도달하면, isClosing을 false로 바꿔 이동을 멈춤
            if (transform.position == closedTransform.position)
            {
                isClosing = false;
            }
        }

        if (isOpening)
        {
            transform.position = Vector3.MoveTowards(transform.position, openTransform.position, Time.deltaTime * 10f);

            if (transform.position == openTransform.position)
            {
                isOpening = false;
            }
        }
    }

    // 문을 닫기 시작하라는 명령을 받는 public 함수
    public void StartClosing()
    {
        // 이미 닫히는 중이거나, 이미 닫혀있다면 다시 실행하지 않도록 할 수 있지만,
        // 현재 로직에서는 다시 호출해도 문제 없으므로 간단하게 유지합니다.
        isClosing = true;
        Debug.Log("문 닫기를 시작합니다.");
    }

    public void StartOpening()
    {
        isOpening = true;
        Debug.Log("문 열기를 시작합니다.");
    }
}