using UnityEngine;
using Unity.Cinemachine;


///<summary>
/// 컷신에서 수행할 동작의 종류
/// </summary>
/// 
public enum ActionType
{
    ChangeCamera, // 카메라 변경
    MovePlayer, // 플레이어 이동
    Wait, // 지정된 시간만큼 대기
    ActivateGameObject // 게임 오브젝트 활성화/비활성화
}

/// <summary>
/// 컷씬의 개별 동작 하나를 정의하는 클래스
///</summary>
[System.Serializable]
public class CutsceneAction
{
    public ActionType actionType; // 동작의 종류

    [Header("동작별 파라미터")]
    // 각 동작에 필요한 변수들을 선언합니다.
    public string cameraName;
    public Transform targetPosition;
    public float duration;
    public GameObject targetObject;
    public bool activationState;
}
