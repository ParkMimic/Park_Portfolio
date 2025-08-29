using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackHitbox : MonoBehaviour
{
    private float attackDamage;
    private List<Collider2D> hittedColliders;

    private void Awake()
    {
        // 중복 타격을 방지하기 위한 리스트 초기화
        hittedColliders = new List<Collider2D>();
    }

    private void Start()
    {
        // PlayerManager가 없을 경우에 대한 예외 처리
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("PlayerManager 인스턴스가 없습니다. PlayerAttackHitbox가 제대로 작동하지 않을 수 있습니다.");
            attackDamage = 1; // 기본값 설정
        }
    }

    // 히트박스 오브젝트가 활성화될 때마다 호출됩니다.
    // PlayerController에서 ActivateAttackHitbox()로 히트박스를 켤 때마다 실행됩니다.
    private void OnEnable()
    {
        // 새로운 공격이 시작되었으므로, 이전에 맞았던 적들의 목록을 초기화합니다.
        if (hittedColliders != null)
        {
            hittedColliders.Clear();
        }

        // 공격이 시작될 때마다 최신 공격력 정보를 가져옵니다.
        // 이렇게 하면 게임 도중에 공격력이 변해도 즉시 반영됩니다.
        if (PlayerManager.Instance != null)
        {
            attackDamage = PlayerManager.Instance.Damage;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 몬스터 태그를 가지고 있고, 이번 공격에서 아직 맞은 적이 아니라면
        if (other.CompareTag("Monster") && !hittedColliders.Contains(other))
        {
            MonsterController monster = other.GetComponent<MonsterController>();
            BossController boss = other.GetComponent<BossController>();

            if (monster != null)
            {
                // 1. 데미지 처리
                Vector2 knockbackDirection = (monster.transform.position - transform.position).normalized;
                monster.TakeDamage(attackDamage, knockbackDirection);

                // 2. 맞은 적 목록에 추가하여 중복 데미지 방지
                hittedColliders.Add(other);
            }
            else if (boss != null)
            {
                // 보스에게도 동일한 방식으로 데미지를 적용합니다.
                BossManager.Instance.TakeDamage((int)attackDamage);
            }
        }
    }
}