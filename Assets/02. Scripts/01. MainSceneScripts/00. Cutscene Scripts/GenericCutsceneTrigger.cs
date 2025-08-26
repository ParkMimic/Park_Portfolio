using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericCutsceneTrigger : MonoBehaviour
{
    [Header("�ƽ� ���� �� ���� ��")]
    [Tooltip("�ƽ��� ���� �� ���� ���� �ִٸ� ���⿡ ����ϼ���.")]
    public List<DoorScript> doors = new List<DoorScript>(); // �ƽ� ���� �� ���� ������ ����Ʈ

    [Header("������ �ƽ� �׼� ���")]
    public List<CutsceneAction> actions;

    [Header("�ʼ� ������Ʈ")]
    [SerializeField] private PlayerController player;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("'Player' ������Ʈ�� �Ҵ���� �ʾҽ��ϴ�!", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(PlayCutscene());
            GetComponent<Collider2D>().enabled = false; // �ߺ� ���� ����
        }
    }

    public IEnumerator PlayCutscene()
    {
        if (player != null)
        {
            player.DisableControl(); // �÷��̾� ���� ��Ȱ��ȭ
        }

        foreach (var action in actions)
        {
            switch (action.actionType)
            {
                case ActionType.ChangeCamera:
                    CutSceneManager.Instance.ChangeCamera(action.cameraName);
                    break;

                case ActionType.MovePlayer:
                    if (player != null && action.targetPosition != null)
                    {
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

                case ActionType.PauseGame:
                    if (action.pauseGame == true)
                    {
                        Time.timeScale = 0f;
                    }
                    else
                    {
                        Time.timeScale = 1f;
                    }
                    break;

                case ActionType.ActivateCollider:
                    if (action.targetCollider != null)
                    {
                        action.targetCollider.GetComponent<Collider2D>().enabled = action.activationState;
                    }
                    break;
            }
        }

        // ��� �׼��� ������ EndCutscene �Լ��� ȣ���մϴ�.
        EndCutscene();
    }

    // �ƽ� ���Ḧ ó���ϴ� �Լ�
    private void EndCutscene()
    {
        if (player != null)
        {
            player.EnableControl(); // �÷��̾� ���� Ȱ��ȭ
        }

        // 'doors' ����Ʈ�� �ִ� ��� ������ �ݱ⸦ �õ��մϴ�.
        if (doors != null && doors.Count > 0)
        {
            foreach (var door in doors)
            {
                if (door != null)
                {
                    door.StartClosing();
                }
            }
        }
    }
}