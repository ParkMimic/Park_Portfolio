using UnityEngine;
using UnityEngine.UI;

public class PlayerHPUI : MonoBehaviour
{
    [SerializeField] private GameObject[] playerHP;
    private Color[] originalColors;

    void Start()
    {
        originalColors = new Color[playerHP.Length];

        // 시작 시 원래 색을 저장
        for (int i = 0; i < playerHP.Length; i++)
        {
            Image hpImage = playerHP[i].GetComponent<Image>();
            if (hpImage != null)
            {
                originalColors[i] = hpImage.color;
            }
        }

        PlayerManager.Instance.OnHPChanged += UpdateHPUI;

        UpdateHPUI(PlayerManager.Instance.HP); // 초기 UI 갱신
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
                    hpImage.color = originalColors[i]; // 원래 색 복원
                }
                else
                {
                    hpImage.color = Color.white; // 잃은 HP는 하얀색
                }
            }
        }
    }
}
