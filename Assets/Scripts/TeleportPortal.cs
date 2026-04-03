using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TeleportPortal : MonoBehaviour
{
    static readonly List<TeleportPortal> activePortals = new List<TeleportPortal>();
    static readonly Dictionary<int, float> entityCooldownUntil =
        new Dictionary<int, float>();

    public string portalGroup = "Default";
    public Vector2 colliderSize = new Vector2(0.58f, 0.82f);
    public Vector2 colliderOffset = new Vector2(0f, -0.04f);
    public Vector2 exitOffset = new Vector2(0f, 0.02f);
    public float teleportCooldown = 1f;

    [Header("Visuals")]
    public Sprite doorSprite;
    public Sprite glowSprite;
    public Color doorTint = Color.white;
    public Color glowTint = new Color(1f, 1f, 1f, 0.9f);
    public Vector2 doorScale = new Vector2(0.34f, 0.34f);
    public Vector2 glowScale = new Vector2(0.3f, 0.3f);
    public Vector2 glowOffset = new Vector2(0f, -0.12f);
    public float glowAnimationSpeed = 8f;

    [Header("Generated References")]
    [SerializeField] BoxCollider2D triggerCollider;
    [SerializeField] Transform doorTransform;
    [SerializeField] Transform glowTransform;
    [SerializeField] SpriteRenderer doorRenderer;
    [SerializeField] SpriteRenderer glowRenderer;
    [SerializeField] Sprite[] glowFrames;
    float nextPortalReadyTime;

    void Awake()
    {
        EnsureCollider();
        EnsureVisuals();
    }

    void OnEnable()
    {
        EnsureCollider();
        EnsureVisuals();

        if (Application.isPlaying && !activePortals.Contains(this))
        {
            activePortals.Add(this);
        }
    }

    void OnDisable()
    {
        if (Application.isPlaying)
        {
            activePortals.Remove(this);
        }
    }

    void OnValidate()
    {
        EnsureCollider();
        EnsureVisuals();
    }

    void Update()
    {
        EnsureVisuals();
        UpdateGlowVisual(Application.isPlaying ? Time.time : Time.realtimeSinceStartup);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            TryTeleportEntity(player, destination => player.TeleportTo(destination));
            return;
        }

        BlueBeetleEnemy beetle = other.GetComponentInParent<BlueBeetleEnemy>();
        if (beetle != null)
        {
            TryTeleportEntity(beetle, destination => beetle.TeleportTo(destination));
            return;
        }

        FireballProjectile projectile = other.GetComponentInParent<FireballProjectile>();
        if (projectile != null)
        {
            TryTeleportEntity(projectile, destination => projectile.TeleportTo(destination));
        }
    }

    void TryTeleportEntity(Component entity, System.Action<Vector3> teleportAction)
    {
        if (entity == null || teleportAction == null || Time.time < nextPortalReadyTime)
        {
            return;
        }

        int entityId = entity.GetInstanceID();
        if (entityCooldownUntil.TryGetValue(entityId, out float cooldownUntil) &&
            Time.time < cooldownUntil)
        {
            return;
        }

        List<TeleportPortal> targets = GetCandidateTargets();
        if (targets.Count == 0)
        {
            return;
        }

        TeleportPortal target = targets[Random.Range(0, targets.Count)];
        entityCooldownUntil[entityId] = Time.time + teleportCooldown;
        nextPortalReadyTime = Time.time + teleportCooldown;
        target.nextPortalReadyTime = Time.time + teleportCooldown;

        teleportAction(target.transform.position + (Vector3)target.exitOffset);
    }

    List<TeleportPortal> GetCandidateTargets()
    {
        List<TeleportPortal> targets = new List<TeleportPortal>();

        foreach (TeleportPortal portal in activePortals)
        {
            if (portal == null || portal == this || !portal.isActiveAndEnabled)
            {
                continue;
            }

            if (portal.portalGroup != portalGroup)
            {
                continue;
            }

            targets.Add(portal);
        }

        return targets;
    }

    void EnsureCollider()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        triggerCollider.isTrigger = true;
        triggerCollider.size = colliderSize;
        triggerCollider.offset = colliderOffset;
    }

    void EnsureVisuals()
    {
        LoadDefaultSpritesIfNeeded();
        CleanupLegacyVisuals();

        bool createdDoor = false;
        bool createdGlow = false;
        doorTransform = EnsureChild("PortalDoor", ref createdDoor);
        glowTransform = EnsureChild("PortalGlow", ref createdGlow);
        doorRenderer = EnsureSpriteRenderer(doorTransform, "PortalDoorRenderer", 6);
        glowRenderer = EnsureSpriteRenderer(glowTransform, "PortalGlowRenderer", 5);

        if (createdDoor)
        {
            doorTransform.localPosition = Vector3.zero;
            doorTransform.localRotation = Quaternion.identity;
            doorTransform.localScale = new Vector3(doorScale.x, doorScale.y, 1f);
        }

        if (createdGlow)
        {
            glowTransform.localPosition = new Vector3(glowOffset.x, glowOffset.y, 0f);
            glowTransform.localRotation = Quaternion.identity;
            glowTransform.localScale = new Vector3(glowScale.x, glowScale.y, 1f);
        }

        if (doorRenderer != null)
        {
            doorRenderer.sprite = doorSprite;
            doorRenderer.color = doorTint;
        }

        if (glowRenderer != null)
        {
            glowRenderer.sprite = GetAnimatedGlowSprite(0f);
        }
    }

    void UpdateGlowVisual(float timeValue)
    {
        if (glowTransform == null || glowRenderer == null)
        {
            return;
        }

        bool isCoolingDown = Application.isPlaying && Time.time < nextPortalReadyTime;
        bool hasLinkedPortal = !Application.isPlaying || GetCandidateTargets().Count > 0;
        glowRenderer.enabled = hasLinkedPortal && !isCoolingDown;

        if (!glowRenderer.enabled)
        {
            return;
        }

        glowRenderer.sprite = GetAnimatedGlowSprite(timeValue);
        glowRenderer.color = glowTint;
    }

    Sprite GetAnimatedGlowSprite(float timeValue)
    {
        if (glowFrames != null && glowFrames.Length > 0)
        {
            int frameIndex = Mathf.FloorToInt(timeValue * glowAnimationSpeed) % glowFrames.Length;
            return glowFrames[frameIndex];
        }

        return glowSprite;
    }

    void CleanupLegacyVisuals()
    {
        CleanupLegacyChild("PortalFrame");
        CleanupLegacyChild("PortalSwirl");
    }

    void CleanupLegacyChild(string childName)
    {
        Transform legacyChild = transform.Find(childName);
        if (legacyChild == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(legacyChild.gameObject);
        }
        else
        {
            DestroyImmediate(legacyChild.gameObject);
        }
    }

    Transform EnsureChild(string childName, ref bool created)
    {
        Transform child = transform.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(transform, false);
        created = true;
        return childObject.transform;
    }

    SpriteRenderer EnsureSpriteRenderer(Transform target, string sortingLayerName, int sortingOrder)
    {
        if (target == null)
        {
            return null;
        }

        LineRenderer legacyLineRenderer = target.GetComponent<LineRenderer>();
        if (legacyLineRenderer != null)
        {
            if (Application.isPlaying)
            {
                Destroy(legacyLineRenderer);
            }
            else
            {
                DestroyImmediate(legacyLineRenderer);
            }
        }

        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = target.gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    void LoadDefaultSpritesIfNeeded()
    {
#if UNITY_EDITOR
        if (doorSprite == null)
        {
            doorSprite = LoadSpriteFromPaths(
                "Assets/Picture/Gameplay/portal_door.png",
                "Assets/Picture/portal_door.png"
            );
        }

        Object[] glowAssets = LoadAllAssetsFromPaths(
            "Assets/Picture/Gameplay/Portal.png",
            "Assets/Picture/Portal.png"
        );
        List<Sprite> loadedGlowFrames = new List<Sprite>();

        foreach (Object asset in glowAssets)
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null)
            {
                loadedGlowFrames.Add(sprite);
            }
        }

        if (loadedGlowFrames.Count > 0)
        {
            loadedGlowFrames.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            glowFrames = loadedGlowFrames.ToArray();
            glowSprite = glowFrames[0];
        }
        else
        {
            glowFrames = null;
            if (glowSprite == null)
            {
                glowSprite = LoadSpriteFromPaths(
                    "Assets/Picture/Gameplay/Portal.png",
                    "Assets/Picture/Portal.png"
                );
            }
        }
#endif
    }

#if UNITY_EDITOR
    Sprite LoadSpriteFromPaths(params string[] candidatePaths)
    {
        for (int i = 0; i < candidatePaths.Length; i++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(candidatePaths[i]);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    Object[] LoadAllAssetsFromPaths(params string[] candidatePaths)
    {
        for (int i = 0; i < candidatePaths.Length; i++)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(candidatePaths[i]);
            if (assets != null && assets.Length > 0)
            {
                return assets;
            }
        }

        return System.Array.Empty<Object>();
    }
#endif
}
