using UnityEngine;
using System.Collections;
using System;

public class PlayerManager : MonoBehaviour
{
    private PlayerController player; // �÷��̾� ��Ʈ�ѷ�
    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)Instance = this;
        else Destroy(gameObject);
    }

    [Header("�÷��̾� ����")]
    [SerializeField] private int hp;
    public int MaxHP = 3;
    public float Damage = 1.0f;

    // HP ���� ���� �� ����Ǵ� �̺�Ʈ
    public event Action<int> OnHPChanged;

    public int HP
    {
        get => hp;
        private set
        {
            hp = Mathf.Clamp(value, 0, MaxHP);
            OnHPChanged?.Invoke(hp); // �̺�Ʈ �߻�
        }
    }

    private void Start()
    {
        player = GetComponent<PlayerController>();
        // ���� ���� �ʱ�ȭ
        HP = MaxHP;
    }

    public void TakeDamage(int attackDamage, Vector2 attackDir)
    {
        Debug.Log("�ǰ� Ȯ��!");
        if (HP <= 0) return; // �̹� �׾����� �ƹ� �͵� ���� ����.
        HP -= attackDamage;

        //if (player.isHurt) return; // �̹� ���� ���¸� �ߺ����� ó������ ����.
        player.StartKnockback(attackDir); // �÷��̾ �˹� ��Ŵ.
    }

    public void Heal(int amount)
    {
        HP += amount;
    }
}
