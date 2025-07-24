using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [Header("�⺻ ����")]
    public float moveSpeed = 2f;
    public int maxHealth = 3;
    private int currentHealth;

    [Header("���� ����")]
    public int contactDamage = 1; // ���� �� �����
    public int attackDamage = 1; // Į ���� �����
    public float attackRange = 1.5f; // �� ���� �ȿ� ������ ���� ����
    public float attackCooldown = 2f; // ���� �� ���� ���ݱ����� ��� �ð�
    public float telegraphDuration = 0.5f; // ���� �� �Ӱ� ����Ǵ� �ð�

    [Header("�÷��̾� ����")]
    public Transform player; // �÷��̾� Transform
    public LayerMask playerLayer; // �÷��̾� ���̾�

    // ���� ����
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Color originalColor;

    private bool isAttacking = false;
    private bool isDead = false;
    private float lastAttackTime;

    private void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // ���� ���� �� �÷��̾ �ڵ����� ã�� (�±װ� "Player"�� ���� �Ǿ� �־�� ��.)
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogError("�÷��̾� ������Ʈ�� ã�� �� �����ϴ�. 'Player' �±׸� Ȯ�����ּ���.");
                enabled = false; // ��ũ��Ʈ ��Ȱ��ȭ
            }
        }
    }

    private void Update()
    {
        if (isDead || player == null) return;

        // �÷��̾���� �Ÿ� ���
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // ���� ��Ÿ���� ������, �÷��̾ ���� ���� �ȿ� ������ ���� ���� ���� �ƴ϶��
        if (Time.time > lastAttackTime + attackCooldown && distanceToPlayer <= attackRange && !isAttacking)
        {
            StartCoroutine(AttackSequence());
        }

        // �÷��̾ ���� �ٶ󺸵��� ��������Ʈ ������ (���� ���� �ƴ� ����)
        if (!isAttacking)
        {
            if (player.position.x < transform.position.x)
            {
                spriteRenderer.flipX = true; // �÷��̾ ���ʿ� ������ ���� ����
            }
            else
            {
                spriteRenderer.flipX = false; // �÷��̾ �����ʿ� ������ ������ ����
            }
        }
    }

    private void FixedUpdate()
    {
        // �׾��ų�, ���� ���̰ų�, �÷��̾ ���� ���� �ȿ� ������ �������� ����
        if (isDead || isAttacking || Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            anim.SetBool("isWalking", false);
            return;
        }

        // �÷��̾ ���� �̵�
        Vector2 direction = (player.position - transform.position).normalized;
        rigid.linearVelocity = new Vector2(direction.x * moveSpeed, rigid.linearVelocity.y);
        anim.SetBool("isWalking", true);
    }
}
