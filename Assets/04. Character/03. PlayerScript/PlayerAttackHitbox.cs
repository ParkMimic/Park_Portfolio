using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    private float attackDamage;// �� ���� ������ �� ������

    private void Awake()
    {
        if (PlayerManager.Instance != null)
        {
            attackDamage = PlayerManager.Instance.Damage; // PlayerManager���� ���� �������� �����ɴϴ�.
        }
        else
        {
            Debug.LogError("PlayerManager �ν��Ͻ��� �����ϴ�. PlayerAttackHitbox ��ũ��Ʈ�� ����� �۵����� ���� �� �ֽ��ϴ�.");
            attackDamage = 1; // �⺻�� ����
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // �浹�� ������Ʈ�� MonsterController ������Ʈ�� ������ �ִ��� Ȯ��
        MonsterController monster = other.GetComponent<MonsterController>();

        // ���� MonsterController ������Ʈ�� ã�Ҵٸ�
        if (monster != null)
        {
            // ������ TakeDamage �Լ��� ȣ���Ͽ� �������� �ݴϴ�.
            monster.TakeDamage(attackDamage);
            Debug.Log($"���Ϳ��� {attackDamage} ������� �������ϴ�!");
        }
    }
}
