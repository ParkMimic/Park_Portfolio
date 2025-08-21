using UnityEngine;
using System.Collections;
using System;

public class PlayerManager : MonoBehaviour
{
    private PlayerController player; // 플레이어 컨트롤러
    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)Instance = this;
        else Destroy(gameObject);
    }

    [Header("플레이어 스텟")]
    [SerializeField] private int hp;
    public int MaxHP = 3;
    public float Damage = 1.0f;

    // HP 값이 변할 때 실행되는 이벤트
    public event Action<int> OnHPChanged;

    public int HP
    {
        get => hp;
        private set
        {
            hp = Mathf.Clamp(value, 0, MaxHP);
            OnHPChanged?.Invoke(hp); // 이벤트 발생
        }
    }

    private void Start()
    {
        player = GetComponent<PlayerController>();
        // 시작 스텟 초기화
        HP = MaxHP;
    }

    public void TakeDamage(int attackDamage, Vector2 attackDir)
    {
        Debug.Log("피격 확인!");
        if (HP <= 0) return; // 이미 죽었으면 아무 것도 하지 않음.
        HP -= attackDamage;

        //if (player.isHurt) return; // 이미 아픈 상태면 중복으로 처리하지 않음.
        player.StartKnockback(attackDir); // 플레이어를 넉백 시킴.
    }

    public void Heal(int amount)
    {
        HP += amount;
    }
}
