using UnityEngine;
using UnityEngine.UI;

public class PlayerHPUI : MonoBehaviour
{
    [SerializeField] private GameObject[] playerHP;
    private Color[] originalColors;

    void Start()
    {
        originalColors = new Color[playerHP.Length];

        // ���� �� ���� ���� ����
        for (int i = 0; i < playerHP.Length; i++)
        {
            Image hpImage = playerHP[i].GetComponent<Image>();
            if (hpImage != null)
            {
                originalColors[i] = hpImage.color;
            }
        }

        PlayerManager.Instance.OnHPChanged += UpdateHPUI;

        UpdateHPUI(PlayerManager.Instance.HP); // �ʱ� UI ����
    }

    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnHPChanged -= UpdateHPUI;
        }
    }

    public void UpdateHPUI(int currentHP)
    {
        for (int i = 0; i < playerHP.Length; i++)
        {
            Image hpImage = playerHP[i].GetComponent<Image>();
            if (hpImage != null)
            {
                if (i < currentHP)
                {
                    hpImage.color = originalColors[i]; // ���� �� ����
                }
                else
                {
                    hpImage.color = Color.white; // ���� HP�� �Ͼ��
                }
            }
        }
    }
}
