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

    // 체력 변경 시 호출될 이벤트
    public static event System.Action<int, int> OnHealthChanged;

    [Header("체력 설정")]
    [SerializeField] private int hp;
    public int maxHp = 30;

    [Header("그로기/기절 설정")]
    public float maxGroggy = 10f;       // 기절에 필요한 총 그로기 수치
    public float stunDuration = 5f;      // 기절 지속 시간
    private float currentGroggy = 0f;    // 현재 그로기 수치

    [Header("보스 사망 시, 열릴 문")]
    public List<DoorScript> doorToOpen = new List<DoorScript>();

    [Header("보스 사망 연출")]
    [Tooltip("보스가 죽었을 때 실행할 컷신 트리거를 연결하세요.")]
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

        // UI의 초기 체력 값을 설정하기 위해 이벤트를 호출합니다.
        OnHealthChanged?.Invoke(hp, maxHp);
    }

    #endregion

    #region Public Methods

    // 일반 데미지를 받는 함수
    public void TakeDamage(int damage)
    {
        if (hp <= 0) return; // 이미 죽은 상태면 리턴

        // 기절 상태일 때 데미지 3배
        if (boss.CurrentState == BossController.BossState.Stunned)
        {
            damage *= 3;
        }

        hp -= damage;

        // 체력이 변경되었음을 UI에 알립니다.
        OnHealthChanged?.Invoke(hp, maxHp);

        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }

    // 그로기 수치를 받는 함수
    public void TakeGroggyDamage(float amount)
    {
        if (hp <= 0) return; // 이미 죽은 상태면 리턴

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

        // 보스 사망 컷신 실행
        if (bossDeathCutsceneTrigger != null)
        {
            StartCoroutine(bossDeathCutsceneTrigger.PlayCutscene());
        }

        // 문 열기
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

        // 스턴 지속시간이 끝난 후, 보스의 상태가 Stunned 상태일 경우에만 Idle로 되돌립니다.
        // (다른 상태에 의해 변경된 경우 덮어쓰지 않기 위함)
        if (boss.CurrentState == BossController.BossState.Stunned)
        {
            Debug.Log("Boss stun has ended.");
            boss.SetState(BossController.BossState.Idle);
        }
    }

    #endregion
}