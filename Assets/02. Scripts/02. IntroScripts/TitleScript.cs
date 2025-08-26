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
            // ������ Ǯ�ȴٸ� ������ ������ �ٽ� ����
            if (lastSelected != null)
            {
                EventSystem.current.SetSelectedGameObject(lastSelected);
            }
        }
        else
        {
            // ���� ���õ� ������Ʈ ����صα�
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
