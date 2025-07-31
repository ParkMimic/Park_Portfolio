using System.Collections;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    #region 변수 선언 (Fields & Properties)

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
    public float attackDelay = 0.4f; // 공격 판정 지속시간
    [SerializeField] private GameObject attackHitboxObject; // 공격 히트박스

    [Header("패링 설정")]
    public float parryFlashDuration = 0.1f; // 패링 가능 타이밍에 번쩍이는 시간

    [Header("피격 설정")]
    public float hurtDuration = 0.5f; // 피격 후 무적시간
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
    public float visionRange = 10f; // 플레이어를 발견할 수 있는 최대 거리
    public float loseSightDistance = 15f; // 플레이어가 이 거리 이상 벗어나면 시야를 잃음
    private bool hasSpottedPlayer = false; // 플레이어를 발견했는지 여부

    // 컴포넌트 변수
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Color originalColor;

    // 상태(State) 변수
    private bool isAttacking = false;
    private bool isDead = false;
    private bool isHurt = false;

    // 내부 로직 변수
    private float lastAttackTime;

    #endregion

    #region 기본 함수 (Unity Lifecycle)

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        currentHealth = maxHealth;
        currentGroggy = 0;
        lastAttackTime = -attackCooldown; // 시작하자마자 공격할 수 있도록
        originalColor = spriteRenderer.color;

        // 히트박스 비활성화로 시작
        if (attackHitboxObject != null) attackHitboxObject.SetActive(false);

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
                enabled = false;
            }
        }
    }

    private void Update()
    {
        if (isDead || isStunned || player == null || PlayerManager.Instance.HP <= 0) return;

        HandlePlayerDetection();
        HandleAttacking();
        HandleSpriteFlip();
    }

    private void FixedUpdate()
    {
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
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer > loseSightDistance)
            {
                hasSpottedPlayer = false;
            }
        }
    }

    private void HandleMovement()
    {
        if (isDead || isHurt || isAttacking || isStunned || !hasSpottedPlayer || Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            anim.SetBool("isWalking", false);
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rigid.linearVelocity = new Vector2(direction.x * moveSpeed, rigid.linearVelocity.y);
        anim.SetBool("isWalking", true);
    }

    private void HandleSpriteFlip()
    {
        if (!isAttacking && hasSpottedPlayer)
        {
            if (player.position.x < transform.position.x)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }

    private void CheckForPlayerInSight()
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

    #endregion

    #region 공격 (Attack)

    private void HandleAttacking()
    {
        if (!hasSpottedPlayer || isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (Time.time >= lastAttackTime + attackCooldown && distanceToPlayer <= attackRange)
        {
            lastAttackTime = Time.time;
            StartCoroutine(AttackSequence());
        }
    }

    // Animation Event로 호출될 함수: 패링 타이밍 시각 효과
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
        }
        else if (!isStunned)
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
            StartCoroutine(StunSequence());
        }
    }

    private void Die()
    {
        isDead = true;
        anim.SetTrigger("isDead");
        rigid.linearVelocity = Vector2.zero;
        rigid.angularVelocity = 0f;
        rigid.bodyType = RigidbodyType2D.Kinematic;
    }

    #endregion

    #region 코루틴 (Coroutines)

    private IEnumerator AttackSequence()
    {
        // 공격 조건이 충족되었는지 확인하기 위한 로그
        Debug.Log("공격 조건 충족! 공격 시퀀스를 시작합니다.");

        isAttacking = true;
        anim.SetTrigger("Attack");

        // 이제 코루틴은 애니메이션을 재생시키기만 하고 바로 종료됩니다.
        // 실제 공격 판정은 애니메이션 이벤트가 처리합니다.
        yield return null;
    }

    private IEnumerator StunSequence()
    {
        isStunned = true;
        currentGroggy = 0;
        isAttacking = false;
        anim.ResetTrigger("Attack");
        rigid.linearVelocity = Vector2.zero;

        // anim.SetTrigger("Stunned");
        spriteRenderer.color = Color.yellow;

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

    private IEnumerator ParryFlashEffect()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(parryFlashDuration);
        spriteRenderer.color = originalColor;
    }

    #endregion

    #region 애니메이션 이벤트 (Animation Events)

    // 공격 애니메이션의 특정 프레임에서 호출
    public void ActivateAttackHitbox()
    {
        if (attackHitboxObject != null) attackHitboxObject.SetActive(true);
    }

    // 공격 애니메이션의 다른 프레임에서 호출
    public void DeactivateAttackHitbox()
    {
        if (attackHitboxObject != null) attackHitboxObject.SetActive(false);
    }

    // Animation Event로 호출될 함수
    public void FinishAttack()
    {
        Debug.Log("FinishAttack 이벤트 호출됨! isAttacking 상태를 false로 설정합니다.");
        isAttacking = false;
    }

    // 죽는 애니메이션이 끝날 때 호출
    public void DisableMonsterCollider()
    {
        Collider2D monsterCollider = GetComponent<Collider2D>();
        if (monsterCollider != null)
        {
            monsterCollider.enabled = false;
        }
    }

    // DisableMonsterCollider 이후 호출
    public void DestroyMonster()
    {
        Destroy(gameObject);
    }

    #endregion
}
