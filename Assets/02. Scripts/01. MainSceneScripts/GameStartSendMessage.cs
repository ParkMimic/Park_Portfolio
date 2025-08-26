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
                Debug.Log("자식 애니메이터가 연결되지 않았습니다.");
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
                Debug.Log("자식 애니메이터가 연결되지 않았습니다.");
            }
        }
    }

}
