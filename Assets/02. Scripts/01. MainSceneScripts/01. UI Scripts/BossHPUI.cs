using UnityEngine;
using UnityEngine.UI;

public class BossHPUI : MonoBehaviour
{
    [SerializeField] private Image bossHPImage;
    [SerializeField] private Image bossGroggyImage;

    private void OnEnable()
    {
        // BossManager�� ü�� ���� �̺�Ʈ�� �߻��� ������ UpdateBossHP �Լ��� ȣ���ϵ��� ����մϴ�.
        BossManager.OnHealthChanged += UpdateBossHP;
        BossManager.OnGroggyChanged += UpdateBossGroggy;
    }

    private void OnDisable()
    {
        // ������Ʈ�� ��Ȱ��ȭ�� �� �̺�Ʈ ������ ����Ͽ� �޸� ������ �����մϴ�.
        BossManager.OnHealthChanged -= UpdateBossHP;
        BossManager.OnGroggyChanged -= UpdateBossGroggy;
    }

    public void UpdateBossHP(int currentHP, int maxHP)
    {
        // maxHP�� 0�� ��� ������ ������ �����մϴ�.
        if (maxHP > 0)
        {
            float ratio = (float)currentHP / maxHP;
            bossHPImage.fillAmount = ratio;
        }
    }

    public void UpdateBossGroggy(float currentGroggy, float maxGroggy)
    {
        // maxGroggy�� 0�� ��� ������ ������ �����մϴ�.
        if (maxGroggy > 0)
        {
            float ratio = currentGroggy / maxGroggy;
            bossGroggyImage.fillAmount = ratio;
        }
    }
}
