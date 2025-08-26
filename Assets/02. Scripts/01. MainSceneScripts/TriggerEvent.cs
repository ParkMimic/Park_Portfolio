using System.Runtime.CompilerServices;
using System.Collections;
using UnityEngine;

public class TriggerEvent : MonoBehaviour
{
    [Header("�ʼ� ������Ʈ")]
    [SerializeField] private PlayerController player;
    [SerializeField] private GameObject boss;

    [Header("�ƾ� ����")]
    [SerializeField] private Transform playerMoveTarget; // 1. �÷��̾ �̵��� ��ǥ ��ġ
    [SerializeField] private string cutsceneCameraName1; // 2. �ƾ����� ����� ī�޶� �̸�
    [SerializeField] private string cutsceneCameraName2; // 2. �ƾ����� ����� ī�޶� �̸�
    [SerializeField] private float pauseDuration = 2f; // 3. �ƾ� ī�޶� ������ �ð�
    [SerializeField] private string returnCameraName; // 4. ���ƿ� �÷��̾� ���� ī�޶� �̸�

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // �ƽ� �ڷ�ƾ�� �����մϴ٤�.
            StartCoroutine(PlayCutscene());
            // Ʈ���Ŵ� �ѹ��� �۵��ϵ��� ��� ��Ȱ��ȭ
            gameObject.GetComponent<Collider2D>().enabled = false;
        }
    }

    private IEnumerator PlayCutscene()
    {
        // 1. �÷��̾� ������ ����, ������ ��ġ�� �̵� ����
        player.DisableControl();
        yield return player.StartMoveToPosition(playerMoveTarget.position);
        // player.StartMoveToPosition �ڷ�ƾ�� ���� ������ ���⼭ ����մϴ�.

        // 2-1. �ƾ� ī�޶�� ��ȯ
        CutSceneManager.Instance.ChangeCamera(cutsceneCameraName1);

        // 3-1. ������ �ð���ŭ ���
        yield return new WaitForSeconds(pauseDuration);

        // 2-2. �ƾ� ī�޶�� ��ȯ
        CutSceneManager.Instance.ChangeCamera(cutsceneCameraName2);

        // 3-2. ������ �ð���ŭ ���
        yield return new WaitForSeconds(pauseDuration);

        // 4. ���� ī�޶�� ����
        CutSceneManager.Instance.ChangeCamera(returnCameraName);

        // 5. �÷��̾� ������ �ٽ� Ȱ��ȭ
        player.EnableControl();
    }
}
