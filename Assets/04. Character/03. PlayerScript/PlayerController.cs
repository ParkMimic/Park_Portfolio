using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("�÷��̾� �⺻ ����")]
    public float moveSpeed = 1f;
    public float jumpForce = 1f;

    private int jumpCount;

    private float moveInput;

    Rigidbody2D rigid;
    SpriteRenderer spriteRenderer;

    void Start()
    {
        // ������Ʈ ����
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // ������ �����ϸ� ���� ī��Ʈ�� �ʱ�ȭ
        jumpCount = 0;
    }

    void Update()
    {
        // �Է� ���� ��ũ��Ʈ
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
        // ���� �̵� ó��
        rigid.linearVelocity = new Vector2(moveInput * moveSpeed, rigid.linearVelocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("OnColiisonEnter Ȯ��");
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // ���� ������ �� ���ʿ� �ִ��� Ȯ��
                if (contact.normal.y > 0.7f)
                {
                    Debug.Log("���� ī��Ʈ �ʱ�ȭ");
                    jumpCount = 0;
                    break;
                }

                if (contact.normal.x > 0.7f || contact.normal.x < -0.7f)
                {
                    Debug.Log("���� �浹 ����");
                    break;
                }
            }
        }
    }
}
