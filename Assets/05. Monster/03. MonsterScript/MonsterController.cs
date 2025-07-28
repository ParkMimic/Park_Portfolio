using System.Collections;
using UnityEditor.Rendering;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [Header("기본 설정")]
    public float moveSpeed = 2f;
    public float maxHealth = 3;
    private float currentHealth;

    [Header("공격 설정")]
    public int contactDamage = 1; // 접촉 시 대미지
    public int attackDamage = 1; // 칼 공격 대미지
    public float attackRange = 3f; // 이 범위 안에 들어오면 공격 시작
    public float attackCooldown = 2f; // 공격 후 다음 공격까지의 대기 시간
    public float telegraphDuration = 0.5f; // 공격 전 붉게 점등되는 시간

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

    private void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        attackCooldown = 0; // 공격 쿨타임 초기화
        lastAttackTime = 0; // 마지막 공격 시간도 초기화

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
                StartCoroutine(AttackSequence());
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
        if (isDead || isAttacking || !hasSpottedPlayer || Vector2.Distance(transform.position, player.position) <= attackRange)
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

    private IEnumerator AttackSequence()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // 1. 공격 예고 (붉은 점멸)
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(telegraphDuration);
        spriteRenderer.color = originalColor;

        // 2. 공격 애니메이션 실행
        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.35f); // 애니메이션이 시작되고 약간의 시간 대기
        PerformSwordAttack();

        // 참고: 실제 공격 판정(대미지 주기)은 애니메이션의 특정 프레임에
        // Animation Event를 추가하여 PerformSwordAttack() 함수를 호출하는 것이 가장 정확합니다.
        // 여기서는 애니메이션의 길이를 가정하여 잠시 후 isAttacking 을 false 로 바꿉니다.
        // 예를 들어 공격 애니메이션이 1초라면 아래 시간도 1초로 설정합니다.
        yield return new WaitForSeconds(1f);

        isAttacking = false;
    }

    // Animation Event 로 호출될 함수
    public void PerformSwordAttack()
    {
        // 플레이어가 여전히 칼 휘두르는 범위 내에 있는지 확인 후 대미지 처리
        if (Vector2.Distance(transform.position, player.position) <= attackRange + 0.5f) // 약간의 추가 범위
        {
            Vector2 knockbackDir = (player.position.x < transform.position.x) ? Vector2.left : Vector2.right;

            // PlayerManager 스크립트가 있다고 가정합니다. 실제 사용하는 스크립트 이름으로 바꿔주세요.
            PlayerManager.Instance.TakeDamage(attackDamage, knockbackDir);
            Debug.Log($"플레이어에게 + {attackDamage} + 대미지를 입혔습니다!");
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        isHurt = true; // 몬스터 피격 상태로 설정
        anim.SetTrigger("isHurt");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        anim.SetTrigger("Die");
        rigid.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false; // 다른 오브젝트와 충돌하지 않도록

        // 사망 애니메이션 시간만큼 기다린 후 오브젝트 파괴
        Destroy(gameObject, 3f); // 3초는 예시, 실제 사망 애니메이션 길이에 맞춰주세요.
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