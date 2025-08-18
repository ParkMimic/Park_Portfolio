using NUnit.Framework.Constraints;
using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    // 보스의 상태 종류
    public enum BossState
    {
        Falling,
        Idle,
        Attacking
    }

    public Transform spawnPoint;  // 보스가 스폰될 위치
    public Transform targetPoint; // 보스가 도착할 위치
    public float dropSpeed = 20f; // 보스가 떨어지는 속도

    // 착지 충격
    public float knockbackRadius = 5f; // 충격 범위
    public float knockbackForce = 25f; // 밀어내는 힘
    public LayerMask playerLayer;      // 플레이어 레이어

    private Rigidbody2D rigid;
    private Animator anim;

    private bool isFalling;

    [Header("AI 설정")]
    public Transform playerTransform;       // 플레이어의 Transform 을 담을 변수
    public float moveSpeed = 3f;            // 보스 이동 속도
    public float dashAttackRange = 15f;     // 돌진 공격 범위
    public float attackRange = 3f;          // 공격을 시작하는 범위
    [Space]
    public float dashAttackSpeed = 25f;     // 돌진 속도
    public float dashAttackDuration = 0.3f; // 돌진 지속 시간
    [Space]
    public float minAttackDelay = 1f; // 최소 공격 딜레이
    public float maxAttackDelay = 2.5f; // 최대 공격 딜레이

    [Header("공격 판정 설정")]
    public GameObject meleeHitbox;
    private BoxCollider2D meleeCollider;

    private BossState currentState;         // 보스의 현재 상태를 저장

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        transform.position = spawnPoint.position;

        currentState = BossState.Falling; // 시작 상태를 '낙하중'으로 설정
        StartCoroutine(DropSequence());

        meleeCollider = meleeHitbox.GetComponent<BoxCollider2D>();
        meleeCollider.enabled = false; // 초기에는 공격 히트박스 비활성화
    }

    private void Update()
    {
        // currentState가 Falling 이거나, playerTransform 이 할당되지 않았으면 아무 것도 안함.
        if (currentState == BossState.Falling || currentState == BossState.Attacking || playerTransform == null) return;

        if (currentState == BossState.Idle)
        {
            DecideNextAction();
        }

        UpdateAnimator();
    }

    void DecideNextAction()
    {
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance < attackRange)
        {
            // 근접 공격 범위 -> 근접 공격 실행
            StartCoroutine(MeleeAttack());
        }
        else if (distance < dashAttackRange)
        {
            // 원거리 공격 범위 -> 돌진 공격 실행
            StartCoroutine(DashAttack());
        }
        else
        {
            // 너무 멀리 있음 -> 플레이어에게 다가감 (이동은 상태 변경 없이 즉시 실행)
            MoveTowardsPlayer();
        }
    }

    // 플레이어 방향으로 이동하는 함수
    void MoveTowardsPlayer()
    {
        // 이동 방향 결정 및 보스 방향 전환
        float direction = Mathf.Sign(playerTransform.position.x - transform.position.x);
        transform.localScale = new Vector3(-direction, 1, 1);

        // 이동 속도 적용
        rigid.linearVelocity = new Vector2(direction * moveSpeed, rigid.linearVelocity.y);
    }

    IEnumerator MeleeAttack()
    {
        currentState = BossState.Attacking;         // 상태를 '공격 중'으로 변경
        rigid.linearVelocity = Vector2.zero;        // 공격 중에는 이동을 멈춤
        Debug.Log("근접 공격 실행!");

        yield return new WaitForSeconds(0.5f); // 공격 선딜레이
        meleeCollider.enabled = true;
        yield return new WaitForSeconds(0.3f); // 공격 판정 유지 시간
        meleeCollider.enabled = false;

        //yield return new WaitForSeconds(0.7f); // 공격 후딜레이
        float randomDelay = Random.Range(minAttackDelay, maxAttackDelay);
        yield return new WaitForSeconds(randomDelay); // 공격 후 랜덤 딜레이

        currentState = BossState.Idle;              // 상태를 다시 '대기'로 변경
    }

    IEnumerator DashAttack()
    {
        currentState = BossState.Attacking;         // 상태를 '공격 중'으로 변경
        Debug.Log("돌진 공격 실행!");

        float horizontalDirection = Mathf.Sign(playerTransform.position.x - transform.position.x);
        Vector2 direction = new Vector2(horizontalDirection, 0);

        // 플레이어 방향으로 방향 및 시선 전환
        transform.localScale = new Vector3(-direction.x, 1, 1);

        // 설정된 속도로 짧은 시간 동안 돌진
        rigid.linearVelocity = direction * dashAttackSpeed;
        yield return new WaitForSeconds(dashAttackDuration);

        // 돌진이 끝나면 그 자리에 멈춤
        rigid.linearVelocity = Vector2.zero;

        //// 공격 후 1초 대기
        //yield return new WaitForSeconds(1f);
        float randomDelay = Random.Range(minAttackDelay, maxAttackDelay);
        yield return new WaitForSeconds(randomDelay); // 공격 후 랜덤 딜레이

        currentState = BossState.Idle;              // 상태를 다시 '대기'로 변경
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    #region Update
    void UpdateAnimator()
    {
        anim.SetBool("isFalling", isFalling);
    }

    #endregion

    #region FixedUpdate
    void HandleMovement()
    {
        isFalling = rigid.linearVelocity.y < 0;
    }
    #endregion

    IEnumerator DropSequence()
    {
        // targetPoint 에 도착할 때까지 반복
        while (Vector3.Distance(transform.position, targetPoint.position) > 0.1f)
        {
            // 현재 위치에서 targetPoint 를 향해 dropSpeed 의 속도로 한 프레임만큼 이동
            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, dropSpeed * Time.deltaTime);

            yield return null;
        }

        // 루프가 끝나면 위치를 targetPoint 로 정확하게 맞춤
        transform.position = targetPoint.position;
        Debug.Log("보스 착지 완료!");

        // 주변 플레이어 감지 후 넉백
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, knockbackRadius, playerLayer);
        if (playerCollider != null)
        {
            // 플레이어가 감지되면 Rigidbody2D 를 가져옵니다.
            Rigidbody2D playerRigid = playerCollider.GetComponent<Rigidbody2D>();

            if (playerRigid != null)
            {
                // 넉백 방향 계산
                Vector2 knockbackDirection = (playerRigid.transform.position - transform.position).normalized;

                // 기존 속도를 0으로 만든 후, 순간적인 큰 힘을 가합니다.
                playerRigid.linearVelocity = Vector2.zero;
                playerRigid.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            }
        }

        currentState = BossState.Idle; // 착지 완료 후, '대기' 상태로 전환하여 AI 시작
    }

    private void OnDrawGizmosSelected()
    {
        // 기즈모의 색상을 빨간색으로 설정
        Gizmos.color = Color.red;

        // 현재 위치를 중심으로, knockbackRadius 크기의 와이어를 그립니다.
        Gizmos.DrawWireSphere(transform.position, knockbackRadius);
    }
}