using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 3f;
    public float jumpForce = 300f;
    public float dashSpeed = 10f;
    public float defaultTime = 0.5f;
    public float dashCooldown = 1f;
    public float wallJumpSpeed = 1f;

    public GameObject ghostPrefab;
    public float ghostDelay = 1f;

    private float dashTime;
    private float gravity;
    private float ghostDelayTime;
    private float dashCooldownTimer = 0f;
    private float wallJumpCooldown = 0.1f;
    private float wallJumpCooldownTimer = 0f;

    private int jumpCount = 0;

    private bool isGrounded = false;
    private bool isWalking = false;
    private bool isFalling = false;
    private bool isDead = false;
    private bool isDash = false;
    private bool isJumping = false;
    private bool isGrap = false;
    private bool isClear = false;

    private bool isLeftPressed = false;
    private bool isRightPressed = false;
    private bool isDashPressed = false;
    private bool jumpRequest = false;

    private Vector2 lastMoveDirection = Vector2.right;
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private BoxCollider2D body;

    public float h;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<BoxCollider2D>();

        ghostDelayTime = 0;
        gravity = rigid.gravityScale;
    }

    private void FixedUpdate()
    {
        isFalling = !isGrounded && rigid.linearVelocity.y < 0;
    }

    void Update()
    {
        if (isLeftPressed) h = -1;
        else if (isRightPressed) h = 1;
        else h = 0;

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) h = -1;
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) h = 1;

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequest = true;
        }

        if (wallJumpCooldownTimer > 0f) wallJumpCooldownTimer -= Time.deltaTime;

        if (!isDash && !isGrap && !isDead && !isClear)
        {
            if (h != 0)
            {
                rigid.linearVelocity = new Vector2(h * speed, rigid.linearVelocity.y);
                isWalking = true;
                lastMoveDirection = new Vector2(h, rigid.linearVelocity.y).normalized;
                spriteRenderer.flipX = h < 0;
            }
            else
            {
                isWalking = false;
                rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            }
        }

        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown("x"))
        {
            isDashPressed = true;
        }

        if (isDashPressed && dashTime <= 0 && dashCooldownTimer <= 0 && !isGrap)
        {
            isDash = true;
            dashTime = defaultTime;
            Vector2 dashDirection = (h != 0) ? new Vector2(h, 0f).normalized : lastMoveDirection;
            rigid.gravityScale = 0;
            rigid.linearVelocity = new Vector2(dashDirection.x * dashSpeed, 0f);
            dashCooldownTimer = dashCooldown;
        }
        isDashPressed = false;

        if (isDash)
        {
            dashTime -= Time.deltaTime;
            MakeGhost();

            if (dashTime <= 0)
            {
                isDash = false;
                rigid.gravityScale = gravity;
                rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            }
        }

        if (jumpRequest && jumpCount < 2 && !isDead && !isGrap)
        {
            jumpCount++;
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0f);
            rigid.AddForce(new Vector2(0, jumpForce));
            jumpRequest = false;
        }

        if (Input.GetKeyUp("z") && rigid.linearVelocityY > 0)
        {
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, rigid.linearVelocity.y * 0.5f);
        }

        if (jumpCount == 2) isJumping = true;

        if (isGrap)
        {
            rigid.gravityScale = 0f;
            rigid.linearVelocity = Vector2.zero;
            jumpCount = 1;

            if (jumpRequest)
            {
                isGrap = false;
                isGrounded = false;
                float jumpDirection = spriteRenderer.flipX ? 1f : -1f;
                rigid.gravityScale = gravity;
                rigid.linearVelocity = new Vector2(jumpDirection * wallJumpSpeed, jumpForce * 0.02f);
                wallJumpCooldownTimer = wallJumpCooldown;
                jumpRequest = false;
            }
        }

        if (isDead)
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);

        if (isClear)
        {
            rigid.linearVelocity = new Vector2(-1 * speed, rigid.linearVelocity.y);
            if (transform.position.x <= -35.5f && isGrounded)
                rigid.AddForce(new Vector2(0, jumpForce));
        }

        //anim.SetBool("isGround", isGrounded);
        //anim.SetBool("isWalk", isWalking);
        //anim.SetBool("isFall", isFalling);
        //anim.SetBool("isDash", isDash);
        //anim.SetBool("isJump", isJumping);
        //anim.SetBool("isWall", isGrap);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contacts[0].normal.y > 0f)
        {
            isGrounded = true;
            isJumping = false;
            jumpCount = 0;
        }

        if (collision.otherCollider is BoxCollider2D)
        {
            Vector2 normal = collision.contacts[0].normal;
            if (Mathf.Abs(normal.x) > 0.5f && Mathf.Abs(normal.y) < 0.5f && !isGrounded && !isDead && wallJumpCooldownTimer <= 0.1f)
            {
                rigid.linearVelocity = lastMoveDirection;
                if (isGrap != true)
                {
                    isGrap = true;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (jumpCount == 1)
            isGrounded = false;

        isGrounded = false;
    }

    //void Die()
    //{
    //    isDash = false;
    //    isGrap = false;
    //    isDead = true;
    //    rigid.gravityScale = gravity;
    //    rigid.linearVelocity = Vector2.zero;
    //    //GameManager.instance.OnPlayerDead();
    //    anim.SetTrigger("isDead");
    //}

    void MakeGhost()
    {
        if (!isDash) return;
        ghostDelayTime -= Time.deltaTime;

        if (ghostDelayTime <= 0)
        {
            GameObject currentGhost = Instantiate(ghostPrefab, transform.position, transform.rotation);
            SpriteRenderer ghostSprite = currentGhost.GetComponent<SpriteRenderer>();
            ghostSprite.sprite = spriteRenderer.sprite;
            ghostSprite.flipX = spriteRenderer.flipX;
            currentGhost.transform.localScale = transform.localScale;
            Destroy(currentGhost, 0.3f);
            ghostDelayTime = 0.05f;
        }
    }

    public void LeftDown() => isLeftPressed = true;
    public void LeftUp() => isLeftPressed = false;
    public void RightDown() => isRightPressed = true;
    public void RightUp() => isRightPressed = false;
    public void JumpDown()
    {
        jumpRequest = true;
    }
    public void JumpUp()
    {

        rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, rigid.linearVelocity.y * 0.5f);
    }
    public void DashClick() => isDashPressed = true;
}