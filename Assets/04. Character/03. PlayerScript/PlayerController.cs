using NUnit.Framework;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public class PlayerController : PlayerManager
{
    [Header("플레이어 기본 설정")]
    public float moveSpeed = 1f; // 이동 속도
    public float jumpForce = 1f; // 점프 힘
    public float wallSlideSpeed = 1f; // 벽 슬라이딩 속도

    [Header("대쉬 설정")]
    public float dashSpeed = 10f; // 대쉬 속도
    public float dashDuration = 0.2f; // 대쉬 지속 시간
    public float dashCooldown = 1f; // 대쉬 쿨타임
    private float dashTime; // 남은 대쉬 시간
    private float dashCooldownTimer = 0f; // 대쉬 쿨타임 타이머
    private Vector2 lastMoveDirection = Vector2.right; // 마지막 이동 방향 (대쉬에 사용)
    private float originalGravity; // 원래 중력값

    [Header("잔상 프리팹")]
    public GameObject ghostPrefab;
    public float ghostDelay = 0.05f; // 잔상 생성 간격
    private float ghostDelayTime; // 잔상 타이머
    public float ghostDelete; // 잔상 삭제 딜레이


    // 넉백 관련 변수
    private float hurtForce = 3f; // 넉백 힘
    private float hurtDuration = 0.5f; // 경직 시간

    // 점프 횟수 카운트
    private int jumpCount;

    // 현재 상태 확인
    private bool isJumping = false;
    private bool isWallSlide = false;
    private bool isWalk = false;
    private bool isDoubleJumping = false;
    private bool isFalling = false;
    private bool isGrounded;
    private bool isGameOver = false;
    private bool _isHurt = false;
    public bool isHurt
    {
        get { return _isHurt; }
        set { _isHurt = value; }
    }

    private bool isDash = false;

    // 이동 확인 변수
    private float moveInput;

    // 공격 관련 변수
    private int attackCount = 3; // 최대 연속 공격 횟수
    private int currentAttack = 0; // 현재 연속 공격 단계 (0~3)
    private float lastAttackTime = 0f; // 마지막 공격 시각
    [SerializeField] private float comboResetTime = 1.0f; // 콤보 리셋 시간 (초)

    private bool isAttacking = false; // 공격 중 상태 확인
    private int queuedAttackCount = 0; // 사용자가 누른 X의 총 횟수


    Rigidbody2D rigid;
    SpriteRenderer spriteRenderer;
    Animator anim;

    void Start()
    {
        // 컴포넌트 선언
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // 게임을 시작 후 설정 초기화
        jumpCount = 0; // 점프 횟수 초기화
        originalGravity = rigid.gravityScale; // 중력 초깃값 저장

    }

    void Update()
    {
        if (isGameOver || isHurt)
        {
            isAttacking = false;
            queuedAttackCount = 0;
            currentAttack = 0;
            return;
        }

        if (!isAttacking)
        {
            // 입력 감지 스크립트
            moveInput = Input.GetAxisRaw("Horizontal");

            // 입력 감지
            if (moveInput != 0)
            {
                isWalk = true;
                if (!isWallSlide)
                {
                    if (moveInput == -1)
                    {
                        spriteRenderer.flipX = true;
                    }
                    else
                    {
                        spriteRenderer.flipX = false;
                    }
                }
            }
            else
            {
                isWalk = false;
            }

            // 점프 관련 로직
            if (jumpCount < 2)
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    if (isWallSlide) isWallSlide = false;

                    rigid.linearVelocity = Vector2.zero;
                    rigid.AddForceY(jumpForce, ForceMode2D.Impulse);

                    // 점프 중임을 확인
                    isJumping = true;
                    isGrounded = false; // 점프하는 중에는 땅이 아님
                    jumpCount++;

                    // 만일 더블 점프 상태라면
                    if (jumpCount == 2) isDoubleJumping = true; // 더블 점프를 true 로 줌으로서 애니메이션을 감지함.
                }

                if (Input.GetKeyUp(KeyCode.Z) && rigid.linearVelocityY > 0)
                {
                    rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, rigid.linearVelocity.y * 0.5f);
                }
            }
        }


        // 공격 관련 로직
        // 콤보 리셋 조건: 일정 시간 안 누르면 초기화
        if (Time.time - lastAttackTime > comboResetTime)
        {
            currentAttack = 0;
            queuedAttackCount = 0;
        }


        // X키 입력 시
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (queuedAttackCount < attackCount)
            {
                queuedAttackCount++;
            }

            lastAttackTime = Time.time;

            if (!isAttacking)
            {
                currentAttack++;
                isAttacking = true;

                // 공격 시작 시점에만 속도 0으로 초기화
                Vector2 vel = rigid.linearVelocity;
                vel.x = 0;
                rigid.linearVelocity = vel;

                PlayAttackAnimation(currentAttack);
            }
        }


        // 대쉬 로직
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0 && !isDash)
        {
            StartDash();
        }

        if (isDash)
        {
            UpdateDash();
            return; // 대쉬 중에는 다른 모든 Update 로직을 건너뜀.
        }


        // 플레이어의 체력 감지
        if (PlayerManager.Instance.HP <= 0 && !isGameOver)
        {
            GameOver();
        }


        // 애니메이션 파라미터 설정
        anim.SetBool("isWalk", isWalk);
        anim.SetBool("isJump", isJumping);
        anim.SetBool("isDoubleJump", isDoubleJumping);
        anim.SetBool("isFalling", isFalling);
        anim.SetBool("isWallSlide", isWallSlide);
    }


    private void FixedUpdate()
    {
        // 특정 상태에서는 물리 처리를 중단
        if (isGameOver || isHurt || isDash) return;


        if (isAttacking)
        {
            if (isGrounded)
            {
                // 땅에 있을 때만 속도를 0으로 만들기
                rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            }
            else
            {
                // 공중에선 x 속도를 유지하거나 입력 받아서 자연스럽게 이동
                rigid.linearVelocity = new Vector2(moveInput * moveSpeed, rigid.linearVelocity.y);
            }
            return;
        }

        if (isWallSlide)
        {
            // 벽 슬라이딩 중일 땐 수평 이동을 막고, 낙하 속도만 제한
            rigid.linearVelocity = new Vector2(0, Mathf.Max(rigid.linearVelocity.y, -wallSlideSpeed));
        }
        else
        {
            // 평소 이동 처리
            rigid.linearVelocity = new Vector2(moveInput * moveSpeed, rigid.linearVelocity.y);
        }


        isFalling = !isGrounded && rigid.linearVelocity.y < 0;
    }

    void StartDash()
    {
        isDash = true;
        anim.SetBool("isDash", true);
        isAttacking = false; // 공격 중이었다면 캔슬
        isWallSlide = false; // 벽 슬라이딩 중이었다면 캔슬
        currentAttack = 0;
        queuedAttackCount = 0;

        dashTime = dashDuration;
        rigid.gravityScale = 0;

        float dashDirectionX = (moveInput != 0) ? moveInput : (spriteRenderer.flipX ? -1 : 1);
        rigid.linearVelocity = new Vector2(dashDirectionX * dashSpeed, 0f);

        dashCooldownTimer = dashCooldown;
        ghostDelayTime = ghostDelay; // 첫 잔상은 바로 생성
    }

    void UpdateDash()
    {
        dashTime -= Time.deltaTime;
        MakeGhost();

        if (dashTime <= 0)
        {
            EndDash();
        }
    }

    void EndDash()
    {
        isDash = false;
        anim.SetBool("isDash", false);
        rigid.gravityScale = originalGravity;
        rigid.linearVelocity = new Vector2(0, rigid.linearVelocityY);
    }

    void MakeGhost()
    {
        if (ghostPrefab == null) return;

        ghostDelayTime -= Time.deltaTime;
        if (ghostDelayTime <= 0)
        {
            GameObject currentGhost = Instantiate(ghostPrefab, transform.position, transform.rotation);
            SpriteRenderer ghostSprite = currentGhost.GetComponent<SpriteRenderer>();

            ghostSprite.sprite = spriteRenderer.sprite;
            ghostSprite.flipX = spriteRenderer.flipX;

            Destroy(currentGhost, ghostDelete);
            ghostDelayTime = ghostDelay;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // 바닥 위에 서 있는지 확인
                if (contact.normal.y > 0.7f)
                {
                    Debug.Log("점프 카운트 초기화");
                    jumpCount = 0; // 점프 카운트 초기화
                    isJumping = false; // 점프 중이 아님을 알려줌.
                    isWallSlide = false; // 벽 슬라이딩이 아님을 알려줌.
                    isDoubleJumping = false; // 더블 점프 상태도 아님을 알려줌.
                    isFalling = false; // 떨어지는 중이 아님
                    isGrounded = true; // 바닥에 서 있음을 확인
                    break;
                }
            }
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // 바닥에 붙어 있지 않을 경우, 벽면에 접촉 시 벽 슬라이딩 구현
                if (!isGrounded && (contact.normal.x > 0.7f || contact.normal.x < -0.7f))
                {
                    Debug.Log("벽면 충돌 감지");
                    isWallSlide = true; // WallSlide 중임을 알려줌
                    isDoubleJumping = false; // 더블 점프 상태도 초기화
                    jumpCount = 0; // 벽 슬라이딩 중, 점프 초기화

                    // 공격 상태 초기화
                    isAttacking = false; // 공격 바로 종료
                    currentAttack = 0;
                    queuedAttackCount = 0;

                    // 모든 공격 애니메이션 트리거를 리셋하여 현재 애니메이션을 강제로 중단
                    anim.ResetTrigger("Attack1");
                    anim.ResetTrigger("Attack2");
                    anim.ResetTrigger("Attack3");
                    break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Debug.Log("벽에서 떨어짐");
            isWallSlide = false;
        }

        if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("바닥에서 떨어짐");
            isGrounded = false;
        }
    }


    // 함정 감지
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HitZone") && !isHurt)
        {
            PlayerManager.Instance.HP -= 1;

            // 플레이어 넉백 방향 계산
            Vector2 knockDirection = (transform.position.x < collision.transform.position.x)
                ? Vector2.left
                : Vector2.right;

            StartKnockback(knockDirection);
        }
    }

    public void StartKnockback(Vector2 knockDirection)
    {
        StartCoroutine(Knockback(knockDirection));
    }


    private IEnumerator Knockback(Vector2 direction)
    {
        // 체력이 0 이하면 아무 것도 하지 않고 종료
        if (PlayerManager.Instance.HP <= 0) yield break;

        isHurt = true;

        anim.ResetTrigger("isHurt"); // 중복 방지
        anim.SetTrigger("isHurt");

        // 넉백 적용
        rigid.linearVelocity = Vector2.zero;
        rigid.AddForce(direction * hurtForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(hurtDuration);

        isHurt = false;
    }

    // 공격 애니메이션 실행 함수
    void PlayAttackAnimation(int attackIndex)
    {
        anim.SetTrigger("Attack" + attackIndex); // 예: Attack1, Attack2, Attack3
    }

    // 공격 종료
    public void EndAttack()
    {
        StartCoroutine(EndAttackDelay());
    }

    // 공격 종료까지 딜레이 (동작의 자연스러움을 위함)
    private IEnumerator EndAttackDelay()
    {
        yield return new WaitForSeconds(0.06f); // 0.05 초의 여유를 주고 처리함
        isAttacking = false;

        if (currentAttack < queuedAttackCount && currentAttack < attackCount)
        {
            currentAttack++;
            isAttacking = true;
            PlayAttackAnimation(currentAttack);
        }
        else
        {
            // 콤보 종료 (입력된 것 이상 실행하지 않음)
            queuedAttackCount = 0;
            currentAttack = 0;
        }
    }


    void GameOver()
    {
        isGameOver = true;
        rigid.linearVelocity = Vector3.zero;
        anim.SetTrigger("isGameOver");
    }
}
