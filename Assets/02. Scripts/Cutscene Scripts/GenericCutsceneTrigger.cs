using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic; // List를 사용하기 위해 추가
using Unity.Cinemachine;
using UnityEngine;

public class GenericCutsceneTrigger : MonoBehaviour
{
    [Header("실행할 컷씬 동작 목록")]
    [Tooltip("여기에 컷씬 동작들을 순서대로 추가하세요.")]
    public List<CutsceneAction> actions;

    [Header("필수 컴포넌트")]
    [SerializeField] private PlayerController player; // 플레이어 참조

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

        // actions 리스트에 있는 모든 동작들을 순서대로 실행
        foreach (var action in actions)
        {
            // action.actionType에 따라 다른 작업을 수행
            switch (action.actionType)
            {
                case ActionType.ChangeCamera:
                    CutSceneManager.Instance.ChangeCamera(action.cameraName);
                    break;

                case ActionType.MovePlayer:
                    if (player != null && action.targetPosition != null)
                    {
                        // 플레이어 이동 코루틴이 끝날 때까지 대기
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
            player.EnableControl(); // 플레이어 조작 활성화
        }
    }
}
