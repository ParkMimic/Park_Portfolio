using System.Runtime.CompilerServices;
using System.Collections;
using UnityEngine;

public class TriggerEvent : MonoBehaviour
{
    [Header("필수 컴포넌트")]
    [SerializeField] private PlayerController player;
    [SerializeField] private GameObject boss;

    [Header("컷씬 설정")]
    [SerializeField] private Transform playerMoveTarget; // 1. 플레이어가 이동할 목표 위치
    [SerializeField] private string cutsceneCameraName1; // 2. 컷씬에서 사용할 카메라 이름
    [SerializeField] private string cutsceneCameraName2; // 2. 컷씬에서 사용할 카메라 이름
    [SerializeField] private float pauseDuration = 2f; // 3. 컷씬 카메라를 보여줄 시간
    [SerializeField] private string returnCameraName; // 4. 돌아올 플레이어 추적 카메라 이름

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 컷신 코루틴을 시작합니다ㅣ.
            StartCoroutine(PlayCutscene());
            // 트리거는 한번만 작동하도록 즉시 비활성화
            gameObject.GetComponent<Collider2D>().enabled = false;
        }
    }

    private IEnumerator PlayCutscene()
    {
        // 1. 플레이어 조작을 막고, 지정된 위치로 이동 시작
        player.DisableControl();
        yield return player.StartMoveToPosition(playerMoveTarget.position);
        // player.StartMoveToPosition 코루틴이 끝날 때까지 여기서 대기합니다.

        // 2-1. 컷씬 카메라로 전환
        CutSceneManager.Instance.ChangeCamera(cutsceneCameraName1);

        // 3-1. 지정된 시간만큼 대기
        yield return new WaitForSeconds(pauseDuration);

        // 2-2. 컷씬 카메라로 전환
        CutSceneManager.Instance.ChangeCamera(cutsceneCameraName2);

        // 3-2. 지정된 시간만큼 대기
        yield return new WaitForSeconds(pauseDuration);

        // 4. 원래 카메라로 복귀
        CutSceneManager.Instance.ChangeCamera(returnCameraName);

        // 5. 플레이어 조작을 다시 활성화
        player.EnableControl();
    }
}
