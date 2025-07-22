using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody2D rigid;
    Transform transform;

    // �̵� �� ���� ���� ����
    public float moveSpeed = 5.0f;
    public float jumpPower = 10.0f;

    // �� �����̵� ���� ����
    public float wallSlidingSpeed = 1f; // ������ �̲������� �ӵ�

    // �� ���� ��
    public Vector2 wallJumpForce = new Vector2(7f, 12f);

    // ���� ����
    private bool isJumping = false;
    private bool isSliding = false;

    // ���� ������ ���� (-1 �Ǵ� 1)
    private float wallDirectionX;

    private void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 1. ���� �Է� ó��
        // 1-1. �� ����: �� �����̵� �߿� ���� ��ư�� ������ ��
        if (Input.GetKeyDown(KeyCode.Z) && isSliding)
        {
            isSliding = false; // �� ������ �ϸ� �� ��� �����̵� ���� ����
            isJumping = true; // ���� ���·� ����

            // ���� �ӵ��� 0 ���� �ʱ�ȭ�Ͽ� �ϰ��� ���� ���� �޵��� ��.
            rigid.linearVelocity = Vector2.zero;

            // �� �ݴ� �������� ���� ����
            // wallDirectionX�� 1 (������ ��)�̸�, x ���� -1(����) �������� ����
            rigid.AddForce(new Vector2(wallJumpForce.x * -wallDirectionX, wallJumpForce.y), ForceMode2D.Impulse);
        }
        // 1-2. �Ϲ� ����: ���� �ְ�, ���� �پ����� ���� ��
        else if (Input.GetKeyDown(KeyCode.Z) && !isJumping && !isSliding)
        {
            rigid.AddForce(new Vector2(0f, jumpPower), ForceMode2D.Impulse);

            // isJumping ���¸� true�� ����
            isJumping = true;
        }

        // 2. ���� �̵� ó��
        float moveInput = Input.GetAxisRaw("Horizontal"); // �¿� ����Ű �Է� (-1~1)

        // �� �����̵� ���°� �ƴ� ���� �Ϲ� �̵�
        if (!isSliding)
        {
            rigid.linearVelocity = new Vector2(moveInput * moveSpeed, rigid.linearVelocity.y);
        }
        // �� �����̵� ������ ���� Ư�� ����
        else
        {
            // ������ �̲������� �ӵ� ����
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, Mathf.Clamp(rigid.linearVelocity.y, -wallSlidingSpeed, float.MaxValue));

            // �� �ݴ� �������� �Է��� ������ �� �����̵� ����
            // moveInput�� ��ȣ�� wallDirectionX �� ��ȣ�� ���� �� (��: �����ʺ� (wallDir=1) ���� ����(moveInput=-1)���� ���� ��)
            if (moveInput != 0 && Mathf.Sign(moveInput) != wallDirectionX)
            {
                isSliding = false;
            }
        }
    }

    // 4. �ٴ� �� �� ���� (�浹�� �����Ǵ� ���� ��� ȣ��)
    private void OnCollisionStay2D(Collision2D collision)
    {
        // �浹�� ���� ������Ʈ�� �±װ� "Ground" �� ���� ó��
        if (collision.gameObject.CompareTag("Ground"))
        {
            // ���� ���� ��
            bool onGround = false;

            // Collision2D ���� �浹 ���� (contacts) ������ ��ȸ
            foreach (var contact in collision.contacts)
            {
                // ���� ���ͷ� �ٴ� üũ
                if (contact.normal.y > 0.7f)
                {
                    onGround = true;
                    isJumping = false;
                    isSliding = false; // �ٴڿ� ������� �� �����̵� ���� ����
                    break; // �ٴ����� Ȯ�������� ���� ����
                }
            }

            // �ٴ��� �ƴ� ��쿡�� �� üũ
            if(!onGround)
            {
                foreach (var contact in collision.contacts)
                {
                    // �浹 ������ ���� ���Ͱ� ���� ������ ������ ����
                    if (Mathf.Abs(contact.normal.x) > 0.7f)
                    {
                        // ���߿� ���� ���� �� �����̵� Ȱ��ȭ
                        if (isJumping)
                        {
                            isSliding = true;
                            // ���� ���� ���� (���� ���Ϳ� �ݴ� ����)
                            // ���� ���� ������ ������(1)��, ������ ���� ������ ����(-1)�� ����
                            wallDirectionX = Mathf.Sign(contact.normal.x) * -1;
                            break; // ������ Ȯ�������� ���� ����
                        }
                    }
                }
            }
        }
    }

    // �浹�� ������ �� ȣ��
    private void OnCollisionExit2D(Collision2D collision)
    {
        // "Ground" �±׸� ���� ������Ʈ���� �������� ��
        if (collision.gameObject.CompareTag("Ground"))
        {
            // �� �����̵� ���¿��ٸ� ����
            isSliding = false;
            // ������ ���������� ���� ���·� ����
            isJumping = true;
        }
    }
}
