using NUnit.Framework.Constraints;
using System.Collections;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [Header("�⺻ ����")]
    public float moveSpeed = 2f;
    public float maxHealth = 3;
    [SerializeField] private float currentHealth;

    [Header("���� ����")]
    public int contactDamage = 1; // ���� �� ������
    public int attackDamage = 1; // Į ���� ������
    public float attackRange = 3f; // �� �Ÿ� �ȿ� ������ ���� ����
    public float attackCooldown = 2f; // �� �� ���� �� ���� ���ݱ����� �ּ� �ð�
    public float pauseBeforeAttackTime = 0.2f; // ���� �ִϸ��̼� ���� �� ���� �������� �ð�
    public float attackDelay = 0.4f; // ���� ���� ���ߴ� �ð�

    [Header("�и� ����")]
    public float parryFlashDuration = 0.1f; // �и� ���� Ÿ�ֿ̹� ��¦�̴� �ð�

    [Header("�ǰ� ����")]
    public float hurtDuration = 0.5f; // �ǰ� �� ���ߴ� �ð�
    public float hurtForce = 5f; // �ǰ� �� �з����� ��

    [Header("�÷��̾� ����")]
    public Transform player; // �÷��̾� Transform
    public LayerMask playerLayer; // �÷��̾� ���̾�

    [Header("�þ� ����")]
    public float visionRange = 10f; // �÷��̾ ������ �� �ִ� �ִ� �Ÿ�
    public float loseSightDistance = 15f; // �÷��̾ �� �Ÿ� �̻� �־����� �þ߸� ����
    private bool hasSpottedPlayer = false; // �÷��̾ �߰��ߴ��� ����

    // ���� ����
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Color originalColor;

    private bool isAttacking = false;
    private bool isDead = false;
    private float lastAttackTime;
    private bool isHurt = false; // ���Ͱ� ���� �������� Ȯ��

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        lastAttackTime = 0; // ������ ���� �ð��� �ʱ�ȭ
        currentHealth = maxHealth; // ���� �� ���� ü���� �ִ� ü������ �ʱ�ȭ
        originalColor = spriteRenderer.color; // �⺻ �� ����

        // player ���� �Ҵ� �� �÷��̾ �ڵ����� ã�� (�±װ� "Player"�� �����Ǿ� �־�� ��)
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

            // ���� �Ÿ��� '�þ߸� �Ҵ� �Ÿ�'���� �־����ٸ�
            if (distanceToPlayer > loseSightDistance)
            {
                hasSpottedPlayer = false; // �÷��̾ �Ҿ��ٰ� �Ǵ�.
                return; // ���� �� �������� ������ �� �̻� �������� ����.
            }

            // ���� ��Ÿ���� ������, �÷��̾ ���� ���� �ȿ� ������ ���� ���� �ƴ϶��
            if (Time.time >= lastAttackTime + attackCooldown && distanceToPlayer <= attackRange && !isAttacking)
            {
                // AttackSequence �ڷ�ƾ�� ���� ȣ���ϴ� ���, �ִϸ��̼� Ʈ���Ÿ� ����մϴ�.
                isAttacking = true;
                lastAttackTime = Time.time;
                StartCoroutine(FullAttackSequence());
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
                    transform.localScale = new Vector3(-1, 1, 1); // �÷��̾ �����ʿ� ������ ����
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // �׾��ų�, �´� ���̰ų�, �÷��̾ ���� ���� �ȿ� ������ �������� ����
        if (isDead || isHurt || isAttacking || !hasSpottedPlayer || Vector2.Distance(transform.position, player.position) <= attackRange)
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

    // Animation Event�� ȣ��� �Լ�: �и� Ÿ�̹� �ð� ȿ��
    public void TriggerParryFlash()
    {
        StartCoroutine(ParryFlashEffect());
    }

    private IEnumerator ParryFlashEffect()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(parryFlashDuration);
        spriteRenderer.color = originalColor;
    }

    private IEnumerator FullAttackSequence()
    {
        anim.SetTrigger("Attack");
        yield return new WaitForSeconds(pauseBeforeAttackTime);

        try
        {
            anim.speed = 0; // �ִϸ��̼� ����
            yield return new WaitForSeconds(attackDelay);
        }
        finally
        {
            anim.speed = 1; // � ��Ȳ������ �ִϸ��̼� �ӵ� ����
        }
    }

    public void FinishAttack()
    {
        isAttacking = false; // ���� �Ϸ�
    }

    // Animation Event �� ȣ��� �Լ�
    public void PerformSwordAttack()
    {
        // ���Ͱ� �ǰ� �����϶� ������ ������� �ʵ��� �մϴ�.
        if (isHurt) return;

        // �÷��̾ ������ Į �ֵθ��� ���� ���� �ִ��� Ȯ�� �� ������ ó��
        if (Vector2.Distance(transform.position, player.position) <= attackRange + 0.5f) // �ణ�� �߰� ����
        {
            Vector2 knockbackDir = (player.position.x < transform.position.x) ? Vector2.left : Vector2.right;

            // PlayerManager ��ũ��Ʈ�� �ִٰ� �����մϴ�. �ٸ� �̸��� ��ũ��Ʈ��� �ٲ��ּ���.
            PlayerManager.Instance.TakeDamage(attackDamage, knockbackDir);
            Debug.Log($"�÷��̾�� + {attackDamage} + �������� �������ϴ�!");
        }
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection)
    {
        if (isDead) return; // �̹� ���� ���¶�� �������� ���� �ʵ��� �մϴ�.

        currentHealth -= damage; // �ǰ� �ִϸ��̼ǰ� ������� ü���� ���� ���ҽ�ŵ�ϴ�.

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // ���Ͱ� �̹� �ǰ� ���°� �ƴ� ���� �ǰ� �������� �����մϴ�.
            // �̷��� �ϸ� ���� ���ݿ� �������� �´���, �˹�/�ִϸ��̼��� ��ġ�� �ʽ��ϴ�.
            if (!isHurt)
            {
                StartCoroutine(HurtSequence(knockbackDirection));
            }
        }
    }

    private IEnumerator HurtSequence(Vector2 knockbackDirection)
    {
        // 1. ���� ���� �� �ൿ ����
        isHurt = true;
        StopAllCoroutines(); // �߿�! �ٸ� ��� �ڷ�ƾ, Ư�� AttackSequence ���� �ڷ�ƾ�� ��� �ߴ�.
        isAttacking = false; // ���� ����
        anim.ResetTrigger("Attack"); // Ȥ�� �� ���� �ִϸ��̼� Ʈ���Ÿ� �����Ͽ� ������ �ߴܽ�ŵ�ϴ�.

        // 2. �ð��� �ǵ��: ���� ����
        spriteRenderer.color = Color.red;

        // 3. �˹� ����
        // rigid.linearVelocity = Vector2.zero; // �� ������ �ּ�ó���Ͽ� ���� �ӵ��� �˹��� ���������� �մϴ�.
        rigid.AddForce(knockbackDirection * hurtForce, ForceMode2D.Impulse); // �˹� ����.

        // 4. �ִϸ��̼� ���
        anim.SetTrigger("isHurt");

        // 5. �ǰ� ���� �ð���ŭ ���
        yield return new WaitForSeconds(hurtDuration);

        // 6. ���� �ʱ�ȭ
        isHurt = false;
        spriteRenderer.color = originalColor; // ������ ������� ����
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines(); // �״� ���� ��� �ڷ�ƾ�� �ߴ��Ͽ� �ٸ� �ൿ�� �����ϴ�.
        anim.SetTrigger("isDead");

        // ���������� ������ ���� �ʵ��� Kinematic���� ����
        rigid.linearVelocity = Vector2.zero;
        rigid.angularVelocity = 0f; // ȸ���� ����ϴ�.
        rigid.bodyType = RigidbodyType2D.Kinematic;

        // �ݶ��̴� ��Ȱ��ȭ�� ������Ʈ �ı��� �ִϸ��̼� �̺�Ʈ�� ó���մϴ�.
    }

    // Animation Event�� ȣ��� �Լ�: ������ �ݶ��̴��� ��Ȱ��ȭ�մϴ�.
    public void DisableMonsterCollider()
    {
        Collider2D monsterCollider = GetComponent<Collider2D>();
        if (monsterCollider != null)
        {
            monsterCollider.enabled = false;
        }
    }

    // Animation Event�� ȣ��� �Լ�: ���� ������Ʈ�� �ı��մϴ�.
    public void DestroyMonster()
    {
        Destroy(gameObject);
    }

    void CheckForPlayerInSight()
    {
        Vector2 direction = transform.localScale.x > 0 ? Vector2.left : Vector2.right;

        Vector2 raycastOrigin = (Vector2)transform.position + new Vector2(0, 1f); // �ణ �þ߰� ����

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, direction, visionRange, playerLayer);

        Debug.DrawRay(raycastOrigin, direction * visionRange, Color.red);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            hasSpottedPlayer = true;
            Debug.Log("�÷��̾� �߰�!");
        }
    }
}