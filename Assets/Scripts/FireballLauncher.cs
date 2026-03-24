using UnityEngine;

public class FireballLauncher : MonoBehaviour
{
    public Sprite launcherSprite;
    public GameObject projectilePrefab;
    public float shotInterval = 1.4f;
    public float projectileOffset = 0.62f;
    public float projectileHeightOffset = 0.18f;
    public Vector2 colliderSize = new Vector2(0.96f, 0.96f);
    public bool facingRight;

    SpriteRenderer spriteRenderer;
    BoxCollider2D bodyCollider;
    float shotTimer;

    public Sprite PreviewSprite
    {
        get { return launcherSprite; }
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (launcherSprite != null)
        {
            spriteRenderer.sprite = launcherSprite;
        }

        ApplyFacingVisual();

        bodyCollider = GetComponent<BoxCollider2D>();
        if (bodyCollider == null)
        {
            bodyCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        bodyCollider.isTrigger = false;
        bodyCollider.size = colliderSize;
    }

    void OnEnable()
    {
        shotTimer = shotInterval;
    }

    void Update()
    {
        if (!CanShoot())
        {
            return;
        }

        shotTimer -= Time.deltaTime;
        if (shotTimer > 0f)
        {
            return;
        }

        shotTimer = shotInterval;
        Fire();
    }

    bool CanShoot()
    {
        if (Time.timeScale <= 0f)
        {
            return false;
        }

        if (BuildPhaseManager.Instance != null && !BuildPhaseManager.Instance.IsRaceActive)
        {
            return false;
        }

        return projectilePrefab != null;
    }

    void Fire()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        Vector3 spawnPosition = transform.position + new Vector3(
            facingRight ? projectileOffset : -projectileOffset,
            projectileHeightOffset,
            0f
        );
        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        FireballProjectile projectile = projectileObject.GetComponent<FireballProjectile>();

        if (projectile != null)
        {
            projectile.Launch(direction);
        }
    }

    public void SetFacingRight(bool value)
    {
        facingRight = value;
        ApplyFacingVisual();
    }

    void ApplyFacingVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.flipX = facingRight;
    }
}
