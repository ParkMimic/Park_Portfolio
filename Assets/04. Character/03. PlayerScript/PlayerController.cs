using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("플레이어 기본 설정")]
    public float moveSpeed = 1f;
    public float jumpForce = 1f;
    public float dashSpeed = 2f;
    public float wallSlideSpeed = 1f;

    // 넉백 관련 함수
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
    private bool isHurt = false;

    // 이동 확인 함수
    private float moveInput;

    // 공격 관련 함수
    private int attackCount = 3; // 최대 연속 공격 횟수
    private int currentAttack = 0; // 현재 연속 공격 단계 (0~3)
    private float lastAttackTime = 0f; // 마지막 공격 시각
    [SerializeField] private float comboResetTime = 1.0f; // 콤보 리셋 시간 (초)

    Rigidbody2D rigid;
    SpriteRenderer spriteRenderer;
    Animator anim;

    void Start()
    {
        // 컴포넌트 선언
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // 게임을 시작하면 점프 카운트를 초기화
        jumpCount = 0;
    }

    void Update()
    {
        if (isGameOver || isHurt) return;

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

        // 전체 이동 로직
        if (jumpCount < 2)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (isWallSlide)
                {
                    isWallSlide = false; // 벽 점프 시 벽 슬라이드 해제
                }

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

        // 공격 관련 로직
        // 콤보 리셋 조건: 일정 시간 안 누르면 초기화
        if (Time.time - lastAttackTime > comboResetTime)
        {
            currentAttack = 0;
        }

        // X키 입력 시
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (currentAttack < attackCount)
            {
                currentAttack++;
                lastAttackTime = Time.time;

                // 애니메이터에 공격 단계 전달
                anim.SetTrigger("Attack" + currentAttack);
            }
        }

        // 플레이어의 체력 감지
        if (PlayerManager.Instance.HP <= 0 && !isGameOver)
        {
            GameOver();
        }

        // 애니메이션 감지
        anim.SetBool("isWalk", isWalk);
        anim.SetBool("isJump", isJumping);
        anim.SetBool("isDoubleJump", isDoubleJumping);
        anim.SetBool("isFalling", isFalling);
        anim.SetBool("isWallSlide", isWallSlide);
    }

    private void FixedUpdate()
    {
        if (isGameOver || isHurt) return;

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("OnColiisonEnter 확인");
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
                if (isJumping) // 점프 중일 때만 벽 슬라이딩 가능
                {
                    if (contact.normal.x > 0.7f || contact.normal.x < -0.7f) // OnCollision의 벽면을 확인
                    {
                        Debug.Log("벽면 충돌 감지");
                        isWallSlide = true; // WallSlide 중임을 알려줌
                        isDoubleJumping = false; // 더블 점프 상태도 초기화
                        jumpCount = 0; // 벽 슬라이딩 중, 점프 1회 초기화
                        break;
                    }
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
            Debug.Log("벽에서 떨어짐");
            isGrounded = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HitZone") && !isHurt)
        {
            PlayerManager.Instance.HP -= 1;

            // 플레이어 넉백 방향 계산
            Vector2 knockDirection = (transform.position.x < collision.transform.position.x)
                ? Vector2.left
                : Vector2.right;

            StartCoroutine(Knockback(knockDirection));
        }
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


    void GameOver()
    {
        isGameOver = true;
        rigid.linearVelocity = Vector3.zero;
        anim.SetTrigger("isGameOver");
    }
}
