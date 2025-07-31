using System.Collections;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    #region ���� ���� (Fields & Properties)

    [Header("�⺻ ����")]
    public float moveSpeed = 2f;
    public float maxHealth = 3;
    [SerializeField] private float currentHealth;

    [Header("���� ����")]
    public int contactDamage = 1; // ���� �� ������
    public int attackDamage = 1; // Į ���� ������
    public float attackRange = 3f; // �� �Ÿ� �ȿ� ������ ���� ����
    public float attackCooldown = 2f; // �� �� ���� �� ���� ���ݱ����� �ּ� �ð�
    public float pauseBeforeAttackTime = 0.2f; // ���� �ִϸ��̼� ���� �� ���� ���������� �ð�
    public float attackDelay = 0.4f; // ���� ���� ���ӽð�
    [SerializeField] private GameObject attackHitboxObject; // ���� ��Ʈ�ڽ�

    [Header("�и� ����")]
    public float parryFlashDuration = 0.1f; // �и� ���� Ÿ�ֿ̹� ��½�̴� �ð�

    [Header("�ǰ� ����")]
    public float hurtDuration = 0.5f; // �ǰ� �� �����ð�
    public float hurtForce = 5f; // �ǰ� �� �з����� ��

    [Header("�׷α�/���� ����")]
    public float maxGroggy = 1f; // �ִ� �׷α� ��ġ
    private float currentGroggy; // ���� �׷α� ��ġ
    public float stunDuration = 2f; // ���� ���� �ð�
    private bool isStunned = false; // ���� ���� ����

    [Header("�÷��̾� ����")]
    public Transform player; // �÷��̾� Transform
    public LayerMask playerLayer; // �÷��̾� ���̾�

    [Header("�þ� ����")]
    public float visionRange = 10f; // �÷��̾ �߰��� �� �ִ� �ִ� �Ÿ�
    public float loseSightDistance = 15f; // �÷��̾ �� �Ÿ� �̻� ����� �þ߸� ����
    private bool hasSpottedPlayer = false; // �÷��̾ �߰��ߴ��� ����

    // ������Ʈ ����
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Color originalColor;

    // ����(State) ����
    private bool isAttacking = false;
    private bool isDead = false;
    private bool isHurt = false;

    // ���� ���� ����
    private float lastAttackTime;

    #endregion

    #region �⺻ �Լ� (Unity Lifecycle)

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        currentHealth = maxHealth;
        currentGroggy = 0;
        lastAttackTime = -attackCooldown; // �������ڸ��� ������ �� �ֵ���
        originalColor = spriteRenderer.color;

        // ��Ʈ�ڽ� ��Ȱ��ȭ�� ����
        if (attackHitboxObject != null) attackHitboxObject.SetActive(false);

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
                enabled = false;
            }
        }
    }

    private void Update()
    {
        if (isDead || isStunned || player == null || PlayerManager.Instance.HP <= 0) return;

        HandlePlayerDetection();
        HandleAttacking();
        HandleSpriteFlip();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    #endregion

    #region ���� (Logic)

    private void HandlePlayerDetection()
    {
        if (!hasSpottedPlayer)
        {
            CheckForPlayerInSight();
        }
        else
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer > loseSightDistance)
            {
                hasSpottedPlayer = false;
            }
        }
    }

    private void HandleMovement()
    {
        if (isDead || isHurt || isAttacking || isStunned || !hasSpottedPlayer || Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            anim.SetBool("isWalking", false);
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rigid.linearVelocity = new Vector2(direction.x * moveSpeed, rigid.linearVelocity.y);
        anim.SetBool("isWalking", true);
    }

    private void HandleSpriteFlip()
    {
        if (!isAttacking && hasSpottedPlayer)
        {
            if (player.position.x < transform.position.x)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }

    private void CheckForPlayerInSight()
    {
        Vector2 direction = transform.localScale.x > 0 ? Vector2.left : Vector2.right;
        Vector2 raycastOrigin = (Vector2)transform.position + new Vector2(0, 1f);
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, direction, visionRange, playerLayer);

        Debug.DrawRay(raycastOrigin, direction * visionRange, Color.red);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            hasSpottedPlayer = true;
            Debug.Log("�÷��̾� �߰�!");
        }
    }

    #endregion

    #region ���� (Attack)

    private void HandleAttacking()
    {
        if (!hasSpottedPlayer || isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (Time.time >= lastAttackTime + attackCooldown && distanceToPlayer <= attackRange)
        {
            lastAttackTime = Time.time;
            StartCoroutine(AttackSequence());
        }
    }

    // Animation Event�� ȣ��� �Լ�: �и� Ÿ�̹� �ð� ȿ��
    public void TriggerParryFlash()
    {
        StartCoroutine(ParryFlashEffect());
    }

    #endregion

    #region �ǰ� �� ü�� (Damage & Health)

    public void TakeDamage(float damage, Vector2 knockbackDirection)
    {
        if (isDead) return;

        float finalDamage = isStunned ? damage * 3 : damage;
        currentHealth -= finalDamage;

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (!isStunned)
        {
            StartCoroutine(HurtSequence(knockbackDirection));
        }
    }

    public void TakeGroggyDamage(float amount)
    {
        if (isDead || isStunned) return;

        currentGroggy += amount;
        if (currentGroggy >= maxGroggy)
        {
            StartCoroutine(StunSequence());
        }
    }

    private void Die()
    {
        isDead = true;
        anim.SetTrigger("isDead");
        rigid.linearVelocity = Vector2.zero;
        rigid.angularVelocity = 0f;
        rigid.bodyType = RigidbodyType2D.Kinematic;
    }

    #endregion

    #region �ڷ�ƾ (Coroutines)

    private IEnumerator AttackSequence()
    {
        // ���� ������ �����Ǿ����� Ȯ���ϱ� ���� �α�
        Debug.Log("���� ���� ����! ���� �������� �����մϴ�.");

        isAttacking = true;
        anim.SetTrigger("Attack");

        // ���� �ڷ�ƾ�� �ִϸ��̼��� �����Ű�⸸ �ϰ� �ٷ� ����˴ϴ�.
        // ���� ���� ������ �ִϸ��̼� �̺�Ʈ�� ó���մϴ�.
        yield return null;
    }

    private IEnumerator StunSequence()
    {
        isStunned = true;
        currentGroggy = 0;
        isAttacking = false;
        anim.ResetTrigger("Attack");
        rigid.linearVelocity = Vector2.zero;

        // anim.SetTrigger("Stunned");
        spriteRenderer.color = Color.yellow;

        yield return new WaitForSeconds(stunDuration);

        isStunned = false;
        spriteRenderer.color = originalColor;
    }

    private IEnumerator HurtSequence(Vector2 knockbackDirection)
    {
        isHurt = true;
        isAttacking = false;
        anim.ResetTrigger("Attack");
        spriteRenderer.color = Color.white;
        rigid.AddForce(knockbackDirection * hurtForce, ForceMode2D.Impulse);

        anim.SetTrigger("isHurt");
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;

        yield return new WaitForSeconds(hurtDuration);

        isHurt = false;
    }

    private IEnumerator ParryFlashEffect()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(parryFlashDuration);
        spriteRenderer.color = originalColor;
    }

    #endregion

    #region �ִϸ��̼� �̺�Ʈ (Animation Events)

    // ���� �ִϸ��̼��� Ư�� �����ӿ��� ȣ��
    public void ActivateAttackHitbox()
    {
        if (attackHitboxObject != null) attackHitboxObject.SetActive(true);
    }

    // ���� �ִϸ��̼��� �ٸ� �����ӿ��� ȣ��
    public void DeactivateAttackHitbox()
    {
        if (attackHitboxObject != null) attackHitboxObject.SetActive(false);
    }

    // Animation Event�� ȣ��� �Լ�
    public void FinishAttack()
    {
        Debug.Log("FinishAttack �̺�Ʈ ȣ���! isAttacking ���¸� false�� �����մϴ�.");
        isAttacking = false;
    }

    // �״� �ִϸ��̼��� ���� �� ȣ��
    public void DisableMonsterCollider()
    {
        Collider2D monsterCollider = GetComponent<Collider2D>();
        if (monsterCollider != null)
        {
            monsterCollider.enabled = false;
        }
    }

    // DisableMonsterCollider ���� ȣ��
    public void DestroyMonster()
    {
        Destroy(gameObject);
    }

    #endregion
}
