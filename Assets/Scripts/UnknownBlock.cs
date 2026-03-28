using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class UnknownBlock : MonoBehaviour
{
    [Serializable]
    public class SpawnOption
    {
        public GameObject prefab;
        public float weight = 1f;
    }

    [Header("Visual")]
    public Sprite unusedSprite;
    public Sprite usedSprite;
    public Color usedTint = Color.white;
    public float bumpDistance = 0.12f;
    public float bumpDuration = 0.14f;

    [Header("Hit Trigger")]
    public Vector2 hitTriggerSize = new Vector2(0.84f, 0.18f);
    public Vector2 hitTriggerLocalPosition = new Vector2(0f, -0.57f);

    [Header("Spawn")]
    public Vector3 spawnOffset = new Vector3(0f, 0.9f, 0f);
    public float spawnUpwardSpeed = 3.5f;
    public Vector3 beetleSpawnOffset = new Vector3(0f, 1.05f, 0f);
    public float beetleUpwardSpeed = 4.25f;
    public float beetleHorizontalSpeed = 2.4f;
    public List<SpawnOption> spawnOptions = new List<SpawnOption>();

    SpriteRenderer spriteRenderer;
    BoxCollider2D blockCollider;
    BoxCollider2D hitTriggerCollider;
    UnknownBlockHitTrigger hitTriggerRelay;
    Vector3 baseLocalPosition;
    bool used;
    float bumpTimer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        blockCollider = GetComponent<BoxCollider2D>();
        EnsureHitTrigger();
        baseLocalPosition = transform.localPosition;

        if (unusedSprite == null && spriteRenderer != null)
        {
            unusedSprite = spriteRenderer.sprite;
        }
    }

    void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (unusedSprite == null && spriteRenderer != null)
        {
            unusedSprite = spriteRenderer.sprite;
        }

        EnsureHitTrigger();
    }

    void Update()
    {
        if (bumpTimer <= 0f)
        {
            return;
        }

        bumpTimer -= Time.deltaTime;
        float normalizedTime = 1f - Mathf.Clamp01(bumpTimer / Mathf.Max(0.01f, bumpDuration));
        float bumpOffset =
            normalizedTime < 0.5f
                ? Mathf.Lerp(0f, bumpDistance, normalizedTime / 0.5f)
                : Mathf.Lerp(bumpDistance, 0f, (normalizedTime - 0.5f) / 0.5f);

        transform.localPosition = baseLocalPosition + Vector3.up * bumpOffset;

        if (bumpTimer <= 0f)
        {
            bumpTimer = 0f;
            transform.localPosition = baseLocalPosition;
        }
    }

    public void NotifyHitTrigger(Collider2D other)
    {
        if (used)
        {
            return;
        }

        if (!TryGetBumpingPlayer(other, out PlayerController player))
        {
            return;
        }

        Activate(player);
    }

    bool TryGetBumpingPlayer(Collider2D other, out PlayerController player)
    {
        player = other != null
            ? other.GetComponentInParent<PlayerController>()
            : null;

        if (player == null || !player.canControl || blockCollider == null || hitTriggerCollider == null)
        {
            return false;
        }

        Bounds blockBounds = blockCollider.bounds;
        Bounds playerBounds = other.bounds;
        Bounds triggerBounds = hitTriggerCollider.bounds;

        bool playerBelowBlock = playerBounds.max.y <= blockBounds.center.y + 0.05f;
        if (!playerBelowBlock || player.VerticalVelocity <= 0.05f)
        {
            return false;
        }

        bool overlapsTrigger =
            playerBounds.max.x >= triggerBounds.min.x &&
            playerBounds.min.x <= triggerBounds.max.x &&
            playerBounds.max.y >= triggerBounds.min.y;
        return overlapsTrigger;
    }

    void Activate(PlayerController triggeringPlayer)
    {
        used = true;
        bumpTimer = Mathf.Max(0.01f, bumpDuration);
        SpawnRandomItem(triggeringPlayer);
        ApplyUsedVisual();
    }

    void ApplyUsedVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (usedSprite != null)
        {
            spriteRenderer.sprite = usedSprite;
        }

        spriteRenderer.color = usedTint;
    }

    void SpawnRandomItem(PlayerController triggeringPlayer)
    {
        GameObject prefab = PickRandomPrefab();
        if (prefab == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.position + spawnOffset;
        GameObject spawned = Instantiate(prefab, spawnPosition, Quaternion.identity);

        BlueBeetleEnemy beetle = spawned.GetComponent<BlueBeetleEnemy>();
        if (beetle != null)
        {
            float horizontalDirection = 1f;
            if (triggeringPlayer != null)
            {
                horizontalDirection = triggeringPlayer.transform.position.x <= transform.position.x
                    ? 1f
                    : -1f;
            }

            beetle.LaunchFromBlock(
                transform.position + beetleSpawnOffset,
                horizontalDirection,
                beetleHorizontalSpeed,
                beetleUpwardSpeed
            );
            return;
        }

        Rigidbody2D spawnedBody = spawned.GetComponent<Rigidbody2D>();
        if (spawnedBody != null)
        {
            Vector2 launchVelocity = spawnedBody.velocity;
            launchVelocity.y = Mathf.Max(launchVelocity.y, spawnUpwardSpeed);
            spawnedBody.velocity = launchVelocity;
        }
    }

    GameObject PickRandomPrefab()
    {
        float totalWeight = 0f;

        for (int i = 0; i < spawnOptions.Count; i++)
        {
            SpawnOption option = spawnOptions[i];
            if (option == null || option.prefab == null || option.weight <= 0f)
            {
                continue;
            }

            totalWeight += option.weight;
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue = UnityEngine.Random.value * totalWeight;
        for (int i = 0; i < spawnOptions.Count; i++)
        {
            SpawnOption option = spawnOptions[i];
            if (option == null || option.prefab == null || option.weight <= 0f)
            {
                continue;
            }

            randomValue -= option.weight;
            if (randomValue <= 0f)
            {
                return option.prefab;
            }
        }

        return null;
    }

    void EnsureHitTrigger()
    {
        Transform existing = transform.Find("HitTrigger");
        GameObject triggerObject = existing != null ? existing.gameObject : null;
        bool createdObject = false;

        if (triggerObject == null)
        {
            triggerObject = new GameObject("HitTrigger");
            triggerObject.transform.SetParent(transform, false);
            createdObject = true;
        }

        hitTriggerRelay = triggerObject.GetComponent<UnknownBlockHitTrigger>();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(triggerObject);
            hitTriggerRelay = triggerObject.GetComponent<UnknownBlockHitTrigger>();
        }
#endif

        if (hitTriggerRelay == null)
        {
            hitTriggerRelay = triggerObject.AddComponent<UnknownBlockHitTrigger>();
        }

        RemoveDuplicateChildren("HitTrigger", triggerObject.transform);
        hitTriggerRelay.owner = this;

        hitTriggerCollider = triggerObject.GetComponent<BoxCollider2D>();
        bool createdCollider = false;
        if (hitTriggerCollider == null)
        {
            hitTriggerCollider = triggerObject.AddComponent<BoxCollider2D>();
            createdCollider = true;
        }

        hitTriggerCollider.enabled = true;
        hitTriggerCollider.isTrigger = true;
        triggerObject.layer = gameObject.layer;

        if (createdObject || createdCollider)
        {
            triggerObject.transform.localPosition = hitTriggerLocalPosition;
            hitTriggerCollider.size = hitTriggerSize;
            hitTriggerCollider.offset = Vector2.zero;
        }
    }

    void RemoveDuplicateChildren(string childName, Transform keepTransform)
    {
        List<GameObject> duplicates = null;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == keepTransform || child.name != childName)
            {
                continue;
            }

            duplicates ??= new List<GameObject>();
            duplicates.Add(child.gameObject);
        }

        if (duplicates == null)
        {
            return;
        }

        foreach (GameObject duplicate in duplicates)
        {
            if (Application.isPlaying)
            {
                Destroy(duplicate);
            }
            else
            {
                DestroyImmediate(duplicate);
            }
        }
    }
}
