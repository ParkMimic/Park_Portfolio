using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody2D rigid;
    Transform transform;

    // 이동 및 점프 관련 변수
    public float moveSpeed = 5.0f;
    public float jumpPower = 10.0f;

    // 벽 슬라이딩 관련 변수
    public float wallSlidingSpeed = 1f; // 벽에서 미끌어지는 속도

    // 벽 점프 힘
    public Vector2 wallJumpForce = new Vector2(7f, 12f);

    // 상태 변수
    private bool isJumping = false;
    private bool isSliding = false;

    // 벽의 방향을 저장 (-1 또는 1)
    private float wallDirectionX;

    private void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 1. 점프 입력 처리
        // 1-1. 벽 점프: 벽 슬라이딩 중에 점프 버튼을 눌렀을 때
        if (Input.GetKeyDown(KeyCode.Z) && isSliding)
        {
            isSliding = false; // 벽 점프를 하면 그 즉시 슬라이딩 상태 해제
            isJumping = true; // 점프 상태로 변경

            // 기존 속도를 0 으로 초기화하여 일관된 점프 힘을 받도록 함.
            rigid.linearVelocity = Vector2.zero;

            // 벽 반대 방향으로 힘을 가함
            // wallDirectionX가 1 (오른쪽 벽)이면, x 힘은 -1(왼쪽) 방향으로 적용
            rigid.AddForce(new Vector2(wallJumpForce.x * -wallDirectionX, wallJumpForce.y), ForceMode2D.Impulse);
        }
        // 1-2. 일반 점프: 땅에 있고, 벽에 붙어있지 않을 때
        else if (Input.GetKeyDown(KeyCode.Z) && !isJumping && !isSliding)
        {
            rigid.AddForce(new Vector2(0f, jumpPower), ForceMode2D.Impulse);

            // isJumping 상태를 true로 변경
            isJumping = true;
        }

        // 2. 수평 이동 처리
        float moveInput = Input.GetAxisRaw("Horizontal"); // 좌우 방향키 입력 (-1~1)

        // 벽 슬라이딩 상태가 아닐 때의 일반 이동
        if (!isSliding)
        {
            rigid.linearVelocity = new Vector2(moveInput * moveSpeed, rigid.linearVelocity.y);
        }
        // 벽 슬라이딩 상태일 때의 특별 로직
        else
        {
            // 벽에서 미끄러지는 속도 제한
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, Mathf.Clamp(rigid.linearVelocity.y, -wallSlidingSpeed, float.MaxValue));

            // 벽 반대 방향으로 입력이 들어오면 벽 슬라이딩 해제
            // moveInput의 부호와 wallDirectionX 의 부호가 같을 때 (예: 오른쪽벽 (wallDir=1) 에서 왼쪽(moveInput=-1)으로 누를 때)
            if (moveInput != 0 && Mathf.Sign(moveInput) != wallDirectionX)
            {
                isSliding = false;
            }
        }
    }

    // 4. 바닥 및 벽 감지 (충돌이 유지되는 동안 계속 호출)
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 충돌한 게임 오브젝트의 태그가 "Ground" 일 때만 처리
        if (collision.gameObject.CompareTag("Ground"))
        {
            // 지상에 있을 때
            bool onGround = false;

            // Collision2D 에서 충돌 지점 (contacts) 정보를 순회
            foreach (var contact in collision.contacts)
            {
                // 법선 벡터로 바닥 체크
                if (contact.normal.y > 0.7f)
                {
                    onGround = true;
                    isJumping = false;
                    isSliding = false; // 바닥에 닿았으니 벽 슬라이딩 상태 해제
                    break; // 바닥임을 확인했으면 루프 종료
                }
            }

            // 바닥이 아닐 경우에만 벽 체크
            if(!onGround)
            {
                foreach (var contact in collision.contacts)
                {
                    // 충돌 지점의 법선 벡터가 수평에 가까우면 벽으로 간주
                    if (Mathf.Abs(contact.normal.x) > 0.7f)
                    {
                        // 공중에 있을 때만 벽 슬라이딩 활성화
                        if (isJumping)
                        {
                            isSliding = true;
                            // 벽의 방향 저장 (법선 벡터와 반대 방향)
                            // 왼쪽 벽의 법선은 오른쪽(1)을, 오른쪽 벽의 법선은 왼쪽(-1)을 향함
                            wallDirectionX = Mathf.Sign(contact.normal.x) * -1;
                            break; // 벽임을 확인했으면 루프 종료
                        }
                    }
                }
            }
        }
    }

    // 충돌이 끝났을 때 호출
    private void OnCollisionExit2D(Collision2D collision)
    {
        // "Ground" 태그를 가진 오브젝트에서 떨어졌을 때
        if (collision.gameObject.CompareTag("Ground"))
        {
            // 벽 슬라이딩 상태였다면 해제
            isSliding = false;
            // 땅에서 떨어졌으니 점프 상태로 변경
            isJumping = true;
        }
    }
}
