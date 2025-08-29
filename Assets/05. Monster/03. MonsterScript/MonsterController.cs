using System.Collections;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    #region 변수 선언 (Fields & Properties)

    [Header("기본 설정")]
    public float moveSpeed = 2f;
    public float maxHealth = 3;
    [SerializeField] private float currentHealth;

    [Header("공격 관련")]
    //public int contactDamage = 1; // 접촉 시 데미지
    public int attackDamage = 1; // 공격 데미지
    public float attackRange = 3f; // 공격 사정거리
    public float attackCooldown = 2f; // 공격 쿨다운
    public float postAttackDelay = 0.5f; // 공격 후 딜레이
    [SerializeField] private GameObject attackHitboxObject; // 공격 히트박스 오브젝트

    [Header("패링 관련")]
    public float parryFlashDuration = 0.1f; // 패링 성공 시 반짝이는 시간

    [Header("피격 관련")]
    public float hurtDuration = 0.5f; // 피격 상태 지속 시간
    public float hurtForce = 5f; // 피격 시 밀려나는 힘

    [Header("넉백 강도")]
    public float stunKnockbackPower = 10f;
    public float stunKnockbackDuration = 0.5f;

    [Header("그로기/스턴 관련")]
    public float maxGroggy = 1f; // 최대 그로기 수치
    [SerializeField] private float currentGroggy; // 현재 그로기 수치
    public float stunDuration = 2f; // 스턴 지속 시간
    private bool isStunned = false; // 스턴 상태 여부

    [Header("타겟 설정")]
    public Transform player; // 플레이어 Transform
    public LayerMask sightLayerMask; // 시야 감지 레이어

    [Header("시야 설정")]
    public float visionRange = 10f; // 플레이어를 발견할 수 있는 최대 거리
    public float loseSightDistance = 15f; // 플레이어가 이 거리 이상 벗어나면 시야를 잃음
    private bool hasSpottedPlayer = false; // 플레이어 발견 여부

    // 컴포넌트 참조
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Color originalColor;
    private BoxCollider2D hitBox;
    private WaitForSeconds shortWait;

    // 상태 변수
    private bool isAttacking = false;
    private bool isDead = false;
    private bool isHurt = false;
    private bool isKnockedback = false;

    // 공격 시간 제어
    private float lastAttackTime;

    #endregion

    #region 기본 함수 (Unity Lifecycle)

    private void Awake()
    {
        // 컴포넌트 초기화
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // 변수 초기화
        currentHealth = maxHealth;
        currentGroggy = 0;
        lastAttackTime = -attackCooldown; // 게임 시작 시 바로 공격 가능하도록
        originalColor = spriteRenderer.color;

        // 히트박스 초기화
        hitBox = attackHitboxObject.GetComponent<BoxCollider2D>();
        hitBox.enabled = false; // 시작 시 공격 히트박스 비활성화

        // WaitForSeconds 저장용
        shortWait = new WaitForSeconds(0.1f);

        // 플레이어 자동 찾기
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
                enabled = false; // 컴포넌트 비활성화
            }
        }
    }

    private void Update()
    {
        // 죽거나, 스턴 상태거나, 플레이어가 없으면 행동 정지
        if (isDead || isStunned || player == null || PlayerManager.Instance.HP <= 0) return;

        HandlePlayerDetection();
        HandleAttacking();
        HandleSpriteFlip();
    }

    private void FixedUpdate()
    {
        // 물리 기반 이동 처리
        HandleMovement();
    }

    #endregion

    #region 로직 (Logic)

    private void HandlePlayerDetection()
    {
        if (!hasSpottedPlayer)
        {
            CheckForPlayerInSight();
        }
        else
        {
            // 플레이어를 놓쳤는지 확인
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer > loseSightDistance)
            {
                hasSpottedPlayer = false;
            }
        }
    }

    private void HandleMovement()
    {
        if (isKnockedback || isHurt || isDead) return; // 피격 상태일 때는 이동하지 않음

        // 행동 정지 조건: 죽음,, 공격, 스턴, 플레이어 미발견, 공격 범위 내 진입
        if (isAttacking || isStunned || !hasSpottedPlayer || Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            anim.SetBool("isWalking", false);
            return;
        }

        // 플레이어 방향으로 이동
        Vector2 direction = (player.position - transform.position).normalized;
        rigid.linearVelocity = new Vector2(direction.x * moveSpeed, rigid.linearVelocity.y);
        anim.SetBool("isWalking", true);
    }

    private void HandleSpriteFlip()
    {
        // 공격 중이 아닐 때만 플레이어를 바라보도록 방향 전환
        if (!isAttacking && hasSpottedPlayer)
        {
            if (player.position.x < transform.position.x)
            {
                transform.localScale = new Vector3(1, 1, 1); // 왼쪽 보기
            }
            else
            {
                transform.localScale = new Vector3(-1, 1, 1); // 오른쪽 보기
            }
        }
    }

    private void CheckForPlayerInSight()
    {
        // 몬스터가 바라보는 방향으로 Raycast 발사하여 플레이어 감지
        Vector2 direction = transform.localScale.x > 0 ? Vector2.left : Vector2.right;
        Vector2 raycastOrigin = (Vector2)transform.position + new Vector2(0, 1f);
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, direction, visionRange, sightLayerMask);

        Debug.DrawRay(raycastOrigin, direction * visionRange, Color.red);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            hasSpottedPlayer = true;
        }
    }

    #endregion

    #region 공격 (Attack)

    private void HandleAttacking()
    {
        if (!hasSpottedPlayer || isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        // 공격 쿨다운이 지났고, 플레이어가 공격 범위 안에 있을 때 공격
        if (Time.time >= lastAttackTime + attackCooldown && distanceToPlayer <= attackRange)
        {
            lastAttackTime = Time.time;
            StartCoroutine(AttackSequence());
        }
    }

    // 애니메이션 이벤트로 호출될 함수: 패링 타이밍 효과
    public void TriggerParryFlash()
    {
        StartCoroutine(ParryFlashEffect());
    }

    #endregion

    #region 피격 및 체력 (Damage & Health)

    public void TakeDamage(float damage, Vector2 knockbackDirection)
    {
        if (isDead) return;

        float finalDamage = isStunned ? damage * 3 : damage;
        currentHealth -= finalDamage;

        if (currentHealth <= 0)
        {
            Die();
            return; // 죽었으면 아래 피격/넉백 처리를 할 필요가 없으므로 즉시 함수 종료
        }

        if (isStunned)
        {

            StartCoroutine(Knockback(knockbackDirection, stunKnockbackPower, stunKnockbackDuration));
        }
        else
        {
            StartCoroutine(HurtSequence(knockbackDirection));
        }
    }

    public void TakeGroggyDamage(float amount)
    {
        if (isDead) return;

        currentGroggy += amount;
        if (currentGroggy >= maxGroggy)
        {
            StartCoroutine(StunSequence());
        }
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines(); // 진행 중인 모든 코루틴 중지
        anim.SetTrigger("isDead");
        rigid.linearVelocity = Vector2.zero;
        rigid.angularVelocity = 0f;
        rigid.bodyType = RigidbodyType2D.Kinematic; // 물리 효과 정지
    }

    #endregion

    #region 코루틴 (Coroutines)

    private IEnumerator AttackSequence()
    {
        isAttacking = true;
        anim.SetTrigger("Attack");
        yield return null; // 다음 프레임까지 대기
    }

    private IEnumerator StunSequence()
    {
        isStunned = true;
        isHurt = false;
        currentGroggy = 0;

        // 공격 중이었다면 공격 상태 강제 종료
        isAttacking = false;
        anim.ResetTrigger("Attack");

        anim.SetTrigger("Stunned");

        // TimeManager에게 슬로우 모션을 요청
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.RequestSlowMotion(0.5f, 0.1f);
        }

        spriteRenderer.color = Color.yellow;
        hitBox.enabled = false; // 스턴 시 공격 히트박스 비활성화

        yield return new WaitForSeconds(stunDuration);

        isStunned = false;
        spriteRenderer.color = originalColor;
    }

    private IEnumerator HurtSequence(Vector2 knockbackDirection)
    {
        isHurt = true;
        isAttacking = false; // 피격 시 공격 중단
        anim.ResetTrigger("Attack");
        spriteRenderer.color = Color.white;

        StartCoroutine(Knockback(knockbackDirection, hurtForce, hurtDuration));
        rigid.AddForce(new Vector2(knockbackDirection.x, 0) * hurtForce, ForceMode2D.Impulse);

        anim.SetTrigger("isHurt");
        yield return shortWait;
        spriteRenderer.color = originalColor;

        yield return new WaitForSeconds(hurtDuration);

        isHurt = false;
    }

    public IEnumerator Knockback(Vector2 direction, float power, float duration)
    {
        isKnockedback = true;

        rigid.linearVelocity = Vector2.zero;

        Vector2 forceToApply = new Vector2(direction.x, 0).normalized * power;
        rigid.AddForce(new Vector2(direction.x ,0).normalized * power, ForceMode2D.Impulse);

        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        rigid.linearVelocity = Vector2.zero;
        isKnockedback = false;
    }

    private IEnumerator ParryFlashEffect()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(parryFlashDuration);
        spriteRenderer.color = originalColor;
    }

    #endregion

    #region 애니메이션 이벤트 (Animation Events)

    // 공격 애니메이션의 특정 프레임에서 호출 (히트박스 활성화)
    public void ActivateAttackHitbox()
    {
        if (attackHitboxObject != null)
        {
            hitBox.enabled = true;
        }
    }

    // 공격 애니메이션의 다른 프레임에서 호출 (히트박스 비활성화)
    public void DeactivateAttackHitbox()
    {
        if (attackHitboxObject != null)
        {
            hitBox.enabled = false;
        }
    }

    // 공격 애니메이션 종료 시 호출
    public void FinishAttack()
    {
        StartCoroutine(PostAttackDelaySequence());
    }

    private IEnumerator PostAttackDelaySequence()
    {
        yield return new WaitForSeconds(postAttackDelay);
        isAttacking = false;
    }

    // 죽는 애니메이션 종료 시 호출 (콜라이더 비활성화)
    public void DisableMonsterCollider()
    {
        Collider2D monsterCollider = GetComponent<Collider2D>();
        if (monsterCollider != null)
        {
            monsterCollider.enabled = false;
        }
    }

    // 콜라이더 비활성화 후 호출 (오브젝트 파괴)
    public void DestroyMonster()
    {
        Destroy(gameObject);
    }

    #endregion
}