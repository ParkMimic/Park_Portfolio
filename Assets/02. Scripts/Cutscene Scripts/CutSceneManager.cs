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

    [Header("������ �ó׸ӽ� ī�޶� ���")]
    [SerializeField] private CinemachineCamera[] cutsceneCameras; // �ƾ� ī�޶� �迭

    /// <summary>
    /// ������ �̸��� �ó׸ӽ� ī�޶� Ȱ��ȭ�մϴ�.
    /// </summary>
    /// <param name="cameraName">Ȱ��ȭ�� ī�޶��� ���� ������Ʈ �̸�</param>
    public void ChangeCamera(string cameraName)
    {
        // �迭�� �ִ� ��� ī�޶� ��ȸ�մϴ�.
        foreach (var cam in cutsceneCameras)
        {
            // ī�޶� �̸��� ��ġ�ϴ��� Ȯ���մϴ�.
            if (cam.gameObject.name == cameraName)
            {
                // �̸��� ��ġ�ϴ� ī�޶�� ���� �켱 ������ �ο��Ͽ� Ȱ��ȭ ��ŵ�ϴ�.
                cam.Priority = 100;
            }
            else
            {
                // �� �� ��� ī�޶�� ���� �켱������ �ο��Ͽ� ��Ȱ��ȭ ��ŵ�ϴ�.
                cam.Priority = 10;
            }
        }
    }
}
