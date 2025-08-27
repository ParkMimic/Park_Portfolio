using JetBrains.Annotations;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region 변수 선언 (Fields & Properties)

    // -- SerializeField는 인스펙터에서 값을 조정할 수 있게 해줍니다. --
    [Header("플레이어 기본 설정")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float jumpForce = 1f;
    [SerializeField] private float wallSlideSpeed = 1f;

    [Header("대쉬 설정")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("공격 설정")]
    [SerializeField] private GameObject attackHitboxObject;
    [SerializeField] private int attackCount = 3;
    [SerializeField] private float comboResetTime = 1.0f;

    [Header("패링 설정")]
    [SerializeField] private float parryDuration = 0.3f; // 패링 유효 시간
    [SerializeField] private float parryCooldown = 1.5f; // 패링 쿨타임
    private float parryCooldownTimer; // 패링 쿨타임 타이머
    private bool isParrying = false;
    private bool isParryWindowActive; // 패링 윈도우 활성화 여부
    private bool wasParrySuccessful; // 패링 성공 여부
    private bool isBlinking = false; // 블링크 효과 중복 실행 방지

    [Header("잔상 효과 설정")]
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private float ghostDelay = 0.05f;
    [SerializeField] private float ghostDelete;

    [Header("피격 설정")]
    [SerializeField] private float hurtForce = 3f;
    [SerializeField] private float hurtDuration = 0.5f;
    public bool isHurt;

    [Header("레이캐스트 설정")]
    [SerializeField] private LayerMask platformLayer; // 바닥과 벽을 감지할 통합 레이어
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float wallCheckDistance = 0.1f;

    // -- 컴포넌트 변수 --
    private Rigidbody2D rigid;
    private Animator anim;
    private CapsuleCollider2D capsuleCollider; // Raycast를 위해 CapsuleCollider2D로 변경

    // -- 상태(State) 변수 --
    private bool isGrounded;
    private bool isJumping;
    private bool isDoubleJumping;
    private bool isFalling;
    private bool isDashing;
    private bool isWallSliding;
    private bool isAttacking;
    private bool isGameOver;
    private bool isInCutscene;

    // -- 내부 로직 변수 --
    private float moveInput;
    private float lastMoveDirection = 1f;
    private int jumpCount;
    private float originalGravity;
    private float collisionCheckCooldown; // 충돌 감지 지연시간

    // -- 타이머 및 카운터 변수 --
    private float dashTime;
    private float dashCooldownTimer;
    private float ghostDelayTime;
    private float lastAttackTime;
    private int currentAttack;
    private int queuedAttackCount;
    private float parryInvincibilityTimer;

    #endregion

    #region 기본 함수

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider2D>(); // 컴포넌트 가져오기
    }

    private void Start()
    {
        originalGravity = rigid.gravityScale;
    }

    private void Update()
    {
        if (isGameOver || isHurt || isInCutscene)
        {
            if (isHurt) ResetAttackState();
            return;
        }

        HandleCollisionChecks(); // 매 프레임 충돌 감지
        HandleTimers();
        HandleInput();
        UpdateAnimator();

        if (PlayerManager.Instance.HP <= 0 && !isGameOver)
        {
            TriggerGameOver();
        }
    }

    private void FixedUpdate()
    {
        if (isGameOver || isHurt || isDashing || isInCutscene) return;
        HandleMovement();
    }

    #endregion

    #region 입력 및 상태 업데이트

    private void HandleInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0 && !isDashing && !isWallSliding && !isParrying)
        {
            StartDash();
        }

        if (isDashing)
        {
            UpdateDash();
            return;
        }

        if (Input.GetKeyDown(KeyCode.X) && !isWallSliding && !isParrying)
        {
            HandleAttackInput();
        }

        if (!isAttacking && !isParrying)
        {
            HandleJumpInput();
        }

        if (Input.GetKeyDown(KeyCode.C) && !isAttacking && !isDashing && !isParrying && parryCooldownTimer <= 0)
        {
            StartCoroutine(Parry());
        }
    }

    private void HandleTimers()
    {
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (parryCooldownTimer > 0) parryCooldownTimer -= Time.deltaTime;
        if (Time.time - lastAttackTime > comboResetTime) ResetAttackState();
        if (collisionCheckCooldown > 0) collisionCheckCooldown -= Time.deltaTime; // 쿨다운 감소
        if (parryInvincibilityTimer > 0) parryInvincibilityTimer -= Time.deltaTime;
    }

    private void UpdateAnimator()
    {
        anim.SetBool("isWalk", moveInput != 0 && isGrounded);
        anim.SetBool("isJump", isJumping);
        anim.SetBool("isDoubleJump", isDoubleJumping);
        anim.SetBool("isFalling", isFalling);
        anim.SetBool("isWallSlide", isWallSliding);
    }

    #endregion

    #region 이동 및 물리 (Movement & Physics)

    private void HandleMovement()
    {
        if (currentAttack >= 1 || isParrying)
        {
            if (isGrounded) rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
        }
        else if (isWallSliding)
        {
            rigid.linearVelocity = new Vector2(0, Mathf.Max(rigid.linearVelocity.y, -wallSlideSpeed));
        }
        else
        {
            rigid.linearVelocity = new Vector2(moveInput * moveSpeed, rigid.linearVelocity.y);
            if (moveInput != 0) transform.localScale = new Vector3(moveInput, 1, 1);
        }

        isFalling = !isGrounded && rigid.linearVelocity.y < 0;
    }

    private void HandleJumpInput()
    {
        if (jumpCount < 2 && Input.GetKeyDown(KeyCode.Z) && !GameManager.Instance.isPause)
        {
            if (isWallSliding) isWallSliding = false;
            isGrounded = false;
            isJumping = true;
            jumpCount++;
            if (jumpCount == 2) isDoubleJumping = true;

            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0);
            rigid.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);

            collisionCheckCooldown = 0.1f; // 점프 직후 0.1초간 충돌 감지 방지
        }

        if (Input.GetKeyUp(KeyCode.Z) && rigid.linearVelocity.y > 0)
        {
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, rigid.linearVelocity.y * 0.5f);
        }
    }

    #endregion

    #region 대쉬 (Dash)

    private void StartDash()
    {
        isDashing = true;
        anim.SetBool("isDash", true);
        ResetAttackState();
        isWallSliding = false;

        dashTime = dashDuration;
        rigid.gravityScale = 0;

        float dashDirectionX = (moveInput != 0) ? moveInput : lastMoveDirection;
        transform.localScale = new Vector3(dashDirectionX, 1, 1);
        lastMoveDirection = dashDirectionX;

        rigid.linearVelocity = new Vector2(dashDirectionX * dashSpeed, 0f);
        dashCooldownTimer = dashCooldown;
        ghostDelayTime = ghostDelay;
    }

    private void UpdateDash()
    {
        DeactivateAttackHitbox();
        dashTime -= Time.deltaTime;
        MakeGhost();
        if (dashTime <= 0) EndDash();
    }

    private void EndDash()
    {
        isDashing = false;
        anim.SetBool("isDash", false);
        rigid.gravityScale = originalGravity;
        rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
    }

    private void MakeGhost()
    {
        if (ghostPrefab == null) return;
        ghostDelayTime -= Time.deltaTime;
        if (ghostDelayTime <= 0)
        {
            GameObject currentGhost = Instantiate(ghostPrefab, transform.position, transform.rotation);
            if (currentGhost.TryGetComponent<SpriteRenderer>(out var ghostSprite))
            {
                ghostSprite.sprite = GetComponent<SpriteRenderer>().sprite;
                ghostSprite.flipX = transform.localScale.x < 0;
            }
            Destroy(currentGhost, ghostDelete);
            ghostDelayTime = ghostDelay;
        }
    }

    #endregion

    #region 공격 (Attack)

    private void HandleAttackInput()
    {
        if (queuedAttackCount < attackCount) queuedAttackCount++;
        lastAttackTime = Time.time;
        if (!isAttacking)
        {
            currentAttack++;
            PlayAttackAnimation(currentAttack);
        }
    }

    private void PlayAttackAnimation(int attackIndex)
    {
        isAttacking = true;
        anim.SetTrigger("Attack" + attackIndex);
    }

    public void EndAttack() => StartCoroutine(EndAttackDelay());

    private IEnumerator EndAttackDelay()
    {
        yield return new WaitForSeconds(0.06f);
        isAttacking = false;
        if (currentAttack < queuedAttackCount && currentAttack < attackCount)
        {
            currentAttack++;
            isAttacking = true;
            PlayAttackAnimation(currentAttack);
        }
        else
        {
            ResetAttackState();
        }
    }

    private void ResetAttackState()
    {
        isAttacking = false;
        queuedAttackCount = 0;
        currentAttack = 0;
        DeactivateAttackHitbox();
        if (anim != null)
        {
            anim.ResetTrigger("Attack1");
            anim.ResetTrigger("Attack2");
            anim.ResetTrigger("Attack3");
        }
    }

    public void ActivateAttackHitbox()
    {
        if (isDashing) return;
        if (attackHitboxObject != null) attackHitboxObject.SetActive(true);
    }

    public void DeactivateAttackHitbox()
    {
        if (attackHitboxObject != null) attackHitboxObject.SetActive(false);
    }

    #endregion

    #region 패링 (Parry)

    private IEnumerator Parry()
    {
        isParryWindowActive = true;
        isParrying = true;
        wasParrySuccessful = false;
        anim.SetTrigger("Parry"); // 패링 애니메이션 실행
        anim.SetBool("isParry", true);

        yield return new WaitForSeconds(parryDuration);

        // 패링에 성공하지 않았을 경우(시간 초과)에만 상태를 리셋하고 쿨타임을 적용합니다.
        // 성공 시에는 OnTriggerEnter2D에서 즉시 상태를 변경합니다.
        if (!wasParrySuccessful)
        {
            isParryWindowActive = false;
            isParrying = false;
            anim.SetBool("isParry", false); // 패링 애니메이션 종료
            parryCooldownTimer = parryCooldown;
        }
    }

    #endregion

    #region 충돌 및 상태 관리 (Collisions & State)

    private void HandleCollisionChecks()
    {
        if (collisionCheckCooldown > 0) return; // 쿨다운 중이면 충돌 감지 안함

        bool previouslyGrounded = isGrounded;
        // Ground Check using BoxCast
        isGrounded = Physics2D.BoxCast(capsuleCollider.bounds.center, new Vector2(capsuleCollider.bounds.size.x * 0.9f, 0.1f), 0f, Vector2.down, groundCheckDistance, platformLayer);

        // isGrounded가 true이면 isWallSliding은 항상 false가 되도록 수정
        if (isGrounded)
        {
            isWallSliding = false;
            if (!previouslyGrounded) // 막 착지한 경우
            {
                isJumping = false;
                isDoubleJumping = false;
                isFalling = false;
                jumpCount = 0;
            }
        }
        else // 공중에 있는 경우에만 벽 감지
        {
            // 3점 벽 감지 로직
            float direction = transform.localScale.x;
            float checkLength = capsuleCollider.bounds.extents.x + wallCheckDistance;
            Vector2 centerOrigin = capsuleCollider.bounds.center;
            //float verticalOffset = boxCollider.bounds.extents.y * 0.9f;

            //Vector2 upperOrigin = (Vector2)centerOrigin + new Vector2(0, verticalOffset);
            //Vector2 lowerOrigin = (Vector2)centerOrigin - new Vector2(0, verticalOffset);

            bool wallDetectedCenter = Physics2D.Raycast(centerOrigin, new Vector2(direction, 0), checkLength, platformLayer);
            //bool wallDetectedUpper = Physics2D.Raycast(upperOrigin, new Vector2(direction, 0), checkLength, platformLayer);
            //bool wallDetectedLower = Physics2D.Raycast(lowerOrigin, new Vector2(direction, 0), checkLength, platformLayer);

            isWallSliding = wallDetectedCenter;// || wallDetectedUpper || wallDetectedLower;

            if (isWallSliding)
            {
                isDoubleJumping = false;
                jumpCount = 0;
                ResetAttackState();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isParryWindowActive && collision.CompareTag("EnemyAttack"))
        {
            // --- 패링 성공! ---
            wasParrySuccessful = true; // 코루틴에 성공을 알려 쿨타임이 돌지 않게 함
            if (!isBlinking) StartCoroutine(Blink());

            // 즉시 다음 행동이 가능하도록 'isParrying' 상태와 애니메이션을 우선 해제합니다.
            isParrying = false;
            anim.SetBool("isParry", false);

            // 패링 성공 직후의 짧은 무적시간을 부여하고, 패링 판정 창은 즉시 닫습니다.
            parryInvincibilityTimer = 0.2f;
            isParryWindowActive = false; // 무한 자동 패링 버그 수정

            // 패링 성공 효과 (적 경직, 보스 그로기 등)를 여기서 처리합니다.
            MonsterController monster = collision.GetComponentInParent<MonsterController>();
            BossController boss = collision.GetComponentInParent<BossController>();
            if (monster != null)
            {
                monster.TakeGroggyDamage(1);
            }
            else if (boss != null)
            {
                BossManager.Instance.TakeGroggyDamage(1);
            }

            Debug.Log("패링 성공!");
            return; // 피격 처리를 막기 위해 여기서 함수 종료.
        }

        // collision.CompareTag("HitZone") || 
        if (collision.CompareTag("EnemyAttack"))
        {
            Debug.Log("플레이어가 적의 공격에 맞았습니다.");
            if (isHurt || parryInvincibilityTimer > 0) return;
            // 일반 피격
            Vector2 knockDirection = (transform.position.x < collision.transform.position.x) ? Vector2.left : Vector2.right;
            PlayerManager.Instance.TakeDamage(1, knockDirection);
            StartKnockback(knockDirection);
        }

        if (collision.CompareTag("DeadZone"))
        {
            // 사망 존에서는 무조건 죽음 처리
            Vector2 knockDirection = (transform.position.x < collision.transform.position.x) ? Vector2.left : Vector2.right;
            PlayerManager.Instance.TakeDamage(PlayerManager.Instance.MaxHP, knockDirection);
            StartKnockback(knockDirection);
        }
    }

    public void StartKnockback(Vector2 direction, float force)
    {
        //if (isHurt) return; // 이미 아픈 상태면 중복으로 처리하지 않음.
        StartCoroutine(Knockback(direction, force));
    }

    // '힘'을 입력받지 않는, 똑같은 이름의 함수를 하나 더 만들어줍니다.
    public void StartKnockback(Vector2 direction)
    {
        // 이 함수가 호출되면, 자신의 기본 넉백 힘(hurtForce)을 사용해서
        // 원래의 StartKnockback 함수를 대신 호출해줍니다.
        StartKnockback(direction, hurtForce);
    }

    private IEnumerator Knockback(Vector2 direction, float force)
    {
        if (PlayerManager.Instance.HP <= 0) yield break;
        isHurt = true;
        anim.SetTrigger("isHurt");
        rigid.linearVelocity = Vector2.zero;
        rigid.AddForce(direction * force, ForceMode2D.Impulse);
        yield return new WaitForSeconds(hurtDuration);
        isHurt = false;
    }

    private IEnumerator Blink()
    {
        isBlinking = true;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            isBlinking = false;
            yield break;
        }

        Color originalColor = spriteRenderer.color;
        float blinkDuration = 0.3f;
        int blinkCount = 1;
        for (int i = 0; i < blinkCount; i++)
        {
            spriteRenderer.color = new Color32(0, 196, 255, 255); // Color -> Color32로 수정 (0-255 범위)
            yield return new WaitForSeconds(blinkDuration / 2);
            spriteRenderer.color = originalColor; // 원래 색으로 돌아옴
            yield return new WaitForSeconds(blinkDuration / 2);
        }
        isBlinking = false;
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        isDashing = false;
        ResetAttackState();
        rigid.linearVelocity = Vector3.zero;
        anim.SetTrigger("isGameOver");
        GameManager.Instance.GameOver();
    }
    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider2D>();
            if (capsuleCollider == null) return; // 여전히 null이면 Gizmo를 그릴 수 없음
        }

        // Ground Check Gizmo
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)capsuleCollider.bounds.center + Vector2.down * groundCheckDistance, new Vector2(capsuleCollider.bounds.size.x * 0.9f, 0.1f));

        // Wall Check Gizmos
        Gizmos.color = Color.red;
        float direction = transform.localScale.x;
        Vector2 centerOrigin = capsuleCollider.bounds.center;
        float checkLength = capsuleCollider.bounds.extents.x + wallCheckDistance;
        //float verticalOffset = boxCollider.bounds.extents.y * 0.9f; // 상단/하단 체크를 위한 오프셋

        //Vector2 upperOrigin = (Vector2)centerOrigin + new Vector2(0, verticalOffset);
        //Vector2 lowerOrigin = (Vector2)centerOrigin - new Vector2(0, verticalOffset);

        // 3개의 체크 라인 그리기
        Gizmos.DrawLine(centerOrigin, centerOrigin + new Vector2(direction, 0) * checkLength);
        //Gizmos.DrawLine(upperOrigin, upperOrigin + new Vector2(direction, 0) * checkLength);
        //Gizmos.DrawLine(lowerOrigin, lowerOrigin + new Vector2(direction, 0) * checkLength);
    }

    #endregion

    #region 컷씬 동작

    // 외부에서 플레이어 조작을 막거나 허용할 때 사용하는 함수들
    public void DisableControl()
    {
        isInCutscene = true; // 컷신 시작, 플레이어 조작 비활성화
        rigid.linearVelocity = Vector2.zero; // 즉시 정지
        anim.SetBool("isWalk", false); // 걷기 애니메이션 비활성화
        anim.SetBool("isDash", false); // 대쉬 애니메이션 비활성화
        anim.SetBool("isFalling", false); // 떨어지는 애니메이션 비활성화
        anim.ResetTrigger("Attack1");
        anim.ResetTrigger("Attack2");
        anim.ResetTrigger("Attack3"); // 공격 애니메이션 비활성화
        anim.SetBool("isJump", false); // 점프 애니메이션 비활성화
        anim.SetBool("isDoubleJump", false); // 더블 점프 애니메이션 비활성화
    }

    public void EnableControl()
    {
        isInCutscene = false; // 컷신 종료
    }

    // 지정된 위치로 이동만 담당하는 코루틴을 시작시키는 함수
    public Coroutine StartMoveToPosition(Vector2 targetPosition)
    {
        return StartCoroutine(MoveToPositionCoroutine(targetPosition));
    }

    private IEnumerator MoveToPositionCoroutine(Vector2 targetPosition)
    {
        // 이동 방향 결정 및 캐릭터 방향 전환
        float direction = Mathf.Sign(targetPosition.x - transform.position.x);
        transform.localScale = new Vector3(direction, 1, 1); // 캐릭터 방향 전환

        // 걷기 애니메이션 활성화
        anim.SetBool("isWalk", true);

        // 목표 지점의 X축에 충분히 가까워질 때까지 반복
        while (Mathf.Abs(targetPosition.x - transform.position.x) > 0.1f)
        {
            rigid.linearVelocity = new Vector2(direction * moveSpeed, rigid.linearVelocity.y);
            yield return null; // 다음 프레임까지 대기
        }

        // 목표 지점 도착 후
        rigid.linearVelocity = Vector2.zero; // 이동 정지
        anim.SetBool("isWalk", false); // 걷기 애니메이션 비활성화
    }
    #endregion
}