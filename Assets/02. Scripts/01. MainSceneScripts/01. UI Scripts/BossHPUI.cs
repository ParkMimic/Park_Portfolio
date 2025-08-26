using UnityEngine;
using UnityEngine.UI;

public class BossHPUI : MonoBehaviour
{
    [SerializeField] private Image bossHPImage;

    private void OnEnable()
    {
        // BossManager의 체력 변경 이벤트가 발생할 때마다 UpdateBossHP 함수를 호출하도록 등록합니다.
        BossManager.OnHealthChanged += UpdateBossHP;
    }

    private void OnDisable()
    {
        // 오브젝트가 비활성화될 때 이벤트 구독을 취소하여 메모리 누수를 방지합니다.
        BossManager.OnHealthChanged -= UpdateBossHP;
    }

    public void UpdateBossHP(int currentHP, int maxHP)
    {
        // maxHP가 0일 경우 나누기 오류를 방지합니다.
        if (maxHP > 0)
        {
            float ratio = (float)currentHP / maxHP;
            bossHPImage.fillAmount = ratio;
        }
    }
}
