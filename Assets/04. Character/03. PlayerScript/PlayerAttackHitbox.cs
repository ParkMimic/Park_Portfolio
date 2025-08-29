using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackHitbox : MonoBehaviour
{
    private float attackDamage;
    private List<Collider2D> hittedColliders;

    private void Awake()
    {
        // �ߺ� Ÿ���� �����ϱ� ���� ����Ʈ �ʱ�ȭ
        hittedColliders = new List<Collider2D>();
    }

    private void Start()
    {
        // PlayerManager�� ���� ��쿡 ���� ���� ó��
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("PlayerManager �ν��Ͻ��� �����ϴ�. PlayerAttackHitbox�� ����� �۵����� ���� �� �ֽ��ϴ�.");
            attackDamage = 1; // �⺻�� ����
        }
    }

    // ��Ʈ�ڽ� ������Ʈ�� Ȱ��ȭ�� ������ ȣ��˴ϴ�.
    // PlayerController���� ActivateAttackHitbox()�� ��Ʈ�ڽ��� �� ������ ����˴ϴ�.
    private void OnEnable()
    {
        // ���ο� ������ ���۵Ǿ����Ƿ�, ������ �¾Ҵ� ������ ����� �ʱ�ȭ�մϴ�.
        if (hittedColliders != null)
        {
            hittedColliders.Clear();
        }

        // ������ ���۵� ������ �ֽ� ���ݷ� ������ �����ɴϴ�.
        // �̷��� �ϸ� ���� ���߿� ���ݷ��� ���ص� ��� �ݿ��˴ϴ�.
        if (PlayerManager.Instance != null)
        {
            attackDamage = PlayerManager.Instance.Damage;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ���� �±׸� ������ �ְ�, �̹� ���ݿ��� ���� ���� ���� �ƴ϶��
        if (other.CompareTag("Monster") && !hittedColliders.Contains(other))
        {
            MonsterController monster = other.GetComponent<MonsterController>();
            BossController boss = other.GetComponent<BossController>();

            if (monster != null)
            {
                // 1. ������ ó��
                Vector2 knockbackDirection = (monster.transform.position - transform.position).normalized;
                monster.TakeDamage(attackDamage, knockbackDirection);

                // 2. ���� �� ��Ͽ� �߰��Ͽ� �ߺ� ������ ����
                hittedColliders.Add(other);
            }
            else if (boss != null)
            {
                // �������Ե� ������ ������� �������� �����մϴ�.
                BossManager.Instance.TakeDamage((int)attackDamage);
            }
        }
    }
}