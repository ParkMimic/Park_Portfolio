using System.Collections;
using UnityEngine;

public class Boss_2 : MonoBehaviour
{
    #region 상태 열거형 (State Enum)
    public enum BossState { Chasing, Attacking, Stunned, Dead }
    [Header("상태")]
    [SerializeField] private BossState currentState;
    #endregion

    #region 기본 설정 (Fields & Properties)

    [Header("기본 스탯")]
    public float moveSpeed = 2f;
    public float maxHealth = 100;
    [SerializeField] private float currentHealth;

    [Header("플레이어")]
    public Transform player;

    [Header("일반 공격")]
    public int attackDamage = 15;
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    [SerializeField] private GameObject attackHitboxObject;

    [Header("피격 및 그로기")]
    public float hurtForce = 5f;
    public float maxGroggy = 100f;
    [SerializeField] private float currentGroggy;
    public float stunDuration = 5f;

    // 컴포넌트 및 내부 변수
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Color originalColor;
    private BoxCollider2D attackHitbox;
    private float lastAttackTime;
    #endregion

    #region 유니티 생명주기 (Unity Lifecycle)

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        attackHitbox = attackHitboxObject.GetComponent<BoxCollider2D>();

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        currentGroggy = 0;
        originalColor = spriteRenderer.color;
        attackHitbox.enabled = false;
        lastAttackTime = -attackCooldown;

        if (player == null)
        {
            Debug.LogError("'Player' 태그를 가진 오브젝트를 찾을 수 없습니다. 보스가 비활성화됩니다.");
            enabled = false;
            return;
        }

        // 시작과 동시에 추적 상태로 설정
        SetState(BossState.Chasing);
    }

    private void Update()
    {
        if (player == null || PlayerManager.Instance.HP <= 0)
        {
            StopMovement();
            return;
        }

        // 상태에 따른 행동 처리
        switch (currentState)
        {
            case BossState.Chasing:
                UpdateChasing();
                break;
            case BossState.Attacking:
            case BossState.Stunned:
            case BossState.Dead:
                // 특정 상태에서는 움직임 및 추가 행동 정지
                StopMovement();
                break;
        }
    }
    #endregion

    #region 상태 관리 (State Machine)

    private void SetState(BossState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        // 상태 변경 시 초기화 로직
        switch (currentState)
        {
            case BossState.Chasing:
                anim.SetBool("isWalking", true);
                break;
            case BossState.Stunned:
                StartCoroutine(StunSequence());
                break;
            case BossState.Dead:
                StartCoroutine(DieSequence());
                break;
        }
    }

    private void UpdateChasing()
    {
        // 공격 결정
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        DecideNextAttack(distanceToPlayer);

        // 공격 중이 아닐 때만 플레이어 방향으로 이동
        if (currentState == BossState.Chasing)
        {
            MoveTowardsPlayer();
            FlipSpriteTowardsPlayer();
        }
    }

    #endregion

    #region 행동 로직 (Behavior Logic)

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rigid.linearVelocity = new Vector2(direction.x * moveSpeed, rigid.linearVelocity.y);
    }

    private void StopMovement()
    {
        rigid.linearVelocity = Vector2.zero;
        anim.SetBool("isWalking", false);
    }

    private void FlipSpriteTowardsPlayer()
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

    #endregion

    #region 공격 (Attacks)

    private void DecideNextAttack(float distanceToPlayer)
    {
        if (Time.time >= lastAttackTime + attackCooldown && distanceToPlayer <= attackRange)
        {
            StartCoroutine(BasicAttackSequence());
        }
    }

    private IEnumerator BasicAttackSequence()
    {
        SetState(BossState.Attacking);
        StopMovement();
        lastAttackTime = Time.time;

        anim.SetTrigger("Attack");

        yield return null;
    }

    // Animation Event: 일반 공격 Hitbox 활성화
    public void ActivateAttackHitbox()
    {
        attackHitbox.enabled = true;
    }

    // Animation Event: 일반 공격 Hitbox 비활성화
    public void DeactivateAttackHitbox()
    {
        attackHitbox.enabled = false;
    }

    // Animation Event: 공격 애니메이션 종료
    public void FinishAttack()
    {
        if (currentState != BossState.Dead && currentState != BossState.Stunned)
        {
            SetState(BossState.Chasing);
        }
    }

    #endregion

    #region 피격 및 체력 (Damage & Health)

    public void TakeDamage(float damage, Vector2 knockbackDirection)
    {
        if (currentState == BossState.Dead) return;

        float finalDamage = (currentState == BossState.Stunned) ? damage * 2f : damage;
        currentHealth -= finalDamage;

        StartCoroutine(HurtEffect(knockbackDirection));

        if (currentHealth <= 0)
        {
            SetState(BossState.Dead);
        }
    }

    public void TakeGroggyDamage(float amount)
    {
        if (currentState == BossState.Stunned || currentState == BossState.Dead) return;

        currentGroggy += amount;
        if (currentGroggy >= maxGroggy)
        {
            SetState(BossState.Stunned);
        }
    }

    private IEnumerator HurtEffect(Vector2 knockbackDirection)
    {
        if (currentState != BossState.Attacking && currentState != BossState.Stunned)
        {
            rigid.AddForce(knockbackDirection * hurtForce, ForceMode2D.Impulse);
        }

        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    #endregion

    #region 특수 상태 (Special States)

    private IEnumerator StunSequence()
    {
        StopMovement();
        currentGroggy = 0;
        anim.SetTrigger("Stunned");
        spriteRenderer.color = Color.green;

        yield return new WaitForSeconds(stunDuration);

        spriteRenderer.color = originalColor;
        SetState(BossState.Chasing);
    }

    private IEnumerator DieSequence()
    {
        StopMovement();
        anim.SetTrigger("isDead");
        GetComponent<Collider2D>().enabled = false;
        rigid.bodyType = RigidbodyType2D.Kinematic;

        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    #endregion
}