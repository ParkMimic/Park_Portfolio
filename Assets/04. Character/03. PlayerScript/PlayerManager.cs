using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public PlayerController player;

    [Header("플레이어 기본 정보")]
    public int HP; // 기본 체력
    public int MaxHP = 3; // 최대 체력
    public float Damage = 1.0f; // 공격력

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("중복된 PlayerManager가 존재하여 파괴됩니다.");
            Destroy(gameObject); // 중복된 인스턴스 제거
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        HP = MaxHP;
    }

    public void TakeDamage(int attackDamage)
    {
        HP -= attackDamage;
        Debug.Log($"플레이어 피격! 현재 HP: {HP}");

        if (HP <= 0)
        {
            Die();
        }
        else
        {
            // 넉백 처리도 이곳에서 가능
            if (player != null)
            {
                Vector2 knockDir = (player.transform.position.x < transform.position.x) ? Vector2.left : Vector2.right;

                player.StartKnockback(knockDir);
            }
        }
    }

    void Die()
    {
        Debug.Log("플레이어 사망!");
        // 사망 로직
    }
}
