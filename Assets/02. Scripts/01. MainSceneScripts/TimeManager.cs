using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    private int slowMotionCounter = 0; // ���ο� ��� ��û ī����

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RequestSlowMotion(float duration, float timeScale)
    {
        // ù��° ���ο� ��� ��û�� ���� �ð��� ������ ��
        if (slowMotionCounter == 0)
        {
            Time.timeScale = timeScale;
        }

        // ī���� ���� �� ���� �ڷ�ƾ ����
        slowMotionCounter++;
        StartCoroutine(RestoreTimeScaleCoroutine(duration));
    }

    // �������� �ð�(���� �ð� ����) �Ŀ� ī���͸� ���̰� �ð��� �����ϴ� �ڷ�ƾ
    private IEnumerator RestoreTimeScaleCoroutine(float duration)
    {
        // Time.timeScale�� ������ ���� �ʴ� ���� �ð� �������� ��ٸ�
        yield return new WaitForSecondsRealtime(duration);

        slowMotionCounter--;

        // ��� ���ο� ��� ��û�� ������ ���� �ð��� ������� ����
        if (slowMotionCounter == 0)
        {
            Time.timeScale = 1f; // ���� �ð����� ����
        }
    }
}
