using UnityEngine;

public class DoorScript : MonoBehaviour
{
    public Transform openTransform;
    public Transform closedTransform;

    private bool isClosing = false;

    void Start()
    {
        // ���� ���� ��, ���� ���� ��ġ�� ����
        transform.position = openTransform.position;
    }

    void Update()
    {
        // isClosing�� true�� ���� ���� �ݴ� ���� ����
        if (isClosing)
        {
            transform.position = Vector3.MoveTowards(transform.position, closedTransform.position, Time.deltaTime * 2f);

            // ���� ��ǥ ��ġ�� �����ϸ�, isClosing�� false�� �ٲ� �̵��� ����
            if (transform.position == closedTransform.position)
            {
                isClosing = false;
            }
        }
    }

    // ���� �ݱ� �����϶�� ����� �޴� public �Լ�
    public void StartClosing()
    {
        // �̹� ������ ���̰ų�, �̹� �����ִٸ� �ٽ� �������� �ʵ��� �� �� ������,
        // ���� ���������� �ٽ� ȣ���ص� ���� �����Ƿ� �����ϰ� �����մϴ�.
        isClosing = true;
        Debug.Log("�� �ݱ⸦ �����մϴ�.");
    }
}