using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class KeepUISelected : MonoBehaviour
{
    private GameObject lastSelected;

    void Update()
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

    public void GoMain()
    {
        SceneManager.LoadScene("01. Main");
    }
    
    public void GameExit()
    {
        Application.Quit();
    }
}
