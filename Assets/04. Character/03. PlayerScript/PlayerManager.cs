using UnityEditor.SceneManagement;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("�÷��̾� �������ͽ�")]
    public int MaxHP; // �÷��̾��� �ִ� ü��
    public int HP; // �÷��̾��� �ʱ� ü��
    public float Damage; // �÷��̾��� ���ݷ�

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
        MaxHP = 3; // �ִ� ü�� 3
        HP = 3; // ���� ü�� 3
        Damage = 1.0f; // ���ݷ� 1
    }

    public void TakeDamage(int attackDamage)
    {
        HP -= attackDamage;
    }
}
