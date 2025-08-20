using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic; // List�� ����ϱ� ���� �߰�
using Unity.Cinemachine;
using UnityEngine;

public class GenericCutsceneTrigger : MonoBehaviour
{
    [Header("������ �ƾ� ���� ���")]
    [Tooltip("���⿡ �ƾ� ���۵��� ������� �߰��ϼ���.")]
    public List<CutsceneAction> actions;

    [Header("�ʼ� ������Ʈ")]
    [SerializeField] private PlayerController player; // �÷��̾� ����

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(PlayCutscene());
            GetComponent<Collider2D>().enabled = false; // �ߺ� ���� ����
        }
    }

    private IEnumerator PlayCutscene()
    {
        if (player != null)
        {
            player.DisableControl(); // �÷��̾� ���� ��Ȱ��ȭ
        }

        // actions ����Ʈ�� �ִ� ��� ���۵��� ������� ����
        foreach (var action in actions)
        {
            // action.actionType�� ���� �ٸ� �۾��� ����
            switch (action.actionType)
            {
                case ActionType.ChangeCamera:
                    CutSceneManager.Instance.ChangeCamera(action.cameraName);
                    break;

                case ActionType.MovePlayer:
                    if (player != null && action.targetPosition != null)
                    {
                        // �÷��̾� �̵� �ڷ�ƾ�� ���� ������ ���
                        yield return player.StartMoveToPosition(action.targetPosition.position);
                    }
                    break;

                case ActionType.Wait:
                    yield return new WaitForSeconds(action.duration);
                    break;

                case ActionType.ActivateGameObject:
                    if (action.targetObject != null)
                    {
                        action.targetObject.SetActive(action.activationState);
                    }
                    break;
            }
        }

        if (player != null)
        {
            player.EnableControl(); // �÷��̾� ���� Ȱ��ȭ
        }
    }
}
