using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("플레이어 기본 설정")]
    public float moveSpeed = 1f;
    public float jumpForce = 1f;

    private int jumpCount;

    private float moveInput;

    Rigidbody2D rigid;
    SpriteRenderer spriteRenderer;

    void Start()
    {
        // 컴포넌트 선언
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 게임을 시작하면 점프 카운트를 초기화
        jumpCount = 0;
    }

    void Update()
    {
        // 입력 감지 스크립트
        moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput != 0)
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

        if (jumpCount < 2)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                rigid.linearVelocity = Vector2.zero;
                rigid.AddForceY(jumpForce, ForceMode2D.Impulse);

                jumpCount++;
            }
        }
    }

    private void FixedUpdate()
    {
        // 실제 이동 처리
        rigid.linearVelocity = new Vector2(moveInput * moveSpeed, rigid.linearVelocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("OnColiisonEnter 확인");
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // 접촉 지점이 내 위쪽에 있는지 확인
                if (contact.normal.y > 0.7f)
                {
                    Debug.Log("점프 카운트 초기화");
                    jumpCount = 0;
                    break;
                }

                if (contact.normal.x > 0.7f || contact.normal.x < -0.7f)
                {
                    Debug.Log("벽면 충돌 감지");
                    break;
                }
            }
        }
    }
}
