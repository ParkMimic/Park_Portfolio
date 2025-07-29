using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float followSpeed = 0.05f; // 카메라가 따라가는 속도

    [SerializeField] private Vector3 offset;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.transform == null)
        {
            Debug.LogWarning("PlayerManager 인스턴스가 없거나 플레이어 Transform이 없습니다.");
            return;
        }
        Vector3 targetPosition = PlayerManager.Instance.transform.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, followSpeed);

        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, offset.z);
    }
}
