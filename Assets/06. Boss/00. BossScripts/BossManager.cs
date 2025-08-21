using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

public class BossManager : MonoBehaviour
{
    #region Fields & Properties

    // --- Singleton Pattern ---
    public static BossManager Instance { get; private set; }

    // --- Components & References ---
    private BossController boss;

    // ü�� ���� �� ȣ��� �̺�Ʈ
    public static event System.Action<int, int> OnHealthChanged;

    [Header("ü�� ����")]
    [SerializeField] private int hp;
    public int maxHp = 30;

    [Header("�׷α�/���� ����")]
    public float maxGroggy = 10f;       // ������ �ʿ��� �� �׷α� ��ġ
    public float stunDuration = 5f;      // ���� ���� �ð�
    private float currentGroggy = 0f;    // ���� �׷α� ��ġ

    [Header("���� ��� ��, ���� ��")]
    public List<DoorScript> doorToOpen = new List<DoorScript>();

    [Header("���� ��� ����")]
    [Tooltip("������ �׾��� �� ������ �ƽ� Ʈ���Ÿ� �����ϼ���.")]
    public GenericCutsceneTrigger bossDeathCutsceneTrigger;

    #endregion

    #region Unity Lifecycle

    public int HP
    {
        get => hp;
        private set
        {
            hp = Mathf.Clamp(hp, 0, maxHp);
        }
    }

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

        // UI�� �ʱ� ü�� ���� �����ϱ� ���� �̺�Ʈ�� ȣ���մϴ�.
        OnHealthChanged?.Invoke(hp, maxHp);
    }

    #endregion

    #region Public Methods

    // �Ϲ� �������� �޴� �Լ�
    public void TakeDamage(int damage)
    {
        if (hp <= 0) return; // �̹� ���� ���¸� ����

        // ���� ������ �� ������ 3��
        if (boss.CurrentState == BossController.BossState.Stunned)
        {
            damage *= 3;
        }

        hp -= damage;

        // ü���� ����Ǿ����� UI�� �˸��ϴ�.
        OnHealthChanged?.Invoke(hp, maxHp);

        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }

    // �׷α� ��ġ�� �޴� �Լ�
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

        // ���� ��� �ƽ� ����
        if (bossDeathCutsceneTrigger != null)
        {
            StartCoroutine(bossDeathCutsceneTrigger.PlayCutscene());
        }

        // �� ����
        if (doorToOpen != null && doorToOpen.Count > 0)
        {
            foreach (var door in doorToOpen)
            {
                if (door != null)
                {
                    door.StartOpening();
                }
            }
        }
    }

    private IEnumerator StunSequence()
    {
        Debug.Log("Boss is stunned!");
        currentGroggy = 0;
        boss.SetState(BossController.BossState.Stunned);

        yield return new WaitForSeconds(stunDuration);

        // ���� ���ӽð��� ���� ��, ������ ���°� Stunned ������ ��쿡�� Idle�� �ǵ����ϴ�.
        // (�ٸ� ���¿� ���� ����� ��� ����� �ʱ� ����)
        if (boss.CurrentState == BossController.BossState.Stunned)
        {
            Debug.Log("Boss stun has ended.");
            boss.SetState(BossController.BossState.Idle);
        }
    }

    #endregion
}