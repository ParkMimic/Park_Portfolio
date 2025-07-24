using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 싱글턴 패턴을 사용하기 위한 인스턴스 변수
    private static GameManager m_instance;
    // 인스턴스에 접근하기 위한 프로퍼티
    public static GameManager Instance
    {
        get
        {
            // 인스턴스가 없는 경우에 접근하려하면 인스턴스를 할당해준다.
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
        // 인스턴스가 존재하는 경우, 새로 생기는 인스턴스를 삭제한다.
        else if (m_instance != this)
        {
            Destroy(gameObject);
        }

        // 씬이 전환 되더라도 선언 되었던 인스턴스가 파괴 되지 않는다.
        DontDestroyOnLoad(gameObject);
    }
}
