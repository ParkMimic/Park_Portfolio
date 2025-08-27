using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject guide;
    public GameObject[] tutorialSteps;
    private GameObject lastSelected;

    private bool isGameClear = false;
    public bool isPause = false;
    private bool isGameOver = false;
    private bool isGuide = false;

    public static GameManager Instance { get; private set; }

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

    void Update()
    {
        ButtonInput();
        GameClearBackToTitle();
        PauseMenu();
        UIManagement();
    }

    void PauseMenu()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver && !isGuide)
        {
            if (isPause)
            {
                // 일시정지 해제
                isPause = false;
                pauseMenu.SetActive(false);
                Time.timeScale = 1f;
            }
            else
            {
                isPause = true;
                pauseMenu.SetActive(true);
                Time.timeScale = 0f;
            }
        }
        else if (isGuide && Input.GetKeyDown(KeyCode.Escape))
        {
            isGuide = false;
            guide.SetActive(false);
        }
    }

    void ButtonInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GameRestart();
        }
    }

    public void GameOver()
    {
        isGameOver = true;
        Invoke("GameRestart", 3f);
    }

    public void GameClear()
    {
        isGameClear = true;
    }

    void GameRestart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void GameClearBackToTitle()
    {
        if (!isGameClear || isGameOver) return;
        if (Input.anyKeyDown)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("00. Title");
            Time.timeScale = 1f;
        }
    }

    void UIManagement()
    {
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            // 선택이 풀렸다면 마지막 선택을 다시 세팅
            if (lastSelected != null)
            {
                EventSystem.current.SetSelectedGameObject(lastSelected);
            }
        }
        else
        {
            // 현재 선택된 오브젝트 기억해두기
            lastSelected = EventSystem.current.currentSelectedGameObject;
        }
    }

    #region PauseMenu Buttons
    public void ResumeButton()
    {
        Debug.Log("버튼 입력 확인");
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
        isPause = false;
    }

    public void GuideButton()
    {
        guide.SetActive(true);
        foreach (var step in tutorialSteps)
        {
            if (step == tutorialSteps[0]) step.SetActive(true);
            else step.SetActive(false);
        }
        isGuide = true;
    }

    public void TitleButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("00. Title");
        Time.timeScale = 1f;
        isPause = false;
    }

    public void ExitButton()
    {
        Application.Quit();
    }
    #endregion
}
