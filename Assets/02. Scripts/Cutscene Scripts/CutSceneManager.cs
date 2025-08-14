using Unity.Cinemachine;
using UnityEngine;

public class CutSceneManager : MonoBehaviour
{
    public static CutSceneManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("관리할 시네머신 카메라 목록")]
    [SerializeField] private CinemachineCamera[] cutsceneCameras; // 컷씬 카메라 배열

    /// <summary>
    /// 지정된 이름의 시네머신 카메라를 활성화합니다.
    /// </summary>
    /// <param name="cameraName">활성화할 카메라의 게임 오브젝트 이름</param>
    public void ChangeCamera(string cameraName)
    {
        // 배열에 있는 모든 카메라를 순회합니다.
        foreach (var cam in cutsceneCameras)
        {
            // 카메라 이름이 일치하는지 확인합니다.
            if (cam.gameObject.name == cameraName)
            {
                // 이름이 일치하는 카메라는 높은 우선 순위를 부여하여 활성화 시킵니다.
                cam.Priority = 100;
            }
            else
            {
                // 그 외 모든 카메라는 낮은 우선순위를 부여하여 비활성화 시킵니다.
                cam.Priority = 10;
            }
        }
    }
}
