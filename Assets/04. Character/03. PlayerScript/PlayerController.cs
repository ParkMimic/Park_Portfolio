using System.Collections;
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
    private bool isParrying = false;

    [Header("잔상 효과 설정")]
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private float ghostDelay = 0.05f;
    [SerializeField] private float ghostDelete;

    [Header("피격 설정")]
    [SerializeField] private float hurtForce = 3f;
    [SerializeField] private float hurtDuration = 0.5f;

    // -- 컴포넌트 변수 --
    private Rigidbody2D rigid;
    private Animator anim;

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

        if (Input.GetKeyDown(KeyCode.C) && !isAttacking && !isDashing && !isParrying)
        {
            StartCoroutine(Parry());
        }
    }

    private void HandleTimers()
    {
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (Time.time - lastAttackTime > comboResetTime) ResetAttackState();
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
        isParrying = true;
        anim.SetTrigger("Parry"); // 패링 애니메이션 실행
        yield return new WaitForSeconds(parryDuration);
        isParrying = false;
    }

    #endregion

    #region 충돌 및 상태 관리 (Collisions & State)

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.7f)
                {
                    isGrounded = true;
                    isJumping = false;
                    isDoubleJumping = false;
                    isFalling = false;
                    isWallSliding = false;
                    jumpCount = 0;
                    break;
                }
            }
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (!isGrounded && Mathf.Abs(contact.normal.x) > 0.7f)
                {
                    isWallSliding = true;
                    isDoubleJumping = false;
                    jumpCount = 0;
                    ResetAttackState();
                    break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) isGrounded = false;
        if (collision.gameObject.CompareTag("Wall")) isWallSliding = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HitZone") && !IsHurt)
        {
            if (isParrying)
            {
                // 패링 성공!
                MonsterController monster = collision.GetComponentInParent<MonsterController>();
                if (monster != null)
                {
                    monster.TakeGroggyDamage(1);
                    Debug.Log("패링 성공! 몬스터 그로기 수치 +1");
                }
                isParrying = false; // 패링 성공 시 즉시 상태 해제
            }
            else
            {
                // 일반 피격
                Vector2 knockDirection = (transform.position.x < collision.transform.position.x) ? Vector2.left : Vector2.right;
                PlayerManager.Instance.TakeDamage(1, knockDirection);
                StartKnockback(knockDirection);
            }
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
        ResetAttackState();
        rigid.linearVelocity = Vector3.zero;
        anim.SetTrigger("isGameOver");
    }

    #endregion
}