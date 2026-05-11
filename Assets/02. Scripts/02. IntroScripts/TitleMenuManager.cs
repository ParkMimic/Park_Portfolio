using UnityEngine;
using UnityEngine.UI;

public class TitleMenuManager : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    [SerializeField] private float selectScale = 1.2f;
    [SerializeField] private float normalScale = 1.0f;

    private int currentIndex = 0;

    void Start()
    {
        UpdateVisual();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1) % buttons.Length;
            UpdateVisual();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + buttons.Length) % buttons.Length;
            UpdateVisual();
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            buttons[currentIndex].onClick.Invoke();
        }
    }

    void UpdateVisual()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            float scale = (i == currentIndex) ? selectScale : normalScale;
            buttons[i].transform.localScale = Vector3.one * scale;
        }
    }
}
