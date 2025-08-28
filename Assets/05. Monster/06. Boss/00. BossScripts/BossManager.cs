using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Hierarchy;

public class BossManager : MonoBehaviour
{
    #region Fields & Properties

    // --- Singleton Pattern ---
    public static BossManager Instance { get; private set; }

    // --- Components & References ---
    private BossController boss;

    // ü�� ���� �� ȣ��� �̺�Ʈ
    public static event System.Action<int, int> OnHealthChanged;
    public static event System.Action<float, float> OnGroggyChanged;

    [Header("ü�� ����")]
    [SerializeField] private int hp;
    public int maxHp = 30;

    [Header("�׷α�/���� ����")]
    public float maxGroggy = 10;       // ������ �ʿ��� �� �׷α� ��ġ
    public float stunDuration = 5f;      // ���� ���� �ð�
    [SerializeField] private float currentGroggy = 0;    // ���� �׷α� ��ġ

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

    public float GROGGY
    {
        get => currentGroggy;
        private set
        {
            currentGroggy = Mathf.Clamp(currentGroggy, 0, maxGroggy);
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
        currentGroggy = maxGroggy;

        // UI�� �ʱ� ü�� ���� �����ϱ� ���� �̺�Ʈ�� ȣ���մϴ�.
        OnHealthChanged?.Invoke(hp, maxHp);
        OnGroggyChanged?.Invoke(currentGroggy, maxGroggy);
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

        currentGroggy -= amount;
        OnGroggyChanged?.Invoke(currentGroggy, maxGroggy);

        if (currentGroggy <= 0)
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
        currentGroggy = maxGroggy;
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