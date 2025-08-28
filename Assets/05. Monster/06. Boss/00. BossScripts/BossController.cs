using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region Data Structures

// 공격 종류 Enum
public enum AttackType
{
    Melee1,
    Melee2,
    Melee3,
    Dash
}

// 단일 공격 행동 정의 클래스
[System.Serializable]
public class AttackAction
{
    public AttackType attackType;
    public float midPauseDuration = 0.3f; // 애니메이션 중간에 멈출 시간 (선딜레이)
    public float postDelay = 0.2f;        // 행동이 모두 끝난 후의 딜레이
}

// 연속 공격 패턴 정의 클래스
[System.Serializable]
public class AttackPattern
{
    public string patternName;
    public AttackAction[] actions;

    [Header("패턴 사용 조건")]
    public float minDistance = 0f;    // 이 패턴을 사용하기 위한 최소 거리
    public float maxDistance = 5f;    // 이 패턴을 사용하기 위한 최대 거리
}

#endregion

public class BossController : MonoBehaviour
{
    #region Enums & Public Fields

    // Stunned와 Dead 상태 추가
    public enum BossState { Falling, Idle, Attacking, Stunned, Dead }

    [Header("보스 기본 설정")]
    public Transform spawnPoint;
    public Transform targetPoint;
    public float dropSpeed = 20f;
    public float knockbackRadius = 5f;
    public float knockbackForce = 25f;
    public LayerMask playerLayer;

    [Header("AI 설정")]
    public Transform playerTransform;
    public float moveSpeed = 3f;

    [Header("공격 상세 설정")]
    public float dashAttackSpeed = 25f;
    public float dashAttackDuration = 0.3f;

    [Header("공격 판정 설정")]
    public GameObject meleeHitbox;

    [Header("공격 패턴 설정")]
    public AttackPattern[] attackPatterns;

    #endregion

    #region Private & Public Properties

    public BossState CurrentState { get; private set; }

    private Rigidbody2D rigid;
    private Animator anim;
    private BoxCollider2D meleeCollider;

    private bool isAttackFinished = false;
    private AttackAction currentAction; // 현재 실행 중인 액션을 기억할 변수

    #endregion

    #region Unity Lifecycle Methods

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        meleeCollider = meleeHitbox.GetComponent<BoxCollider2D>();
        meleeCollider.enabled = false;

        transform.position = spawnPoint.position;
        SetState(BossState.Falling);
        StartCoroutine(DropSequence());
    }

    void Update()
    {
        // Stunned, Dead 상태일 때 행동하지 않도록 가드 조건 추가
        if (CurrentState == BossState.Falling || CurrentState == BossState.Attacking || CurrentState == BossState.Stunned || CurrentState == BossState.Dead || playerTransform == null) return;

        if (CurrentState == BossState.Idle)
        {
            DecideNextAction();
        }
    }

    #endregion

    #region State Control

    // 외부에서 보스의 상태를 제어하기 위한 public 함수
    public void SetState(BossState newState)
    {
        CurrentState = newState;

        // 상태에 따라 특정 로직을 처리
        if (CurrentState == BossState.Stunned || CurrentState == BossState.Dead)
        {
            DisableMeleeHitbox(); // 만약 공격 중에 상태가 바뀌면 hitbox를 비활성화시킵니다.
            anim.SetBool("isWalking", false);
            rigid.linearVelocity = Vector2.zero;
        }

        if (CurrentState == BossState.Stunned)
        {
            anim.SetTrigger("Stunned"); // "Stunned" 트리거가 애니메이터에 있다고 가정
        }

        if (CurrentState == BossState.Dead)
        {
            anim.SetTrigger("Dead"); // "Dead" 트리거가 애니메이터에 있다고 가정
        }
    }

    #endregion

    #region Core AI Logic

    void DecideNextAction()
    {
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        List<AttackPattern> availablePatterns = new List<AttackPattern>();

        foreach (var pattern in attackPatterns)
        {
            if (distance >= pattern.minDistance && distance <= pattern.maxDistance)
            {
                availablePatterns.Add(pattern);
            }
        }

        if (availablePatterns.Count > 0)
        {
            int randomIndex = Random.Range(0, availablePatterns.Count);
            StartCoroutine(ExecutePattern(availablePatterns[randomIndex]));
        }
        else
        {
            MoveTowardsPlayer();
        }
    }

    void MoveTowardsPlayer()
    {
        float direction = Mathf.Sign(playerTransform.position.x - transform.position.x);
        transform.localScale = new Vector3(-direction, 1, 1);
        rigid.linearVelocity = new Vector2(direction * moveSpeed, rigid.linearVelocity.y);
        anim.SetBool("isWalking", true);
    }

    IEnumerator ExecutePattern(AttackPattern pattern)
    {
        SetState(BossState.Attacking);
        rigid.linearVelocity = Vector2.zero;
        anim.SetBool("isWalking", false);

        // 패턴을 시작하기 직전에, 플레이어의 방향을 바라보도록 설정합니다.
        float direction = Mathf.Sign(playerTransform.position.x - transform.position.x);
        transform.localScale = new Vector3(-direction, 1, 1);

        Debug.Log($"패턴 시작: {pattern.patternName}");

        foreach (var action in pattern.actions)
        {
            // 행동 실행 전, 보스의 상태가 공격 상태가 아니면(예: 기절) 패턴을 중단합니다.
            if (CurrentState != BossState.Attacking)
            {
                yield break;
            }

            this.currentAction = action;
            isAttackFinished = false;

            yield return StartCoroutine(PerformAction(action));

            if (action.attackType != AttackType.Dash)
            {
                // isAttackFinished가 true가 되거나, 보스 상태가 Attacking이 아니게 될 때까지 기다립니다.
                yield return new WaitUntil(() => isAttackFinished || CurrentState != BossState.Attacking);
            }

            if (action.postDelay > 0)
            {
                yield return new WaitForSeconds(action.postDelay);
            }
        }

        this.currentAction = null;
        Debug.Log($"패턴 종료: {pattern.patternName}");

        // 패턴이 정상적으로 끝났을 때만 상태를 Idle로 변경합니다.
        // (기절했거나 죽었을 경우 상태를 덮어쓰지 않기 위함)
        if (CurrentState == BossState.Attacking)
        {
            SetState(BossState.Idle);
        }
    }

    IEnumerator PerformAction(AttackAction action)
    {
        if (action.attackType == AttackType.Dash)
        {
            Debug.Log("액션: 돌진");
            anim.SetTrigger("Dash");
            float horizontalDirection = Mathf.Sign(playerTransform.position.x - transform.position.x);
            Vector2 direction = new Vector2(horizontalDirection, 0);
            transform.localScale = new Vector3(-direction.x, 1, 1);
            rigid.linearVelocity = direction * dashAttackSpeed;
            yield return new WaitForSeconds(dashAttackDuration);
            rigid.linearVelocity = Vector2.zero;
        }
        else
        {
            string triggerName = action.attackType.ToString();
            Debug.Log($"액션: {triggerName}");
            anim.SetTrigger(triggerName);
        }
        yield return null;
    }

    #endregion

    #region Animation Event Handlers

    public void EnableMeleeHitbox()
    {
        meleeCollider.enabled = true;
    }

    public void DisableMeleeHitbox()
    {
        meleeCollider.enabled = false;
    }

    public void OnAttackFinished()
    {
        isAttackFinished = true;
    }

    public void TriggerMidAttackPause()
    {
        if (currentAction != null && currentAction.midPauseDuration > 0)
        {
            StartCoroutine(PauseAndResume(currentAction.midPauseDuration));
        }
    }

    private IEnumerator PauseAndResume(float duration)
    {
        anim.speed = 0;
        yield return new WaitForSeconds(duration);
        anim.speed = 1;
    }

    #endregion

    #region Special Sequences

    IEnumerator DropSequence()
    {
        while (Vector3.Distance(transform.position, targetPoint.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, dropSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPoint.position;
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, knockbackRadius, playerLayer);
        if (playerCollider != null)
        {
            // Rigidbody2D 대신 PlayerController 컴포넌트를 가져옵니다.
            PlayerController playerController = playerCollider.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // 넉백 방향을 계산합니다.
                float horizontalDirection = Mathf.Sign(playerController.transform.position.x - transform.position.x);
                Vector2 knockbackDirection = new Vector2(horizontalDirection, 0);

                // 플레이어의 넉백 함수를 호출하되, 보스의 넉백 힘(knockbackForce)을 함께 전달합니다.
                playerController.StartKnockback(knockbackDirection, knockbackForce);
            }
        }
        yield return new WaitForSeconds(2f);
        SetState(BossState.Idle);
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, knockbackRadius);
    }

    #endregion
}