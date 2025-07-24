using UnityEditor.SceneManagement;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("플레이어 스테이터스")]
    public int MaxHP; // 플레이어의 최대 체력
    public int HP; // 플레이어의 초기 체력
    public float Damage; // 플레이어의 공격력

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

    void Start()
    {
        MaxHP = 3; // 최대 체력 3
        HP = 3; // 현재 체력 3
        Damage = 1.0f; // 공격력 1
    }

    public void TakeDamage(int attackDamage)
    {
        HP -= attackDamage;
    }
}
