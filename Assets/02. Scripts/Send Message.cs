using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class SendMessage : MonoBehaviour
{
    [SerializeField] private GameObject message;
    public Transform player;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        message.SetActive(false);
    }
    private void Update()
    {
        if (player.position.x < transform.position.x)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            message.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            message.SetActive(false);
        }
    }
}
