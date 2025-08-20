using UnityEngine;
using System.Collections;

public class BossManager : MonoBehaviour
{
    #region Fields & Properties

    // --- Singleton Pattern ---
    public static BossManager Instance { get; private set; }

    // --- Components & References ---
    private BossController boss;

    [Header("체력 설정")]
    public int hp;
    public int maxHp = 30;

    [Header("그로기/기절 설정")]
    public float maxGroggy = 10f;       // 기절에 필요한 총 그로기 수치
    public float stunDuration = 5f;      // 기절 지속 시간
    private float currentGroggy = 0f;    // 현재 그로기 수치

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

    // 일반 피해를 받는 함수
    public void TakeDamage(int damage)
    {
        if (hp <= 0) return; // 이미 죽은 상태면 무시

        // 스턴 상태일 경우 데미지 3배
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

    // 그로기 수치를 쌓는 함수
    public void TakeGroggyDamage(float amount)
    {
        if (hp <= 0) return; // 이미 죽은 상태면 무시

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
        // 여기에 추가적인 죽음 처리 로직을 넣을 수 있습니다.
        // (예: 보스 체력 UI 비활성화, 충돌 비활성화 등)
    }

    private IEnumerator StunSequence()
    {
        Debug.Log("Boss is stunned!");
        currentGroggy = 0;
        boss.SetState(BossController.BossState.Stunned);

        yield return new WaitForSeconds(stunDuration);

        // 기절 지속시간이 끝났을 때, 보스가 여전히 Stunned 상태일 경우에만 Idle로 되돌립니다.
        // (기절 중에 죽는 경우 등을 방지)
        if (boss.CurrentState == BossController.BossState.Stunned)
        {
            Debug.Log("Boss stun has ended.");
            boss.SetState(BossController.BossState.Idle);
        }
    }

    #endregion
}