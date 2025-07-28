using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    private float attackDamage;// 이 공격 판정이 줄 데미지

    private void Awake()
    {
        if (PlayerManager.Instance != null)
        {
            attackDamage = PlayerManager.Instance.Damage; // PlayerManager에서 공격 데미지를 가져옵니다.
        }
        else
        {
            Debug.LogError("PlayerManager 인스턴스가 없습니다. PlayerAttackHitbox 스크립트가 제대로 작동하지 않을 수 있습니다.");
            attackDamage = 1; // 기본값 설정
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌한 오브젝트가 MonsterController 컴포넌트를 가지고 있는지 확인
        MonsterController monster = other.GetComponent<MonsterController>();

        // 만약 MonsterController 컴포넌트를 찾았다면
        if (monster != null)
        {
            // 몬스터의 TakeDamage 함수를 호출하여 데미지를 줍니다.
            monster.TakeDamage(attackDamage);
            Debug.Log($"몬스터에게 {attackDamage} 대미지를 입혔습니다!");
        }
    }
}
