using UnityEngine;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    private PlayerController player; // 플레이어 컨트롤러
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

    [Header("플레이어 스텟")]
    public int HP;
    public int MaxHP = 3;
    public float Damage = 1.0f;

    private void Start()
    {
        player = GetComponent<PlayerController>();
        // 시작 스텟 초기화
        HP = MaxHP;
    }

    public void TakeDamage(int attackDamage, Vector2 attackDir)
    {
        if (HP <= 0) return; // 이미 죽었으면 아무 것도 하지 않음.
        HP -= attackDamage;

        if (player.IsHurt) return; // 이미 아픈 상태면 중복으로 처리하지 않음.
        player.StartKnockback(attackDir); // 플레이어를 넉백 시킴.
    }
}
