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
    public int contactDamage = 1; // 접촉 시 데미지
    public int attackDamage = 1; // 칼 공격 데미지
    public float attackRange = 3f; // 이 거리 안에 들어오면 공격 시작
    public float attackCooldown = 2f; // 한 번 공격 후 다음 공격까지의 최소 시간
    public float pauseBeforeAttackTime = 0.2f; // 공격 애니메이션 시작 후 공격 판정까지의 시간
    public float attackDelay = 0.4f; // 공격 판정 지연시간

    [Header("패링 설정")]
    public float parryFlashDuration = 0.1f; // 패링 가능 타이밍에 번쩍이는 시간

    [Header("피격 설정")]
    public float hurtDuration = 0.5f; // 피격 후 지연시간
    public float hurtForce = 5f; // 피격 시 밀려나는 힘

    [Header("그로기/기절 설정")]
    public float maxGroggy = 1f; // 최대 그로기 수치
    private float currentGroggy; // 현재 그로기 수치
    public float stunDuration = 2f; // 기절 지속 시간
    private bool isStunned = false; // 기절 상태 여부

    [Header("플레이어 설정")]
    public Transform player; // 플레이어 Transform
    public LayerMask playerLayer; // 플레이어 레이어

    [Header("시야 설정")]
    public float visionRange = 10f; // 플레이어를 감지할 수 있는 최대 거리
    public float loseSightDistance = 15f; // 플레이어가 이 거리 이상 벗어나면 시야를 잃음
    private bool hasSpottedPlayer = false; // 플레이어를 발견했는지 여부

    // 상태 변수
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Color originalColor;

    private bool isAttacking = false;
    private bool isDead = false;
    private float lastAttackTime;
    private bool isHurt = false; // 몬스터가 맞고 있는지 확인

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        lastAttackTime = 0; // 마지막 공격 시간을 초기화
        currentHealth = maxHealth; // 시작 시 현재 체력을 최대 체력으로 초기화
        currentGroggy = 0; // 그로기 수치 초기화
        originalColor = spriteRenderer.color; // 기본 색 저장

        // player 변수 할당 안 됐을 시 플레이어를 자동으로 찾음 (태그가 "Player"로 설정되어 있어야 함)
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
        if (isDead || isStunned || player == null || PlayerManager.Instance.HP <= 0) return;

        // 플레이어를 발견하지 못했다면, 시야 내에 있는지 확인
        if (!hasSpottedPlayer)
        {
            CheckForPlayerInSight();
        }
        else // 플레이어를 발견했다면
        {
            // 플레이어와의 거리 계산
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // 현재 거리가 '시야를 잃는 거리'보다 멀어졌다면
            if (distanceToPlayer > loseSightDistance)
            {
                hasSpottedPlayer = false; // 플레이어를 잃었다고 판단.
                return; // 이후의 로직을 더 이상 진행하지 않음.
            }

            // 공격 쿨타임이 지났고, 플레이어가 공격 범위 내에 있으며, 현재 공격 중이 아니라면
            if (Time.time >= lastAttackTime + attackCooldown && distanceToPlayer <= attackRange && !isAttacking)
            {
                // AttackSequence 코루틴을 직접 호출하는 대신, 애니메이션 트리거를 발동합니다.
                isAttacking = true;
                lastAttackTime = Time.time;
                StartCoroutine(FullAttackSequence());
            }

            // 플레이어를 향해 바라보도록 스프라이트 방향 전환 (공격 중이 아닐 때만)
            if (!isAttacking)
            {
                if (player.position.x < transform.position.x)
                {
                    transform.localScale = new Vector3(1, 1, 1); // 플레이어가 왼쪽에 있으면 오른쪽을 봄
                }
                else
                {
                    transform.localScale = new Vector3(-1, 1, 1); // 플레이어가 오른쪽에 있으면 왼쪽을 봄
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // 죽었거나, 맞고 있거나, 공격 중이거나, 기절 중이거나, 플레이어가 공격 범위 내에 있으면 움직이지 않음
        if (isDead || isHurt || isAttacking || isStunned || !hasSpottedPlayer || Vector2.Distance(transform.position, player.position) <= attackRange)
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

    // Animation Event로 호출될 함수: 패링 타이밍 시간 효과
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

    private IEnumerator FullAttackSequence()
    {
        anim.SetTrigger("Attack");
        yield return new WaitForSeconds(pauseBeforeAttackTime);

        try
        {
            anim.speed = 0; // 애니메이션 정지
            yield return new WaitForSeconds(attackDelay);
        }
        finally
        {
            anim.speed = 1; // 어떤 상황에서든 애니메이션 속도 복원
        }
    }

    public void FinishAttack()
    {
        isAttacking = false; // 공격 완료
    }

    // Animation Event 로 호출될 함수
    public void PerformSwordAttack()
    {
        // 몬스터가 피격/기절 상태일때 공격판정이 일어나지 않도록 합니다.
        if (isHurt || isStunned) return;

        // 플레이어가 실제로 칼 휘두르는 범위 내에 있는지 확인 후 데미지 처리
        if (Vector2.Distance(transform.position, player.position) <= attackRange + 0.5f) // 약간의 추가 범위
        {
            Vector2 knockbackDir = (player.position.x < transform.position.x) ? Vector2.left : Vector2.right;

            // PlayerManager 스크립트가 있다고 가정합니다. 다른 이름의 스크립트라면 바꿔주세요.
            PlayerManager.Instance.TakeDamage(attackDamage, knockbackDir);
            Debug.Log($"플레이어에게 + {attackDamage} + 데미지를 입혔습니다!");
        }
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection)
    {
        if (isDead) return;

        float finalDamage = isStunned ? damage * 3 : damage;
        currentHealth -= finalDamage;

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (!isStunned) // 기절 상태가 아닐 때만 피격 효과
        {
            StartCoroutine(HurtSequence(knockbackDirection));
        }
    }

    public void TakeGroggyDamage(float amount)
    {
        if (isDead || isStunned) return;

        currentGroggy += amount;
        if (currentGroggy >= maxGroggy)
        {
            StartCoroutine(Stun());
        }
    }

    private IEnumerator Stun()
    {
        isStunned = true;
        currentGroggy = 0;
        isAttacking = false;
        anim.ResetTrigger("Attack");

        rigid.linearVelocity = Vector2.zero;

        // 기절 애니메이션이 있다면 여기서 재생
        // anim.SetTrigger("Stunned"); 
        spriteRenderer.color = Color.yellow; // 시각적 표시

        yield return new WaitForSeconds(stunDuration);

        isStunned = false;
        spriteRenderer.color = originalColor;
    }

    private IEnumerator HurtSequence(Vector2 knockbackDirection)
    {
        isHurt = true;
        isAttacking = false;
        anim.ResetTrigger("Attack");

        spriteRenderer.color = Color.white;
        rigid.AddForce(knockbackDirection * hurtForce, ForceMode2D.Impulse);

        anim.SetTrigger("isHurt");
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;

        yield return new WaitForSeconds(hurtDuration);

        isHurt = false;
    }

    private void Die()
    {
        isDead = true;
        anim.SetTrigger("isDead");

        rigid.linearVelocity = Vector2.zero;
        rigid.angularVelocity = 0f;
        rigid.bodyType = RigidbodyType2D.Kinematic;
    }

    public void DisableMonsterCollider()
    {
        Collider2D monsterCollider = GetComponent<Collider2D>();
        if (monsterCollider != null)
        {
            monsterCollider.enabled = false;
        }
    }

    public void DestroyMonster()
    {
        Destroy(gameObject);
    }

    void CheckForPlayerInSight()
    {
        Vector2 direction = transform.localScale.x > 0 ? Vector2.left : Vector2.right;

        Vector2 raycastOrigin = (Vector2)transform.position + new Vector2(0, 1f);

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, direction, visionRange, playerLayer);

        Debug.DrawRay(raycastOrigin, direction * visionRange, Color.red);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            hasSpottedPlayer = true;
            Debug.Log("플레이어 발견!");
        }
    }
}