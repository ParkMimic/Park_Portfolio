using System.Collections;
using UnityEditor.Rendering;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [Header("�⺻ ����")]
    public float moveSpeed = 2f;
    public float maxHealth = 3;
    private float currentHealth;

    [Header("���� ����")]
    public int contactDamage = 1; // ���� �� �����
    public int attackDamage = 1; // Į ���� �����
    public float attackRange = 3f; // �� ���� �ȿ� ������ ���� ����
    public float attackCooldown = 2f; // ���� �� ���� ���ݱ����� ��� �ð�
    public float telegraphDuration = 0.5f; // ���� �� �Ӱ� ����Ǵ� �ð�

    [Header("�÷��̾� ����")]
    public Transform player; // �÷��̾� Transform
    public LayerMask playerLayer; // �÷��̾� ���̾�

    [Header("�þ� ����")]
    public float visionRange = 10f; // �÷��̾ ������ �� �ִ� �ִ� �Ÿ�
    public float loseSightDistance = 15f; // �÷��̾ �� �Ÿ� �̻� �־����� �þ߸� ����
    private bool hasSpottedPlayer = false; // �÷��̾ �����ߴ��� ����

    // ���� ����
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Color originalColor;

    private bool isAttacking = false;
    private bool isDead = false;
    private float lastAttackTime;
    private bool isHurt = false; // ���Ͱ� ���� �������� ����

    private void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        attackCooldown = 0; // ���� ��Ÿ�� �ʱ�ȭ
        lastAttackTime = 0; // ������ ���� �ð��� �ʱ�ȭ

        originalColor = spriteRenderer.color;

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
        if (isDead || player == null || PlayerManager.Instance.HP <= 0) return;

        // �÷��̾ �߰����� ���ߴٸ�, �þ� ���� �ִ��� Ȯ��
        if (!hasSpottedPlayer)
        {
            CheckForPlayerInSight();
        }
        else // �÷��̾ �߰��ߴٸ�
        {
            // �÷��̾���� �Ÿ� ���
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // ���� �Ÿ��� '��ġ�� �Ÿ�'���� �־����ٸ�
            if (distanceToPlayer > loseSightDistance)
            {
                hasSpottedPlayer = false; // �÷��̾ ���ƴٰ� �Ǵ�.
                return; // ���� �� ���� ������ �� �̻� �������� ����.
            }

            // ���� ��Ÿ���� ������, �÷��̾ ���� ���� �ȿ� ������ ���� ���� ���� �ƴ϶��
            if (Time.time >= lastAttackTime + attackCooldown && distanceToPlayer <= attackRange && !isAttacking)
            {
                StartCoroutine(AttackSequence());
            }

            // �÷��̾ ���� �ٶ󺸵��� ��������Ʈ ������ (���� ���� �ƴ� ����)
            if (!isAttacking)
            {
                if (player.position.x < transform.position.x)
                {
                    transform.localScale = new Vector3(1, 1, 1); // �÷��̾ ���ʿ� ������ ���� ����
                }
                else
                {
                    transform.localScale = new Vector3(-1, 1, 1); // �÷��̾ �����ʿ� ������ ������ ����
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // �׾��ų�, ���� ���̰ų�, �÷��̾ ���� ���� �ȿ� ������ �������� ����
        if (isDead || isAttacking || !hasSpottedPlayer || Vector2.Distance(transform.position, player.position) <= attackRange)
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

    private IEnumerator AttackSequence()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // 1. ���� ���� (���� ����)
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(telegraphDuration);
        spriteRenderer.color = originalColor;

        // 2. ���� �ִϸ��̼� ����
        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.35f); // �ִϸ��̼��� ���۵ǰ� �ణ�� �ð� ���
        PerformSwordAttack();

        // ����: ���� ���� ����(����� �ֱ�)�� �ִϸ��̼��� Ư�� �����ӿ�
        // Animation Event�� �߰��Ͽ� PerformSwordAttack() �Լ��� ȣ���ϴ� ���� ���� ��Ȯ�մϴ�.
        // ���⼭�� �ִϸ��̼��� ���̸� �����Ͽ� ��� �� isAttacking �� false �� �ٲߴϴ�.
        // ���� ��� ���� �ִϸ��̼��� 1�ʶ�� �Ʒ� �ð��� 1�ʷ� �����մϴ�.
        yield return new WaitForSeconds(1f);

        isAttacking = false;
    }

    // Animation Event �� ȣ��� �Լ�
    public void PerformSwordAttack()
    {
        // �÷��̾ ������ Į �ֵθ��� ���� ���� �ִ��� Ȯ�� �� ����� ó��
        if (Vector2.Distance(transform.position, player.position) <= attackRange + 0.5f) // �ణ�� �߰� ����
        {
            Vector2 knockbackDir = (player.position.x < transform.position.x) ? Vector2.left : Vector2.right;

            // PlayerManager ��ũ��Ʈ�� �ִٰ� �����մϴ�. ���� ����ϴ� ��ũ��Ʈ �̸����� �ٲ��ּ���.
            PlayerManager.Instance.TakeDamage(attackDamage, knockbackDir);
            Debug.Log($"�÷��̾�� + {attackDamage} + ������� �������ϴ�!");
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        isHurt = true; // ���� �ǰ� ���·� ����
        anim.SetTrigger("isHurt");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        anim.SetTrigger("Die");
        rigid.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false; // �ٸ� ������Ʈ�� �浹���� �ʵ���

        // ��� �ִϸ��̼� �ð���ŭ ��ٸ� �� ������Ʈ �ı�
        Destroy(gameObject, 3f); // 3�ʴ� ����, ���� ��� �ִϸ��̼� ���̿� �����ּ���.
    }

    void CheckForPlayerInSight()
    {
        Vector2 direction = transform.localScale.x > 0 ? Vector2.left : Vector2.right;

        Vector2 raycastOrigin = (Vector2)transform.position + new Vector2(0, 1f); // ���� �þ߰� ����

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, direction, visionRange, playerLayer);

        Debug.DrawRay(raycastOrigin, direction * visionRange, Color.red);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            hasSpottedPlayer = true;
            Debug.Log("�÷��̾� �߰�!");
        }
    }
}