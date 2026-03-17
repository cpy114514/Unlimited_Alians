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
    BoxCollider2D bounceTrigger;
    TrampolineBounceTrigger bounceTriggerRelay;
    readonly Dictionary<PlayerController, float> lastBounceTimes =
        new Dictionary<PlayerController, float>();

    float pressedTimer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CacheCollider();
        ConfigureCollider();
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

    public bool TryTriggerGroundJumpBoost(PlayerController player)
    {
        return TryLaunch(player, bounceForce);
    }

    bool TryLaunch(PlayerController player, float force)
    {
        if (player == null || !player.canControl)
        {
            return false;
        }

        if (lastBounceTimes.TryGetValue(player, out float lastBounceTime) &&
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

        player.RegisterTrampolineContact(this);
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
            bodyCollider.isTrigger = false;
            bodyCollider.size = bodySize;
            bodyCollider.offset = bodyOffset;
        }
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
