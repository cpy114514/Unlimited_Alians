using System.Collections.Generic;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
    public Sprite idleSprite;
    public Sprite bounceSprite;
    public float bounceForce = 17f;
    public float jumpBoostForce = 21f;
    public float bounceCooldown = 0.2f;
    public float pressedDuration = 0.12f;
    public Vector2 bodySize = new Vector2(0.96f, 0.46f);
    public Vector2 bodyOffset = new Vector2(0f, -0.04f);
    public Vector2 triggerSize = new Vector2(0.92f, 0.16f);
    public Vector2 triggerOffset = new Vector2(0f, 0.22f);

    SpriteRenderer spriteRenderer;
    BoxCollider2D bodyCollider;
    BoxCollider2D groundSupportCollider;
    BoxCollider2D bounceTrigger;
    GameObject groundSupportObject;
    TrampolineBounceTrigger bounceTriggerRelay;
    readonly Dictionary<PlayerController, float> lastBounceTimes =
        new Dictionary<PlayerController, float>();

    float pressedTimer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CacheCollider();
        ConfigureCollider();
        EnsureGroundSupportCollider();
        EnsureBounceTrigger();
        UpdateSprite();
    }

    void Update()
    {
        if (pressedTimer > 0f)
        {
            pressedTimer -= Time.deltaTime;
            if (pressedTimer <= 0f)
            {
                pressedTimer = 0f;
                UpdateSprite();
            }
        }
    }

    public bool TryTriggerAutoBounce(PlayerController player)
    {
        return TryLaunch(player, bounceForce);
    }

    public bool TryTriggerGroundJumpBoost(PlayerController player, float playerJumpForce)
    {
        float assistedForce = bounceForce + Mathf.Max(0f, playerJumpForce) * 0.1f;
        return TryLaunch(player, assistedForce, true);
    }

    bool TryLaunch(PlayerController player, float force, bool ignoreCooldown = false)
    {
        if (player == null || !player.canControl)
        {
            return false;
        }

        if (!ignoreCooldown &&
            lastBounceTimes.TryGetValue(player, out float lastBounceTime) &&
            Time.time - lastBounceTime < bounceCooldown)
        {
            return false;
        }

        lastBounceTimes[player] = Time.time;
        pressedTimer = pressedDuration;
        UpdateSprite();
        player.Launch(transform.up.normalized * force);
        return true;
    }

    public void NotifyBounceZone(Collider2D other)
    {
        PlayerController player = other != null ? other.GetComponentInParent<PlayerController>() : null;

        if (player == null || !player.canControl)
        {
            return;
        }

        if (lastBounceTimes.TryGetValue(player, out float lastBounceTime) &&
            Time.time - lastBounceTime < bounceCooldown)
        {
            return;
        }

        if (!IsPlayerAboveBounceFace(other))
        {
            return;
        }

        player.RegisterTrampolineContact(this);
    }

    bool IsPlayerAboveBounceFace(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        Vector2 localCenter = transform.InverseTransformPoint(other.bounds.center);
        float localSurfaceY = bodyOffset.y + bodySize.y * 0.5f;
        float localHalfWidth = bodySize.x * 0.5f;
        float sideTolerance = 0.1f;
        float topTolerance = 0.1f;

        bool aboveTop = localCenter.y >= localSurfaceY + topTolerance;
        bool withinBodyWidth = Mathf.Abs(localCenter.x - bodyOffset.x) <= localHalfWidth + sideTolerance;
        return aboveTop && withinBodyWidth;
    }

    void CacheCollider()
    {
        bodyCollider = GetComponent<BoxCollider2D>();

        if (bodyCollider == null)
        {
            bodyCollider = gameObject.AddComponent<BoxCollider2D>();
        }
    }

    void ConfigureCollider()
    {
        if (bodyCollider != null)
        {
            // Keep the root object on Default and let the ground child handle
            // normal "block" collisions so player ground checks still work.
            bodyCollider.enabled = false;
            bodyCollider.isTrigger = true;
            bodyCollider.size = bodySize;
            bodyCollider.offset = bodyOffset;
        }
    }

    void EnsureGroundSupportCollider()
    {
        if (groundSupportObject == null)
        {
            Transform existing = transform.Find("GroundSupport");
            groundSupportObject = existing != null ? existing.gameObject : null;
        }

        if (groundSupportObject == null)
        {
            groundSupportObject = new GameObject("GroundSupport");
            groundSupportObject.transform.SetParent(transform, false);
        }

        groundSupportCollider = groundSupportObject.GetComponent<BoxCollider2D>();
        if (groundSupportCollider == null)
        {
            groundSupportCollider = groundSupportObject.AddComponent<BoxCollider2D>();
        }

        groundSupportCollider.enabled = true;
        groundSupportCollider.isTrigger = false;
        groundSupportCollider.size = bodySize;
        groundSupportCollider.offset = bodyOffset;
        groundSupportObject.layer = GetGroundLayer();
    }

    void EnsureBounceTrigger()
    {
        if (bounceTriggerRelay == null)
        {
            bounceTriggerRelay = GetComponentInChildren<TrampolineBounceTrigger>(true);
        }

        GameObject triggerObject;
        if (bounceTriggerRelay == null)
        {
            triggerObject = new GameObject("BounceTrigger");
            triggerObject.transform.SetParent(transform, false);
            bounceTriggerRelay = triggerObject.AddComponent<TrampolineBounceTrigger>();
        }
        else
        {
            triggerObject = bounceTriggerRelay.gameObject;
        }

        bounceTriggerRelay.owner = this;

        bounceTrigger = triggerObject.GetComponent<BoxCollider2D>();
        if (bounceTrigger == null)
        {
            bounceTrigger = triggerObject.AddComponent<BoxCollider2D>();
        }

        bounceTrigger.isTrigger = true;
        bounceTrigger.size = triggerSize;
        bounceTrigger.offset = triggerOffset;
        triggerObject.layer = gameObject.layer;
    }

    int GetGroundLayer()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        return groundLayer >= 0 ? groundLayer : 6;
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (pressedTimer > 0f && bounceSprite != null)
        {
            spriteRenderer.sprite = bounceSprite;
            return;
        }

        if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }
}

public class TrampolineBounceTrigger : MonoBehaviour
{
    [HideInInspector]
    public Trampoline owner;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null)
        {
            owner.NotifyBounceZone(other);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (owner != null)
        {
            owner.NotifyBounceZone(other);
        }
    }
}
