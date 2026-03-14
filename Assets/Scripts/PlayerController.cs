using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool canControl = true;

    public enum ControlType
    {
        WASD,
        ArrowKeys,
        IJKL
    }

    [Header("Control")]
    public ControlType controlType;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float airControl = 5f;
    public float groundAcceleration = 90f;
    public float groundDeceleration = 110f;
    public float airAcceleration = 55f;
    public float airDeceleration = 35f;
    public float maxFallSpeed = 20f;

    [Header("Jump")]
    public float jumpForce = 12f;
    public float fallMultiplier = 3f;
    public float lowJumpMultiplier = 2f;
    public float jumpBufferTime = 0.12f;
    public bool holdJumpToBounce = true;

    [Header("Wall Jump")]
    public float wallJumpForceX = 10f;
    public float wallJumpForceY = 12f;

    [Header("Wall Slide")]
    public float wallSlideSpeed = 2f;

    [Header("Coyote Time")]
    public float coyoteTime = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.2f;

    [Header("Wall Check")]
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public float wallCheckDistance = 0.2f;

    [Header("Animation")]
    public Sprite idleSprite;
    public Sprite runSpriteA;
    public Sprite runSpriteB;
    public float runAnimationSpeed = 10f;

    public LayerMask groundLayer;

    Rigidbody2D rb;
    SpriteRenderer sr;

    float horizontal;
    bool jumpHeld;
    bool holdJumpQueued;

    bool isGrounded;
    bool touchingWallLeft;
    bool touchingWallRight;

    float coyoteTimer;
    float jumpBufferTimer;
    float runAnimationTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (idleSprite == null && sr != null)
        {
            idleSprite = sr.sprite;
        }
    }

    void Update()
    {
        if (!canControl)
        {
            ClearInput();
            return;
        }

        DetectGround();
        DetectWalls();
        HandleInput();
        HandleCoyoteTime();
        HandleJump();
        HandleFlip();
        HandleAnimation();
    }

    void FixedUpdate()
    {
        if (!canControl)
        {
            if (rb.velocity != Vector2.zero)
            {
                rb.velocity = Vector2.zero;
            }

            return;
        }

        HandleMovement();
        HandleWallSlide();
        BetterJump();
    }

    void ClearInput()
    {
        horizontal = 0f;
        jumpHeld = false;
        holdJumpQueued = false;
        jumpBufferTimer = 0f;
    }

    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;
        ClearInput();

        if (!enabled)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void ResetForNextRound(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        holdJumpQueued = false;
        runAnimationTimer = 0f;
        DetectGround();
        DetectWalls();
        SetSprite(idleSprite);
        SetControlEnabled(true);
    }

    void HandleInput()
    {
        horizontal = 0;
        bool jumpPressed = false;

        switch (controlType)
        {
            case ControlType.WASD:
                if (Input.GetKey(KeyCode.A)) horizontal = -1;
                if (Input.GetKey(KeyCode.D)) horizontal = 1;
                jumpHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space);
                jumpPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);
                break;

            case ControlType.ArrowKeys:
                if (Input.GetKey(KeyCode.LeftArrow)) horizontal = -1;
                if (Input.GetKey(KeyCode.RightArrow)) horizontal = 1;
                jumpHeld = Input.GetKey(KeyCode.UpArrow);
                jumpPressed = Input.GetKeyDown(KeyCode.UpArrow);
                break;

            case ControlType.IJKL:
                if (Input.GetKey(KeyCode.J)) horizontal = -1;
                if (Input.GetKey(KeyCode.L)) horizontal = 1;
                jumpHeld = Input.GetKey(KeyCode.I);
                jumpPressed = Input.GetKeyDown(KeyCode.I);
                break;
        }

        if (jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else if (holdJumpToBounce && jumpHeld && isGrounded && !holdJumpQueued)
        {
            jumpBufferTimer = jumpBufferTime;
            holdJumpQueued = true;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (!jumpHeld || !isGrounded)
        {
            holdJumpQueued = false;
        }
    }

    void DetectGround()
    {
        isGrounded = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );
    }

    void DetectWalls()
    {
        touchingWallLeft = Physics2D.Raycast(
            wallCheckLeft.position,
            Vector2.left,
            wallCheckDistance,
            groundLayer
        );

        touchingWallRight = Physics2D.Raycast(
            wallCheckRight.position,
            Vector2.right,
            wallCheckDistance,
            groundLayer
        );
    }

    void HandleCoyoteTime()
    {
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    void HandleJump()
    {
        if (jumpBufferTimer <= 0f)
        {
            return;
        }

        if (coyoteTimer > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            isGrounded = false;
            return;
        }

        if (touchingWallLeft)
        {
            rb.velocity = new Vector2(wallJumpForceX, wallJumpForceY);
            jumpBufferTimer = 0f;
            return;
        }

        if (touchingWallRight)
        {
            rb.velocity = new Vector2(-wallJumpForceX, wallJumpForceY);
            jumpBufferTimer = 0f;
        }
    }

    void HandleMovement()
    {
        float targetSpeed = horizontal * moveSpeed;
        float acceleration;

        if (Mathf.Abs(targetSpeed) > 0.01f)
        {
            acceleration = isGrounded ? groundAcceleration : airAcceleration;
        }
        else
        {
            acceleration = isGrounded ? groundDeceleration : airDeceleration;
        }

        float newX = Mathf.MoveTowards(
            rb.velocity.x,
            targetSpeed,
            acceleration * Time.fixedDeltaTime
        );

        rb.velocity = new Vector2(newX, rb.velocity.y);
    }

    void HandleWallSlide()
    {
        if (!isGrounded && (touchingWallLeft || touchingWallRight) && rb.velocity.y < 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
        }
    }

    void BetterJump()
    {
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y *
                           (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && !jumpHeld)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y *
                           (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        if (rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
        }
    }

    void HandleFlip()
    {
        if (horizontal > 0.1f)
        {
            sr.flipX = true;
        }
        else if (horizontal < -0.1f)
        {
            sr.flipX = false;
        }
    }

    void HandleAnimation()
    {
        if (sr == null)
        {
            return;
        }

        if (idleSprite == null)
        {
            idleSprite = sr.sprite;
        }

        bool hasRunAnimation = runSpriteA != null && runSpriteB != null;
        bool shouldRunAnimate =
            hasRunAnimation &&
            canControl &&
            isGrounded &&
            Mathf.Abs(rb.velocity.x) > 0.1f &&
            Mathf.Abs(horizontal) > 0.1f;

        if (!shouldRunAnimate)
        {
            runAnimationTimer = 0f;
            SetSprite(idleSprite);
            return;
        }

        runAnimationTimer += Time.deltaTime * runAnimationSpeed;
        bool useFirstFrame = Mathf.FloorToInt(runAnimationTimer) % 2 == 0;
        SetSprite(useFirstFrame ? runSpriteA : runSpriteB);
    }

    void SetSprite(Sprite sprite)
    {
        if (sr == null || sprite == null || sr.sprite == sprite)
        {
            return;
        }

        sr.sprite = sprite;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                groundCheck.position,
                groundCheck.position + Vector3.down * groundCheckDistance
            );
        }

        if (wallCheckLeft != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                wallCheckLeft.position,
                wallCheckLeft.position + Vector3.left * wallCheckDistance
            );
        }

        if (wallCheckRight != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                wallCheckRight.position,
                wallCheckRight.position + Vector3.right * wallCheckDistance
            );
        }
    }
}
