using NUnit.Framework.Constraints;
using System.Collections;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [Header("기본 설정")]
    public float moveSpeed = 2f;
    public float maxHealth = 3;
    [SerializeField] private float currentHealth;

    [Header("공격 설정")]
    public int contactDamage = 1; // 접촉 시 대미지
    public int attackDamage = 1; // 칼 공격 대미지
    public float attackRange = 3f; // 이 범위 안에 들어오면 공격 시작
    public float attackCooldown = 2f; // 공격 후 다음 공격까지의 대기 시간

    [Header("패링 설정")]
    public float parryFlashDuration = 0.1f; // 패링 가능 타이밍에 점멸할 시간

    [Header("피격 설정")]
    public float hurtDuration = 0.5f; // 피격 후 무적 시간
    public float hurtForce = 5f; // 피격 시 넉백 힘

    [Header("플레이어 감지")]
    public Transform player; // 플레이어 Transform
    public LayerMask playerLayer; // 플레이어 레이어

    [Header("시야 설정")]
    public float visionRange = 10f; // 플레이어를 감지할 수 있는 최대 거리
    public float loseSightDistance = 15f; // 플레이어가 이 거리 이상 멀어지면 시야를 잃음
    private bool hasSpottedPlayer = false; // 플레이어를 감지했는지 여부

    // 내부 변수
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Color originalColor;

    private bool isAttacking = false;
    private bool isDead = false;
    private float lastAttackTime;
    private bool isHurt = false; // 몬스터가 아픈 상태인지 여부

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        attackCooldown = 0; // 공격 쿨타임 초기화
        lastAttackTime = 0; // 마지막 공격 시간도 초기화

        currentHealth = maxHealth; // 몬스터의 현재 체력을 최대 체력으로 초기화

        originalColor = spriteRenderer.color;

        // 게임 시작 시 플레이어를 자동으로 찾기 (태그가 "Player"로 설정 되어 있어야 함.)
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogError("플레이어 오브젝트를 찾을 수 없습니다. 'Player' 태그를 확인해주세요.");
                enabled = false; // 스크립트 비활성화
            }
        }
    }

    private void Update()
    {
        if (isDead || player == null || PlayerManager.Instance.HP <= 0) return;

        // 플레이어를 발견하지 못했다면, 시야 내에 있는지 확인
        if (!hasSpottedPlayer)
        {
            CheckForPlayerInSight();
        }
        else // 플레이어를 발견했다면
        {
            // 플레이어와의 거리 계산
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // 만약 거리가 '놓치는 거리'보다 멀어졌다면
            if (distanceToPlayer > loseSightDistance)
            {
                hasSpottedPlayer = false; // 플레이어를 놓쳤다고 판단.
                return; // 추적 및 공격 로직을 더 이상 진행하지 않음.
            }

            // 공격 쿨타임이 지났고, 플레이어가 공격 범위 안에 있으며 현재 공격 중이 아니라면
            if (Time.time >= lastAttackTime + attackCooldown && distanceToPlayer <= attackRange && !isAttacking)
            {
                // AttackSequence 코루틴을 직접 시작하는 대신, 애니메이션 트리거만 설정합니다.
                isAttacking = true;
                lastAttackTime = Time.time;
                anim.SetTrigger("Attack");
            }

            // 플레이어를 향해 바라보도록 스프라이트 뒤집기 (공격 중이 아닐 때만)
            if (!isAttacking)
            {
                if (player.position.x < transform.position.x)
                {
                    transform.localScale = new Vector3(1, 1, 1); // 플레이어가 왼쪽에 있으면 왼쪽 보기
                }
                else
                {
                    transform.localScale = new Vector3(-1, 1, 1); // 플레이어가 오른쪽에 있으면 오른쪽 보기
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // 죽었거나, 공격 중이거나, 플레이어가 공격 범위 안에 있으면 움직이지 않음
        if (isDead || isHurt || isAttacking || !hasSpottedPlayer || Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            anim.SetBool("isWalking", false);
            return;
        }

        // 플레이어를 향해 이동
        Vector2 direction = (player.position - transform.position).normalized;
        rigid.linearVelocity = new Vector2(direction.x * moveSpeed, rigid.linearVelocity.y);
        anim.SetBool("isWalking", true);
    }

    // Animation Event로 호출될 함수: 패링 타이밍 점멸 효과
    public void TriggerParryFlash()
    {
        StartCoroutine(ParryFlashEffect());
    }

    private IEnumerator ParryFlashEffect()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(parryFlashDuration);
        spriteRenderer.color = originalColor;
    }

    public void FinishAttack()
    {
        isAttacking = false; // 공격 완료
    }

    // Animation Event 로 호출될 함수
    public void PerformSwordAttack()
    {
        // 몬스터가 피격 상태라면 공격 판정을 실행하지 않습니다.
        if (isHurt) return;

        // 플레이어가 여전히 칼 휘두르는 범위 내에 있는지 확인 후 대미지 처리
        if (Vector2.Distance(transform.position, player.position) <= attackRange + 0.5f) // 약간의 추가 범위
        {
            Vector2 knockbackDir = (player.position.x < transform.position.x) ? Vector2.left : Vector2.right;

            // PlayerManager 스크립트가 있다고 가정합니다. 실제 사용하는 스크립트 이름으로 바꿔주세요.
            PlayerManager.Instance.TakeDamage(attackDamage, knockbackDir);
            Debug.Log($"플레이어에게 + {attackDamage} + 대미지를 입혔습니다!");
        }
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection)
    {
        if (isDead) return; // 죽은 상태에서는 데미지를 받지 않습니다.

        currentHealth -= damage; // 피격 애니메이션과 무관하게 체력은 항상 깎습니다.

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 몬스터가 이미 피격 상태가 아닐 때만 피격 시퀀스를 시작합니다.
            // 이렇게 하면 연속 공격 시에도 데미지는 계속 들어가지만, 넉백/애니메이션은 중첩되지 않습니다.
            if (!isHurt)
            {
                StartCoroutine(HurtSequence(knockbackDirection));
            }
        }
    }

    private IEnumerator HurtSequence(Vector2 knockbackDirection)
    {
        // 1. 상태 잠금 및 행동 취소
        isHurt = true;
        StopAllCoroutines(); // 가장 중요! 진행 중이던 AttackSequence 같은 코루틴을 즉시 중단.
        isAttacking = false; // 공격 중지
        anim.ResetTrigger("Attack"); // 진행 중이던 공격 애니메이션 트리거를 리셋하여 강제로 중단합니다.

        // 2. 시각적 피드백: 잠시 붉게 변함
        spriteRenderer.color = Color.red;

        // 3. 넉백 적용
        // rigid.linearVelocity = Vector2.zero; // 이 줄을 제거하여 기존 속도에 넉백이 더해지도록 합니다.
        rigid.AddForce(knockbackDirection * hurtForce, ForceMode2D.Impulse); // 넉백 적용.

        // 4. 애니메이션 재생
        anim.SetTrigger("isHurt");

        // 5. 피격 지속 시간만큼 대기
        yield return new WaitForSeconds(hurtDuration);

        // 6. 상태 초기화
        isHurt = false;
        spriteRenderer.color = originalColor; // 원래 색깔로 돌아옴
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines(); // 죽는 순간 모든 코루틴을 중단하여 다른 행동을 무시합니다.
        anim.SetTrigger("isDead");

        // 움직임을 멈추고 물리 영향을 받지 않도록 Kinematic으로 설정
        rigid.linearVelocity = Vector2.zero;
        rigid.angularVelocity = 0f; // 회전도 멈춥니다.
        rigid.bodyType = RigidbodyType2D.Kinematic;

        // 콜라이더 비활성화와 오브젝트 파괴는 애니메이션 이벤트로 처리합니다.
    }

    // Animation Event로 호출될 함수: 몬스터의 콜라이더를 비활성화합니다.
    public void DisableMonsterCollider()
    {
        Collider2D monsterCollider = GetComponent<Collider2D>();
        if (monsterCollider != null)
        {
            monsterCollider.enabled = false;
        }
    }

    // Animation Event로 호출될 함수: 몬스터 오브젝트를 파괴합니다.
    public void DestroyMonster()
    {
        Destroy(gameObject);
    }

    void CheckForPlayerInSight()
    {
        Vector2 direction = transform.localScale.x > 0 ? Vector2.left : Vector2.right;

        Vector2 raycastOrigin = (Vector2)transform.position + new Vector2(0, 1f); // 몬스터 시야각 조정

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, direction, visionRange, playerLayer);

        Debug.DrawRay(raycastOrigin, direction * visionRange, Color.red);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            hasSpottedPlayer = true;
            Debug.Log("플레이어 발견!");
        }
    }
}
