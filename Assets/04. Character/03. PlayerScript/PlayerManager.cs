using UnityEngine;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    private PlayerController player; // �÷��̾� ��Ʈ�ѷ�
    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    [Header("�÷��̾� ����")]
    public int HP;
    public int MaxHP = 3;
    public float Damage = 1.0f;

    private void Start()
    {
        player = GetComponent<PlayerController>();
        // ���� ���� �ʱ�ȭ
        HP = MaxHP;
    }

    public void TakeDamage(int attackDamage, Vector2 attackDir)
    {
        if (HP <= 0) return; // �̹� �׾����� �ƹ� �͵� ���� ����.
        HP -= attackDamage;

        if (player.IsHurt) return; // �̹� ���� ���¸� �ߺ����� ó������ ����.
        player.StartKnockback(attackDir); // �÷��̾ �˹� ��Ŵ.
    }
}
