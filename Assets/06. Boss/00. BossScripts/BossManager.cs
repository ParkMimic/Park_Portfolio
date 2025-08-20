using UnityEngine;
using System.Collections;

public class BossManager : MonoBehaviour
{
    #region Fields & Properties

    // --- Singleton Pattern ---
    public static BossManager Instance { get; private set; }

    // --- Components & References ---
    private BossController boss;

    [Header("ü�� ����")]
    public int hp;
    public int maxHp = 30;

    [Header("�׷α�/���� ����")]
    public float maxGroggy = 10f;       // ������ �ʿ��� �� �׷α� ��ġ
    public float stunDuration = 5f;      // ���� ���� �ð�
    private float currentGroggy = 0f;    // ���� �׷α� ��ġ

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        boss = GetComponent<BossController>();
        hp = maxHp;
        currentGroggy = 0;
    }

    #endregion

    #region Public Methods

    // �Ϲ� ���ظ� �޴� �Լ�
    public void TakeDamage(int damage)
    {
        if (hp <= 0) return; // �̹� ���� ���¸� ����

        // ���� ������ ��� ������ 3��
        if (boss.CurrentState == BossController.BossState.Stunned)
        {
            damage *= 3;
        }

        hp -= damage;

        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }

    // �׷α� ��ġ�� �״� �Լ�
    public void TakeGroggyDamage(float amount)
    {
        if (hp <= 0) return; // �̹� ���� ���¸� ����

        currentGroggy += amount;
        if (currentGroggy >= maxGroggy)
        {
            StartCoroutine(StunSequence());
        }
    }

    #endregion

    #region Internal Logic & Coroutines

    private void Die()
    {
        Debug.Log("Boss has died.");
        boss.SetState(BossController.BossState.Dead);
        // ���⿡ �߰����� ���� ó�� ������ ���� �� �ֽ��ϴ�.
        // (��: ���� ü�� UI ��Ȱ��ȭ, �浹 ��Ȱ��ȭ ��)
    }

    private IEnumerator StunSequence()
    {
        Debug.Log("Boss is stunned!");
        currentGroggy = 0;
        boss.SetState(BossController.BossState.Stunned);

        yield return new WaitForSeconds(stunDuration);

        // ���� ���ӽð��� ������ ��, ������ ������ Stunned ������ ��쿡�� Idle�� �ǵ����ϴ�.
        // (���� �߿� �״� ��� ���� ����)
        if (boss.CurrentState == BossController.BossState.Stunned)
        {
            Debug.Log("Boss stun has ended.");
            boss.SetState(BossController.BossState.Idle);
        }
    }

    #endregion
}