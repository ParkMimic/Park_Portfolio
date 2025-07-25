using UnityEngine;

public class GameManager : MonoBehaviour
{
    // �̱��� ������ ����ϱ� ���� �ν��Ͻ� ����
    private static GameManager m_instance;
    // �ν��Ͻ��� �����ϱ� ���� ������Ƽ
    public static GameManager Instance
    {
        get
        {
            // �ν��Ͻ��� ���� ��쿡 �����Ϸ��ϸ� �ν��Ͻ��� �Ҵ����ش�.
            if (!m_instance)
            {
                m_instance = FindAnyObjectByType<GameManager>();

                if (m_instance == null)
                {
                    Debug.Log("no Singleton obj");
                }
            }
            return m_instance;
        }
    }

    void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
        }
        // �ν��Ͻ��� �����ϴ� ���, ���� ����� �ν��Ͻ��� �����Ѵ�.
        else if (m_instance != this)
        {
            Destroy(gameObject);
        }

        // ���� ��ȯ �Ǵ��� ���� �Ǿ��� �ν��Ͻ��� �ı� ���� �ʴ´�.
        DontDestroyOnLoad(gameObject);
    }
}
