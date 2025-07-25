using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public PlayerController player;

    [Header("�÷��̾� �⺻ ����")]
    public int HP; // �⺻ ü��
    public int MaxHP = 3; // �ִ� ü��
    public float Damage = 1.0f; // ���ݷ�

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("�ߺ��� PlayerManager�� �����Ͽ� �ı��˴ϴ�.");
            Destroy(gameObject); // �ߺ��� �ν��Ͻ� ����
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
        Debug.Log($"�÷��̾� �ǰ�! ���� HP: {HP}");

        if (HP <= 0)
        {
            Die();
        }
        else
        {
            // �˹� ó���� �̰����� ����
            if (player != null)
            {
                Vector2 knockDir = (player.transform.position.x < transform.position.x) ? Vector2.left : Vector2.right;

                player.StartKnockback(knockDir);
            }
        }
    }

    void Die()
    {
        Debug.Log("�÷��̾� ���!");
        // ��� ����
    }
}
