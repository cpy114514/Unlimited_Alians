using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool canControl = true;

    public enum ControlType
    {
        WASD,
        ArrowKeys,
        IJKL,
        Slot4,
        Slot5,
        Slot6
    }

    [Header("Control")]
    public ControlType controlType;
    public GameInput.BindingId inputBinding = GameInput.BindingId.KeyboardWasd;
    public int playerPrefabIndex;

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

    [Header("Step Climb")]
    public float stepUpHeight = 0.52f;
    public float stepCheckDistance = 0.12f;
    public float stepForwardNudge = 0.08f;

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

    static readonly Vector3 defaultTagMarkerOffset = new Vector3(0f, 1.1f, 0f);
    static readonly Vector3 defaultTagMarkerScale = new Vector3(0.7f, 0.7f, 1f);
    static readonly Color defaultTagMarkerColor = Color.white;
    static readonly Color defaultProtectedTagMarkerColor = new Color(1f, 0.85f, 0.55f, 1f);

    Rigidbody2D rb;
    SpriteRenderer sr;
    BoxCollider2D bodyCollider;
    SpriteRenderer tagMarkerRenderer;
    readonly Collider2D[] tagOverlapBuffer = new Collider2D[8];

    float horizontal;
    bool jumpHeld;
    bool holdJumpQueued;
    bool buildPhaseFrozen;
    bool externalMotionOnly;

    bool isGrounded;
    bool touchingWallLeft;
    bool touchingWallRight;
    Trampoline groundedTrampoline;
    Trampoline contactedTrampoline;
    float trampolineContactExpiryTime;

    float coyoteTimer;
    float jumpBufferTimer;
    float runAnimationTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<BoxCollider2D>();

        if (idleSprite == null && sr != null)
        {
            idleSprite = sr.sprite;
        }

        EnsureTagMarker();
        SetTagState(
            false,
            false,
            false,
            null,
            defaultTagMarkerOffset,
            defaultTagMarkerScale,
            defaultTagMarkerColor,
            defaultProtectedTagMarkerColor
        );
    }

    void Update()
    {
        if (buildPhaseFrozen)
        {
            ClearInput();
            return;
        }

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
        HandleTrampolineAutoBounce();
        HandleFlip();
        HandleAnimation();
    }

    void FixedUpdate()
    {
        if (buildPhaseFrozen)
        {
            return;
        }

        if (!canControl)
        {
            if (!externalMotionOnly && rb.velocity != Vector2.zero)
            {
                rb.velocity = Vector2.zero;
            }

            return;
        }

        HandleMovement();
        TryStepUp();
        HandleWallSlide();
        BetterJump();
        CheckTagOverlap();
    }

    void ClearInput()
    {
        horizontal = 0f;
        jumpHeld = false;
        holdJumpQueued = false;
        jumpBufferTimer = 0f;
        groundedTrampoline = null;
        contactedTrampoline = null;
        trampolineContactExpiryTime = 0f;
    }

    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;
        externalMotionOnly = false;
        ClearInput();

        if (!enabled)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void SetBuildPhaseFrozen(bool frozen)
    {
        buildPhaseFrozen = frozen;
        externalMotionOnly = false;
        ClearInput();

        if (rb == null)
        {
            return;
        }

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = !frozen;

        if (!frozen)
        {
            DetectGround();
            DetectWalls();
            SetSprite(idleSprite);
        }
    }

    public void ResetForNextRound(Vector3 spawnPosition)
    {
        SetBuildPhaseFrozen(false);
        externalMotionOnly = false;
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

    public void SetExternalMotionOnly(bool active)
    {
        externalMotionOnly = active;
        canControl = !active;
        ClearInput();
    }

    public void ApplyAvatarAnimation(Sprite idle, Sprite runA, Sprite runB)
    {
        if (idle != null)
        {
            idleSprite = idle;
            SetSprite(idleSprite);
        }

        runSpriteA = runA;
        runSpriteB = runB;
    }

    public void SetTagState(
        bool isIt,
        bool isProtected,
        bool tagModeActive,
        Sprite markerSprite,
        Vector3 markerOffset,
        Vector3 markerScale,
        Color markerColor,
        Color protectedMarkerColor
    )
    {
        EnsureTagMarker();

        if (tagMarkerRenderer == null)
        {
            return;
        }

        bool showLabel = tagModeActive && isIt;
        tagMarkerRenderer.gameObject.SetActive(showLabel);

        if (!showLabel)
        {
            return;
        }

        tagMarkerRenderer.sprite = markerSprite;
        tagMarkerRenderer.transform.localPosition = markerOffset;
        tagMarkerRenderer.transform.localScale = markerScale;
        tagMarkerRenderer.color = isProtected
            ? protectedMarkerColor
            : markerColor;
    }

    public void MoveToWaitingArea(Vector3 position)
    {
        transform.position = position;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        isGrounded = false;
        DetectWalls();
    }

    public float VerticalVelocity
    {
        get { return rb != null ? rb.velocity.y : 0f; }
    }

    public void Bounce(float upwardForce)
    {
        if (rb == null)
        {
            return;
        }

        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.velocity = new Vector2(rb.velocity.x, upwardForce);
        isGrounded = false;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        holdJumpQueued = false;
        groundedTrampoline = null;
        contactedTrampoline = null;
        trampolineContactExpiryTime = 0f;
    }

    public void Launch(Vector2 velocity)
    {
        if (rb == null)
        {
            return;
        }

        rb.velocity = Vector2.zero;
        rb.velocity = velocity;
        isGrounded = false;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        holdJumpQueued = false;
        groundedTrampoline = null;
        contactedTrampoline = null;
        trampolineContactExpiryTime = 0f;
    }

    public void RegisterTrampolineContact(Trampoline trampoline)
    {
        if (trampoline == null || !canControl || buildPhaseFrozen)
        {
            return;
        }

        contactedTrampoline = trampoline;
        groundedTrampoline = trampoline;
        trampolineContactExpiryTime = Time.time + 0.12f;
    }

    void EnsureTagMarker()
    {
        if (tagMarkerRenderer != null)
        {
            tagMarkerRenderer.transform.localPosition = defaultTagMarkerOffset;
            tagMarkerRenderer.transform.localScale = defaultTagMarkerScale;
            return;
        }

        Transform existing = transform.Find("TagMarker");
        if (existing != null)
        {
            tagMarkerRenderer = existing.GetComponent<SpriteRenderer>();
        }

        if (tagMarkerRenderer == null)
        {
            GameObject markerObject = new GameObject("TagMarker");
            markerObject.transform.SetParent(transform, false);
            tagMarkerRenderer = markerObject.AddComponent<SpriteRenderer>();
        }

        tagMarkerRenderer.sortingOrder = sr != null ? sr.sortingOrder + 3 : 12;
        tagMarkerRenderer.color = defaultTagMarkerColor;
        tagMarkerRenderer.transform.localPosition = defaultTagMarkerOffset;
        tagMarkerRenderer.transform.localScale = defaultTagMarkerScale;
        tagMarkerRenderer.gameObject.SetActive(false);
    }

    void HandleInput()
    {
        horizontal = GameInput.GetHorizontal(inputBinding);
        jumpHeld = GameInput.GetJumpHeld(inputBinding);
        bool jumpPressed = GameInput.GetJumpPressed(inputBinding);

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
        RaycastHit2D groundHit = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        isGrounded = groundHit.collider != null;
        groundedTrampoline = isGrounded
            ? groundHit.collider.GetComponentInParent<Trampoline>()
            : null;
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

        if (TryGetActiveTrampoline(out Trampoline activeTrampoline) &&
            activeTrampoline.TryTriggerGroundJumpBoost(this, jumpForce))
        {
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            isGrounded = false;
            groundedTrampoline = null;
            contactedTrampoline = null;
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

    void HandleTrampolineAutoBounce()
    {
        if (!TryGetActiveTrampoline(out Trampoline activeTrampoline))
        {
            return;
        }

        if (jumpBufferTimer > 0f)
        {
            return;
        }

        if (VerticalVelocity > 0.05f)
        {
            return;
        }

        activeTrampoline.TryTriggerAutoBounce(this);
    }

    bool TryGetActiveTrampoline(out Trampoline trampoline)
    {
        trampoline = null;

        if (groundedTrampoline != null)
        {
            contactedTrampoline = groundedTrampoline;
            trampolineContactExpiryTime = Time.time + 0.12f;
        }

        if (contactedTrampoline == null || Time.time > trampolineContactExpiryTime)
        {
            groundedTrampoline = null;
            contactedTrampoline = null;
            return false;
        }

        trampoline = contactedTrampoline;
        return true;
    }

    void HandleMovement()
    {
        float speedMultiplier = 1f;
        if (RoundManager.Instance != null)
        {
            speedMultiplier = RoundManager.Instance.GetPlayerMoveSpeedMultiplier(controlType);
        }

        float targetSpeed = horizontal * moveSpeed * speedMultiplier;
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
            acceleration * Mathf.Max(1f, speedMultiplier) * Time.fixedDeltaTime
        );

        rb.velocity = new Vector2(newX, rb.velocity.y);
    }

    void TryStepUp()
    {
        if (rb == null || bodyCollider == null || !isGrounded)
        {
            return;
        }

        if (Mathf.Abs(horizontal) < 0.01f || Mathf.Abs(rb.velocity.x) < 0.05f || rb.velocity.y > 0.2f)
        {
            return;
        }

        float direction = Mathf.Sign(horizontal);
        Bounds bounds = bodyCollider.bounds;
        Vector2 lowerOrigin = new Vector2(
            direction > 0f ? bounds.max.x : bounds.min.x,
            bounds.min.y + 0.06f
        );
        Vector2 upperOrigin = lowerOrigin + Vector2.up * stepUpHeight;

        RaycastHit2D lowerHit = Physics2D.Raycast(
            lowerOrigin,
            Vector2.right * direction,
            stepCheckDistance,
            groundLayer
        );
        if (lowerHit.collider == null)
        {
            return;
        }

        RaycastHit2D upperHit = Physics2D.Raycast(
            upperOrigin,
            Vector2.right * direction,
            stepCheckDistance,
            groundLayer
        );
        if (upperHit.collider != null)
        {
            return;
        }

        Vector2 landingProbe = upperOrigin + Vector2.right * direction * (stepCheckDistance + 0.02f);
        RaycastHit2D landingHit = Physics2D.Raycast(
            landingProbe,
            Vector2.down,
            stepUpHeight + 0.2f,
            groundLayer
        );
        if (landingHit.collider == null)
        {
            return;
        }

        float targetBottom = landingHit.point.y + 0.02f;
        float verticalLift = targetBottom - bounds.min.y;
        if (verticalLift <= 0.02f || verticalLift > stepUpHeight + 0.05f)
        {
            return;
        }

        Vector2 stepOffset = new Vector2(direction * stepForwardNudge, verticalLift);
        Vector2 overlapCenter = (Vector2)bounds.center + stepOffset;
        Vector2 overlapSize = bounds.size - new Vector3(0.06f, 0.04f, 0f);
        Collider2D blockingCollider = Physics2D.OverlapBox(
            overlapCenter,
            overlapSize,
            0f,
            groundLayer
        );

        if (blockingCollider != null && blockingCollider.transform.root != transform)
        {
            return;
        }

        rb.position += stepOffset;
        rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(0f, rb.velocity.y));
        DetectGround();
        DetectWalls();
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        NotifyTagContact(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        NotifyTagContact(collision);
    }

    void NotifyTagContact(Collision2D collision)
    {
        if (collision == null)
        {
            return;
        }

        PlayerController otherPlayer = collision.collider != null
            ? collision.collider.GetComponentInParent<PlayerController>()
            : null;

        if (otherPlayer == null || otherPlayer == this)
        {
            return;
        }

        RoundManager.Instance?.TryTagContact(this, otherPlayer);
    }

    void CheckTagOverlap()
    {
        if (bodyCollider == null || RoundManager.Instance == null || !RoundManager.Instance.IsTagMode)
        {
            return;
        }

        Bounds bounds = bodyCollider.bounds;
        int hitCount = Physics2D.OverlapBoxNonAlloc(
            bounds.center,
            bounds.size,
            0f,
            tagOverlapBuffer
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = tagOverlapBuffer[i];
            if (hit == null)
            {
                continue;
            }

            PlayerController otherPlayer = hit.GetComponentInParent<PlayerController>();
            if (otherPlayer == null || otherPlayer == this)
            {
                continue;
            }

            RoundManager.Instance.TryTagContact(this, otherPlayer);
        }
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
