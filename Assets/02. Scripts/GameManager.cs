using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject pauseMenu;

    private bool isGameClear = false;
    private bool isPause = false;
    private bool isGameOver = false;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
    }

    void PauseMenu()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            isPause = true;
            pauseMenu.SetActive(true);
            Time.timeScale = 0f;
        }

        if (isPause)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isPause = false;
                pauseMenu.SetActive(false);
                Time.timeScale = 1f;
            }
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

    #region Buttons
    public void ResumeButton()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPause = false;
    }

    public void GuideButton()
    {

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
