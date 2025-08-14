using UnityEngine;
using Unity.Cinemachine;


///<summary>
/// �ƽſ��� ������ ������ ����
/// </summary>
/// 
public enum ActionType
{
    ChangeCamera, // ī�޶� ����
    MovePlayer, // �÷��̾� �̵�
    Wait, // ������ �ð���ŭ ���
    ActivateGameObject // ���� ������Ʈ Ȱ��ȭ/��Ȱ��ȭ
}

/// <summary>
/// �ƾ��� ���� ���� �ϳ��� �����ϴ� Ŭ����
///</summary>
[System.Serializable]
public class CutsceneAction
{
    public ActionType actionType; // ������ ����

    [Header("���ۺ� �Ķ����")]
    // �� ���ۿ� �ʿ��� �������� �����մϴ�.
    public string cameraName;
    public Transform targetPosition;
    public float duration;
    public GameObject targetObject;
    public bool activationState;
}
