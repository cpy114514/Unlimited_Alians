using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BlueBeetleEnemy : MonoBehaviour
{
    enum BeetleState
    {
        Walking,
        ShellIdle,
        ShellMoving
    }

    public Sprite walkFrameA;
    public Sprite walkFrameB;
    public Sprite shellSprite;
    public float moveSpeed = 1.8f;
    public float shellMoveSpeed = 5.5f;
    public float animationSpeed = 6f;
    public float edgeCheckDistance = 0.24f;
    public float wallCheckDistance = 0.08f;
    public float frontProbeInset = 0.06f;
    public float stompBounceForce = 10f;
    public LayerMask groundMask;
    public Vector2 walkColliderSize = new Vector2(0.68f, 0.46f);
    public Vector2 walkColliderOffset = new Vector2(0f, -0.11f);
    public Vector2 shellColliderSize = new Vector2(0.68f, 0.3f);
    public Vector2 shellColliderOffset = new Vector2(0f, -0.19f);
    [Header("Hitboxes")]
    public Vector2 backHitboxSize = new Vector2(0.54f, 0.16f);
    public Vector2 backHitboxOffset = new Vector2(0f, 0.08f);
    public Vector2 bodyHitboxSize = new Vector2(0.76f, 0.42f);
    public Vector2 bodyHitboxOffset = new Vector2(0f, -0.06f);
    public float shellKickNudge = 0.18f;
    public float shellKickIgnoreTime = 0.12f;

    Rigidbody2D rb;
    BoxCollider2D hitbox;
    SpriteRenderer spriteRenderer;

    Vector3 spawnPosition;
    Quaternion spawnRotation;
    Vector3 spawnScale;
    bool movingRight;
    bool wasRaceActive;
    float animationTimer;
    BeetleState state;

    void Awake()
    {
        CacheComponents();
        LoadDefaultSpritesIfNeeded();
        RecordSpawnState();
        ApplySprite(0f);
    }

    void OnValidate()
    {
        CacheComponents();
        LoadDefaultSpritesIfNeeded();
    }

    void Update()
    {
        if (!IsRaceActive())
        {
            return;
        }

        if (state == BeetleState.Walking)
        {
            animationTimer += Time.deltaTime * animationSpeed;
        }

        ApplySprite(animationTimer);
    }

    void FixedUpdate()
    {
        bool raceActive = IsRaceActive();

        if (!raceActive)
        {
            if (wasRaceActive)
            {
                ResetEnemy(false);
            }

            SetPhysicsActive(false);
            wasRaceActive = false;
            return;
        }

        wasRaceActive = true;

        SetPhysicsActive(true);

        if (state == BeetleState.Walking)
        {
            Patrol();
            return;
        }

        if (state == BeetleState.ShellIdle)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        MoveShell();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerCollision(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        HandlePlayerCollision(collision);
    }

    void CacheComponents()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (hitbox == null)
        {
            hitbox = GetComponent<BoxCollider2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (rb != null)
        {
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        ApplyColliderForState();

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 2;
        }
    }

    void RecordSpawnState()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        spawnScale = transform.localScale;
    }

    void Patrol()
    {
        float direction = movingRight ? 1f : -1f;

        if (ShouldTurnAround(direction))
        {
            movingRight = !movingRight;
            direction = movingRight ? 1f : -1f;
        }

        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
        spriteRenderer.flipX = movingRight;
    }

    void MoveShell()
    {
        float direction = movingRight ? 1f : -1f;

        if (CastForGround(
                new Vector2(
                    direction > 0f ? hitbox.bounds.max.x + 0.02f : hitbox.bounds.min.x - 0.02f,
                    hitbox.bounds.center.y),
                Vector2.right * direction,
                wallCheckDistance))
        {
            movingRight = !movingRight;
            direction = movingRight ? 1f : -1f;
        }

        rb.velocity = new Vector2(direction * shellMoveSpeed, rb.velocity.y);
        spriteRenderer.flipX = movingRight;
    }

    bool ShouldTurnAround(float direction)
    {
        Bounds bounds = hitbox.bounds;

        Vector2 wallProbe = new Vector2(
            direction > 0f ? bounds.max.x + 0.02f : bounds.min.x - 0.02f,
            bounds.center.y
        );

        if (CastForGround(wallProbe, Vector2.right * direction, wallCheckDistance))
        {
            return true;
        }

        Vector2 edgeProbe = new Vector2(
            direction > 0f ? bounds.max.x - frontProbeInset : bounds.min.x + frontProbeInset,
            bounds.min.y + 0.04f
        );

        return !CastForGround(edgeProbe, Vector2.down, edgeCheckDistance);
    }

    bool CastForGround(Vector2 origin, Vector2 direction, float distance)
    {
        LayerMask mask = ResolveGroundMask();
        RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, mask);
        return hit.collider != null && hit.collider.transform.root != transform;
    }

    LayerMask ResolveGroundMask()
    {
        if (groundMask.value != 0)
        {
            return groundMask;
        }

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            groundMask = player.groundLayer;
            return groundMask;
        }

        groundMask = Physics2D.AllLayers;
        return groundMask;
    }

    void HandlePlayerCollision(Collision2D collision)
    {
        if (collision == null)
        {
            return;
        }

        PlayerController player = collision.collider.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        if (RoundManager.Instance != null &&
            RoundManager.Instance.IsPlayerResolved(player.controlType))
        {
            return;
        }

        bool stomped = IsStompCollision(player, collision);

        if (stomped)
        {
            HandleStomp(player);
            player.Bounce(stompBounceForce);
            return;
        }

        if (state == BeetleState.ShellIdle)
        {
            KickShell(player.transform.position.x < transform.position.x, collision.collider);
            return;
        }

        if (state == BeetleState.ShellMoving)
        {
            StopShell();
            return;
        }

        if (!IsBodyCollision(collision))
        {
            return;
        }

        RoundManager.Instance?.PlayerDied(player.controlType);
        Destroy(player.gameObject);
    }

    bool IsStompCollision(PlayerController player, Collision2D collision)
    {
        if (player == null || collision == null || hitbox == null)
        {
            return false;
        }

        Bounds playerBounds = collision.collider.bounds;
        Bounds backBounds = GetWorldBounds(backHitboxOffset, backHitboxSize);
        Bounds bodyBounds = GetWorldBounds(bodyHitboxOffset, bodyHitboxSize);
        Bounds feetBounds = new Bounds(
            new Vector3(playerBounds.center.x, playerBounds.min.y + 0.06f, 0f),
            new Vector3(Mathf.Max(0.18f, playerBounds.size.x * 0.68f), 0.12f, 0.1f)
        );
        bool playerDescending = player.VerticalVelocity <= -0.05f;
        bool playerAboveBody = playerBounds.min.y >= bodyBounds.center.y;
        bool feetInsideBack = backBounds.Intersects(feetBounds);
        bool contactInsideBack = false;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (ContainsPoint(backBounds, contact.point) && contact.point.y >= backBounds.min.y)
            {
                contactInsideBack = true;
                break;
            }
        }

        return playerDescending &&
               playerAboveBody &&
               (feetInsideBack || contactInsideBack);
    }

    bool IsBodyCollision(Collision2D collision)
    {
        if (collision == null)
        {
            return false;
        }

        Bounds bodyBounds = GetWorldBounds(bodyHitboxOffset, bodyHitboxSize);
        if (bodyBounds.Intersects(collision.collider.bounds))
        {
            return true;
        }

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (ContainsPoint(bodyBounds, contact.point))
            {
                return true;
            }
        }

        return collision.contactCount > 0;
    }

    void HandleStomp(PlayerController player)
    {
        if (state == BeetleState.Walking)
        {
            EnterShell();
            return;
        }

        if (state == BeetleState.ShellIdle)
        {
            KickShell(
                player != null && player.transform.position.x < transform.position.x,
                player != null ? player.GetComponent<Collider2D>() : null
            );
            return;
        }

        if (state == BeetleState.ShellMoving)
        {
            StopShell();
        }
    }

    void EnterShell()
    {
        state = BeetleState.ShellIdle;
        animationTimer = 0f;

        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        ApplyColliderForState();
        ApplySprite(0f);
    }

    public void HitByHazard()
    {
        if (state != BeetleState.ShellIdle)
        {
            EnterShell();
        }
        else if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    void StopShell()
    {
        state = BeetleState.ShellIdle;

        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        ApplyColliderForState();
    }

    void KickShell(bool kickToRight, Collider2D kickerCollider)
    {
        state = BeetleState.ShellMoving;
        movingRight = kickToRight;
        ApplyColliderForState();

        float direction = movingRight ? 1f : -1f;
        transform.position += new Vector3(direction * shellKickNudge, 0f, 0f);

        if (rb != null)
        {
            rb.position = transform.position;
            rb.velocity = new Vector2(direction * shellMoveSpeed, Mathf.Max(0f, rb.velocity.y));
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = movingRight;
        }

        if (kickerCollider != null && shellKickIgnoreTime > 0f && hitbox != null)
        {
            StartCoroutine(TemporarilyIgnoreCollision(kickerCollider));
        }
    }

    void ApplySprite(float timer)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite targetSprite = walkFrameA;

        if (state != BeetleState.Walking && shellSprite != null)
        {
            targetSprite = shellSprite;
        }
        else if (walkFrameA != null && walkFrameB != null && walkFrameA != walkFrameB)
        {
            targetSprite = Mathf.FloorToInt(timer) % 2 == 0
                ? walkFrameA
                : walkFrameB;
        }

        if (targetSprite != null)
        {
            spriteRenderer.sprite = targetSprite;
        }
    }

    void ApplyColliderForState()
    {
        if (hitbox == null)
        {
            return;
        }

        if (state == BeetleState.Walking)
        {
            hitbox.size = walkColliderSize;
            hitbox.offset = walkColliderOffset;
            return;
        }

        hitbox.size = shellColliderSize;
        hitbox.offset = shellColliderOffset;
    }

    void SetPhysicsActive(bool active)
    {
        if (rb == null)
        {
            return;
        }

        rb.simulated = active;
        if (!active)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    Bounds GetWorldBounds(Vector2 localOffset, Vector2 localSize)
    {
        Vector3 worldCenter = transform.TransformPoint(localOffset);
        Vector3 lossyScale = transform.lossyScale;
        Vector3 size = new Vector3(
            Mathf.Abs(localSize.x * lossyScale.x),
            Mathf.Abs(localSize.y * lossyScale.y),
            0.1f
        );

        return new Bounds(worldCenter, size);
    }

    bool ContainsPoint(Bounds bounds, Vector2 point)
    {
        return point.x >= bounds.min.x &&
               point.x <= bounds.max.x &&
               point.y >= bounds.min.y &&
               point.y <= bounds.max.y;
    }

    bool IsRaceActive()
    {
        return BuildPhaseManager.Instance == null || BuildPhaseManager.Instance.IsRaceActive;
    }

    public void TeleportTo(Vector3 position)
    {
        transform.position = position;

        if (rb == null)
        {
            CacheComponents();
        }

        if (rb != null)
        {
            rb.position = position;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void LoadDefaultSpritesIfNeeded()
    {
#if UNITY_EDITOR
        Object[] spriteAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Picture/tilemap-characters.png");
        Dictionary<string, Sprite> spritesByName = new Dictionary<string, Sprite>();

        foreach (Object asset in spriteAssets)
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null)
            {
                spritesByName[sprite.name] = sprite;
            }
        }

        if (walkFrameA == null &&
            spritesByName.TryGetValue("tilemap-characters_18", out Sprite beetleWalkA))
        {
            walkFrameA = beetleWalkA;
        }

        if (walkFrameB == null &&
            spritesByName.TryGetValue("tilemap-characters_19", out Sprite beetleWalkB))
        {
            walkFrameB = beetleWalkB;
        }

        if (shellSprite == null &&
            spritesByName.TryGetValue("tilemap-characters_20", out Sprite beetleShell))
        {
            shellSprite = beetleShell;
        }

        if (walkFrameB == null)
        {
            walkFrameB = walkFrameA;
        }
#endif
    }

    public void ResetEnemy(bool forceFullReset)
    {
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        transform.localScale = spawnScale;
        movingRight = false;
        state = BeetleState.Walking;
        animationTimer = 0f;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        ApplyColliderForState();
        ApplySprite(0f);
        SetPhysicsActive(IsRaceActive());
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0.35f, 1f, 0.35f, 0.9f);
        DrawBoundsGizmo(GetWorldBounds(backHitboxOffset, backHitboxSize));
        Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.9f);
        DrawBoundsGizmo(GetWorldBounds(bodyHitboxOffset, bodyHitboxSize));
    }

    void DrawBoundsGizmo(Bounds bounds)
    {
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    IEnumerator TemporarilyIgnoreCollision(Collider2D otherCollider)
    {
        if (otherCollider == null || hitbox == null)
        {
            yield break;
        }

        Physics2D.IgnoreCollision(hitbox, otherCollider, true);
        yield return new WaitForSeconds(shellKickIgnoreTime);

        if (otherCollider != null && hitbox != null)
        {
            Physics2D.IgnoreCollision(hitbox, otherCollider, false);
        }
    }
}
