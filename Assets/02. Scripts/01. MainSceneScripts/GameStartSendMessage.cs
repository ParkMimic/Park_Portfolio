using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class GameStartSendMessage : MonoBehaviour
{
    public Animator childTextAnimator;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (childTextAnimator != null)
            {
                childTextAnimator.SetTrigger("FadeIn");
            }
            else
            {
                Debug.Log("�ڽ� �ִϸ����Ͱ� ������� �ʾҽ��ϴ�.");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (childTextAnimator != null)
            {
                childTextAnimator.SetTrigger("FadeOut");
            }
            else
            {
                Debug.Log("�ڽ� �ִϸ����Ͱ� ������� �ʾҽ��ϴ�.");
            }
        }
    }

}
