using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class GameStartSendMessage : MonoBehaviour
{

    [SerializeField] private Animator anim;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            anim.SetTrigger("FadeIn");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            anim.SetTrigger("FadeOut");
    }

}
