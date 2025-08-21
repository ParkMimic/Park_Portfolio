using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericCutsceneTrigger : MonoBehaviour
{
    // 이름 충돌 방지를 위해 클래스 정의를 안으로 이동
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

    [Header("제어할 문 목록")]
    [Tooltip("컷신이 끝난 후 닫을 문들을 여기에 연결하세요.")]
    public List<DoorScript> doors = new List<DoorScript>(); // 단일 문에서 문 리스트로 변경

    [Header("실행할 컷씬 동작 목록")]
    public List<CutsceneAction> actions;

    [Header("필수 컴포넌트")]
    [SerializeField] private PlayerController player;

    private void Start()
    {
        if (doors == null || doors.Count == 0)
        {
            Debug.LogWarning("'Doors' 리스트가 비어있습니다! 이 컷신은 문을 닫지 않습니다.", this);
        }
        if (player == null)
        {
            Debug.LogError("'Player' 변수가 할당되지 않았습니다!", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(PlayCutscene());
            GetComponent<Collider2D>().enabled = false; // 중복 실행 방지
        }
    }

    private IEnumerator PlayCutscene()
    {
        if (player != null)
        {
            player.DisableControl(); // 플레이어 조작 비활성화
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

        // 모든 액션이 끝나면 EndCutscene 함수를 호출합니다.
        EndCutscene();
    }

    // 컷신 종료 로직을 처리하는 함수
    private void EndCutscene()
    {
        Debug.Log("컷신 종료. 문 닫기를 시도합니다.");

        if (player != null)
        {
            player.EnableControl(); // 플레이어 조작 활성화
        }

        // 'doors' 리스트에 있는 모든 문에게 닫으라는 신호를 보냅니다.
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