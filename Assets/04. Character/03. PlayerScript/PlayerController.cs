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

    [Header("잔상 효과 설정")]
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private float ghostDelay = 0.05f;
    [SerializeField] private float ghostDelete;

    [Header("피격 설정")]
    [SerializeField] private float hurtForce = 3f;
    [SerializeField] private float hurtDuration = 0.5f;

    [Header("레이캐스트 설정")]
    [SerializeField] private LayerMask platformLayer; // 바닥과 벽을 감지할 통합 레이어
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float wallCheckDistance = 0.1f;

    // -- 컴포넌트 변수 --
    private Rigidbody2D rigid;
    private Animator anim;
    private BoxCollider2D boxCollider; // Raycast를 위해 BoxCollider2D 추가

    // -- 상태(State) 변수 --
    public bool IsHurt { get; private set; }
    private bool isGrounded;
    private bool isJumping;
    private bool isDoubleJumping;
    private bool isFalling;
    private bool isDashing;
    private bool isWallSliding;
    private bool isAttacking;
    private bool isGameOver;

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

    #endregion

    #region 기본 함수

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>(); // 컴포넌트 가져오기
    }

    private void Start()
    {
        originalGravity = rigid.gravityScale;
    }

    private void Update()
    {
        if (isGameOver || IsHurt)
        {
            if (IsHurt) ResetAttackState();
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
        if (isGameOver || IsHurt || isDashing) return;
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
        if (jumpCount < 2 && Input.GetKeyDown(KeyCode.Z))
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

        yield return new WaitForSeconds(parryDuration);

        isParryWindowActive = false;

        isParrying = false;
        if (!wasParrySuccessful)
        {
            parryCooldownTimer = parryCooldown; // 패링 실패 시 쿨타임 설정
        }
    }

    #endregion

    #region 충돌 및 상태 관리 (Collisions & State)

    private void HandleCollisionChecks()
    {
        if (collisionCheckCooldown > 0) return; // 쿨다운 중이면 충돌 감지 안함

        bool previouslyGrounded = isGrounded;
        // Ground Check using BoxCast
        isGrounded = Physics2D.BoxCast(boxCollider.bounds.center, new Vector2(boxCollider.bounds.size.x * 0.9f, 0.1f), 0f, Vector2.down, groundCheckDistance, platformLayer);

        if (!previouslyGrounded && isGrounded)
        {
            isJumping = false;
            isDoubleJumping = false;
            isFalling = false;
            isWallSliding = false;
            jumpCount = 0;
        }

        // Wall Check using Raycast
        if (!isGrounded)
        {
            float direction = transform.localScale.x;
            isWallSliding = Physics2D.Raycast(boxCollider.bounds.center, new Vector2(direction, 0), boxCollider.bounds.extents.x + wallCheckDistance, platformLayer);

            if (isWallSliding)
            {
                isDoubleJumping = false;
                jumpCount = 0;
                ResetAttackState();
            }
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isParryWindowActive && collision.CompareTag("EnemyAttack"))
        {
            MonsterController monster = collision.GetComponentInParent<MonsterController>();
            if (monster != null)
            {
                monster.TakeGroggyDamage(1);
                Debug.Log("패링 성공! 몬스터 그로기 수치 + 1");
            }
            wasParrySuccessful = true; // 패링 성공!
            isParryWindowActive = false; // 성공했으므로 창을 바로 닫음
            isParrying = false; // 패링 상태 종료
            return; // 피격 처리를 막기 위해 여기서 함수 종료.
        }

        if ((collision.CompareTag("HitZone") || collision.CompareTag("EnemyAttack")))
        {
            if (IsHurt) return;
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

    public void StartKnockback(Vector2 direction)
    {
        if (IsHurt) return; // 이미 아픈 상태면 중복으로 처리하지 않음.
        StartCoroutine(Knockback(direction));
    }

    private IEnumerator Knockback(Vector2 direction)
    {
        if (PlayerManager.Instance.HP <= 0) yield break;
        IsHurt = true;
        anim.SetTrigger("isHurt");
        rigid.linearVelocity = Vector2.zero;
        rigid.AddForce(direction * hurtForce, ForceMode2D.Impulse);
        yield return new WaitForSeconds(hurtDuration);
        IsHurt = false;
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        isDashing = false;
        ResetAttackState();
        rigid.linearVelocity = Vector3.zero;
        anim.SetTrigger("isGameOver");
    }
    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) return; // 여전히 null이면 Gizmo를 그릴 수 없음
        }

        // Ground Check Gizmo
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)boxCollider.bounds.center + Vector2.down * groundCheckDistance, new Vector2(boxCollider.bounds.size.x * 0.9f, 0.1f));

        // Wall Check Gizmo
        Gizmos.color = Color.red;
        float direction = transform.localScale.x;
        Vector2 origin = boxCollider.bounds.center;
        Gizmos.DrawLine(origin, origin + new Vector2(direction, 0) * (boxCollider.bounds.extents.x + wallCheckDistance));
    }

    #endregion
}