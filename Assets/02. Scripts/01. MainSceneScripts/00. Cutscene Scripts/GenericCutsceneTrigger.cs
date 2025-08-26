using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericCutsceneTrigger : MonoBehaviour
{
    [Header("컷신 종료 후 닫을 문")]
    [Tooltip("컷신이 끝난 후 닫을 문이 있다면 여기에 등록하세요.")]
    public List<DoorScript> doors = new List<DoorScript>(); // 컷신 종료 후 닫을 문들의 리스트

    [Header("수행할 컷신 액션 목록")]
    public List<CutsceneAction> actions;

    [Header("필수 오브젝트")]
    [SerializeField] private PlayerController player;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("'Player' 오브젝트가 할당되지 않았습니다!", this);
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

    public IEnumerator PlayCutscene()
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

        // 모든 액션이 끝나면 EndCutscene 함수를 호출합니다.
        EndCutscene();
    }

    // 컷신 종료를 처리하는 함수
    private void EndCutscene()
    {
        if (player != null)
        {
            player.EnableControl(); // 플레이어 조작 활성화
        }

        // 'doors' 리스트에 있는 모든 문들의 닫기를 시도합니다.
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