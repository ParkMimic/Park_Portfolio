using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericCutsceneTrigger : MonoBehaviour
{
    // �̸� �浹 ������ ���� Ŭ���� ���Ǹ� ������ �̵�
    [System.Serializable]
    public class CutsceneAction
    {
        public ActionType actionType;
        public string cameraName;
        public Transform targetPosition;
        public float duration;
        public GameObject targetObject;
        public bool activationState;
    }

    public enum ActionType
    {
        ChangeCamera,
        MovePlayer,
        Wait,
        ActivateGameObject
    }

    [Header("������ �� ���")]
    [Tooltip("�ƽ��� ���� �� ���� ������ ���⿡ �����ϼ���.")]
    public List<DoorScript> doors = new List<DoorScript>(); // ���� ������ �� ����Ʈ�� ����

    [Header("������ �ƾ� ���� ���")]
    public List<CutsceneAction> actions;

    [Header("�ʼ� ������Ʈ")]
    [SerializeField] private PlayerController player;

    private void Start()
    {
        if (doors == null || doors.Count == 0)
        {
            Debug.LogWarning("'Doors' ����Ʈ�� ����ֽ��ϴ�! �� �ƽ��� ���� ���� �ʽ��ϴ�.", this);
        }
        if (player == null)
        {
            Debug.LogError("'Player' ������ �Ҵ���� �ʾҽ��ϴ�!", this);
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

    private IEnumerator PlayCutscene()
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
            }
        }

        // ��� �׼��� ������ EndCutscene �Լ��� ȣ���մϴ�.
        EndCutscene();
    }

    // �ƽ� ���� ������ ó���ϴ� �Լ�
    private void EndCutscene()
    {
        Debug.Log("�ƽ� ����. �� �ݱ⸦ �õ��մϴ�.");

        if (player != null)
        {
            player.EnableControl(); // �÷��̾� ���� Ȱ��ȭ
        }

        // 'doors' ����Ʈ�� �ִ� ��� ������ ������� ��ȣ�� �����ϴ�.
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