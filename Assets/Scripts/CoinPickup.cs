using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public Sprite frameA;
    public Sprite frameB;
    public float animationSpeed = 8f;

    SpriteRenderer spriteRenderer;
    Collider2D triggerCollider;
    bool collected;
    float animationTimer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<Collider2D>();
        ResetCoin();
    }

    void Update()
    {
        if (collected || spriteRenderer == null || frameA == null || frameB == null)
        {
            return;
        }

        animationTimer += Time.deltaTime * animationSpeed;
        bool useFirstFrame = Mathf.FloorToInt(animationTimer) % 2 == 0;
        spriteRenderer.sprite = useFirstFrame ? frameA : frameB;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null || RoundManager.Instance == null ||
            !RoundManager.Instance.CanCollectCoin(player.controlType))
        {
            return;
        }

        collected = true;
        RoundManager.Instance.PlayerCollectedCoin(player.controlType);

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }

    public void ResetCoin()
    {
        collected = false;
        animationTimer = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = frameA != null ? frameA : spriteRenderer.sprite;
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
    }
}
