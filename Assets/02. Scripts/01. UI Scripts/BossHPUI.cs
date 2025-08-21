using UnityEngine;
using UnityEngine.UI;

public class BossHPUI : MonoBehaviour
{
    [SerializeField] private Image bossHPImage;

    private void OnEnable()
    {
        // BossManager�� ü�� ���� �̺�Ʈ�� �߻��� ������ UpdateBossHP �Լ��� ȣ���ϵ��� ����մϴ�.
        BossManager.OnHealthChanged += UpdateBossHP;
    }

    private void OnDisable()
    {
        // ������Ʈ�� ��Ȱ��ȭ�� �� �̺�Ʈ ������ ����Ͽ� �޸� ������ �����մϴ�.
        BossManager.OnHealthChanged -= UpdateBossHP;
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
}
