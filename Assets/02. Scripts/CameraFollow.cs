using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float followSpeed = 0.05f; // ī�޶� ���󰡴� �ӵ�

    [SerializeField] private Vector3 offset;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.transform == null)
        {
            Debug.LogWarning("PlayerManager �ν��Ͻ��� ���ų� �÷��̾� Transform�� �����ϴ�.");
            return;
        }
        Vector3 targetPosition = PlayerManager.Instance.transform.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, followSpeed);

        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, offset.z);
    }
}
