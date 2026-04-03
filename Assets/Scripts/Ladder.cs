using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Ladder : MonoBehaviour
{
    public const float DefaultTopSegmentLocalYOffset = -0.08f;

    [Header("Setup")]
    public int segmentCount = 2;
    public Sprite bodySprite;
    public Sprite[] topSprites;
    public int spriteSortingOrder = -1;
    public bool randomizeTopVariant;
    public int topVariantIndex;
    public bool normalizeTopToBodyWidth = true;
    public bool normalizeTopToBodyHeight = true;
    public float topSegmentLocalYOffset = DefaultTopSegmentLocalYOffset;

    [Header("Climb")]
    public float climbSpeed = 5.2f;
    public float horizontalSnapSpeed = 14f;
    public Vector2 climbZoneSize = new Vector2(0.48f, 2f);
    public Vector2 climbZoneOffset = new Vector2(0f, 0.5f);
    public float topEntryPadding = 0.2f;

    [Header("Top Support")]
    public Vector2 topSupportSize = new Vector2(0.48f, 0.16f);
    public Vector2 topSupportOffset = Vector2.zero;

    [Header("Generated References")]
    [SerializeField] Transform climbZoneTransform;
    [SerializeField] BoxCollider2D climbZoneCollider;
    [SerializeField] LadderClimbZone climbZoneMarker;
    [SerializeField] Transform topSupportTransform;
    [SerializeField] BoxCollider2D topSupportCollider;
    [SerializeField] List<SpriteRenderer> segmentRenderers = new List<SpriteRenderer>();

    public void Configure(int height, Sprite body, Sprite[] tops)
    {
        segmentCount = Mathf.Clamp(height, 2, 4);

        if (body != null)
        {
            bodySprite = body;
        }

        if (tops != null && tops.Length > 0)
        {
            topSprites = tops;
        }

        if (topSprites != null && topSprites.Length > 0)
        {
            topVariantIndex = randomizeTopVariant
                ? Random.Range(0, topSprites.Length)
                : Mathf.Clamp(topVariantIndex, 0, topSprites.Length - 1);
        }
        else
        {
            topVariantIndex = 0;
        }

        EnsureSetup(false);
    }

    public float GetSnapX()
    {
        return transform.position.x;
    }

    public float GetTopSurfaceY()
    {
        if (TryGetTopSupportBounds(out Bounds supportBounds))
        {
            return supportBounds.max.y;
        }

        return transform.position.y + segmentCount - 0.5f;
    }

    void Awake()
    {
        EnsureSetup(false);
    }

    void OnEnable()
    {
        EnsureSetup(false);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!gameObject.scene.IsValid())
        {
            return;
        }

        segmentCount = Mathf.Clamp(segmentCount, 2, 4);
        if (topSprites == null || topSprites.Length == 0)
        {
            topVariantIndex = 0;
        }
        else
        {
            topVariantIndex = Mathf.Clamp(topVariantIndex, 0, topSprites.Length - 1);
        }

        EnsureSetup(false);
    }
#endif

    void EnsureSetup(bool forceRefresh)
    {
        segmentCount = Mathf.Clamp(segmentCount, 2, 4);
        EnsureClimbZone(forceRefresh);
        EnsureTopSupport(forceRefresh);
        EnsureSegments(forceRefresh);
    }

    void EnsureClimbZone(bool forceRefresh)
    {
        bool createdZone = false;

        if (climbZoneTransform == null)
        {
            Transform existing = transform.Find("ClimbZone");
            if (existing != null)
            {
                climbZoneTransform = existing;
            }
        }

        if (climbZoneTransform == null)
        {
            GameObject zoneObject = new GameObject("ClimbZone");
            zoneObject.transform.SetParent(transform, false);
            climbZoneTransform = zoneObject.transform;
            createdZone = true;
        }

        if (climbZoneCollider == null)
        {
            climbZoneCollider = climbZoneTransform.GetComponent<BoxCollider2D>();
        }

        if (climbZoneCollider == null)
        {
            climbZoneCollider = climbZoneTransform.gameObject.AddComponent<BoxCollider2D>();
            createdZone = true;
        }

        if (climbZoneMarker == null)
        {
            climbZoneMarker = climbZoneTransform.GetComponent<LadderClimbZone>();
        }

        if (climbZoneMarker == null)
        {
            climbZoneMarker = climbZoneTransform.gameObject.AddComponent<LadderClimbZone>();
            createdZone = true;
        }

        climbZoneMarker.owner = this;
        climbZoneCollider.isTrigger = true;

        if (!(forceRefresh || createdZone))
        {
            return;
        }

        float topPadding = Mathf.Max(0f, topEntryPadding);
        climbZoneTransform.localPosition = Vector3.zero;
        climbZoneCollider.offset = new Vector2(
            climbZoneOffset.x,
            climbZoneOffset.y + (segmentCount - 2) * 0.5f + topPadding * 0.5f
        );
        climbZoneCollider.size = new Vector2(
            climbZoneSize.x,
            climbZoneSize.y + (segmentCount - 2) + topPadding
        );
    }

    void EnsureTopSupport(bool forceRefresh)
    {
        bool createdSupport = false;

        if (topSupportTransform == null)
        {
            Transform existing = transform.Find("TopSupport");
            if (existing != null)
            {
                topSupportTransform = existing;
            }
        }

        if (topSupportTransform == null)
        {
            GameObject supportObject = new GameObject("TopSupport");
            supportObject.transform.SetParent(transform, false);
            topSupportTransform = supportObject.transform;
            createdSupport = true;
        }

        if (topSupportCollider == null)
        {
            topSupportCollider = topSupportTransform.GetComponent<BoxCollider2D>();
        }

        if (topSupportCollider == null)
        {
            topSupportCollider = topSupportTransform.gameObject.AddComponent<BoxCollider2D>();
            createdSupport = true;
        }

        topSupportCollider.isTrigger = true;

        if (!(forceRefresh || createdSupport))
        {
            return;
        }

        float clampedWidth = Mathf.Max(0.05f, topSupportSize.x);
        float clampedHeight = Mathf.Max(0.04f, topSupportSize.y);
        float defaultTopSurfaceY = segmentCount - 0.5f;

        topSupportTransform.localPosition = Vector3.zero;
        topSupportCollider.offset = new Vector2(
            topSupportOffset.x,
            defaultTopSurfaceY - clampedHeight * 0.5f + topSupportOffset.y
        );
        topSupportCollider.size = new Vector2(clampedWidth, clampedHeight);
    }

    void EnsureSegments(bool forceRefresh)
    {
        segmentRenderers.Clear();

        for (int i = 0; i < segmentCount; i++)
        {
            bool createdSegment = false;
            Transform existing = transform.Find("Segment_" + i);
            if (existing == null)
            {
                GameObject segmentObject = new GameObject("Segment_" + i);
                segmentObject.transform.SetParent(transform, false);
                existing = segmentObject.transform;
                createdSegment = true;
            }

            SpriteRenderer renderer = existing.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = existing.gameObject.AddComponent<SpriteRenderer>();
                createdSegment = true;
            }

            segmentRenderers.Add(renderer);

            if (forceRefresh || createdSegment)
            {
                renderer.transform.localPosition = new Vector3(
                    0f,
                    i == segmentCount - 1 ? i + topSegmentLocalYOffset : i,
                    0f
                );
                renderer.transform.localRotation = Quaternion.identity;
                renderer.transform.localScale = Vector3.one;
            }
        }

        List<Transform> extras = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child == climbZoneTransform)
            {
                continue;
            }

            if (!child.name.StartsWith("Segment_"))
            {
                continue;
            }

            bool keep = false;
            for (int i = 0; i < segmentCount; i++)
            {
                if (child.name == "Segment_" + i)
                {
                    keep = true;
                    break;
                }
            }

            if (!keep)
            {
                extras.Add(child);
            }
        }

        for (int i = 0; i < extras.Count; i++)
        {
            if (Application.isPlaying)
            {
                Destroy(extras[i].gameObject);
            }
            else
            {
                DestroyImmediate(extras[i].gameObject);
            }
        }

        for (int i = 0; i < segmentRenderers.Count; i++)
        {
            if (segmentRenderers[i] == null)
            {
                continue;
            }

            Sprite segmentSprite = i == segmentCount - 1
                ? GetTopSprite()
                : bodySprite;

            segmentRenderers[i].sortingOrder = spriteSortingOrder;
            segmentRenderers[i].sprite = segmentSprite;
            segmentRenderers[i].color = Color.white;
        }
    }

    Sprite GetTopSprite()
    {
        if (topSprites != null && topSprites.Length > 0)
        {
            int safeIndex = Mathf.Clamp(topVariantIndex, 0, topSprites.Length - 1);
            if (topSprites[safeIndex] != null)
            {
                return topSprites[safeIndex];
            }

            for (int i = 0; i < topSprites.Length; i++)
            {
                if (topSprites[i] != null)
                {
                    return topSprites[i];
                }
            }
        }

        return bodySprite;
    }

    public static Vector3 CalculateSegmentScale(
        Sprite body,
        Sprite segmentSprite,
        bool isTopSegment,
        bool normalizeWidth,
        bool normalizeHeight
    )
    {
        if (!isTopSegment)
        {
            return Vector3.one;
        }

        float scaleX = 1f;
        float scaleY = 1f;

        float bodyWidth = GetSpriteWorldWidth(body);
        float bodyHeight = GetSpriteWorldHeight(body);
        float segmentWidth = GetSpriteWorldWidth(segmentSprite);
        float segmentHeight = GetSpriteWorldHeight(segmentSprite);

        if (normalizeWidth && bodyWidth > 0.0001f && segmentWidth > 0.0001f)
        {
            scaleX = bodyWidth / segmentWidth;
        }

        if (normalizeHeight && bodyHeight > 0.0001f && segmentHeight > 0.0001f)
        {
            scaleY = bodyHeight / segmentHeight;
        }

        return new Vector3(scaleX, scaleY, 1f);
    }

    static float GetSpriteWorldWidth(Sprite sprite)
    {
        return sprite != null ? sprite.bounds.size.x : 0f;
    }

    static float GetSpriteWorldHeight(Sprite sprite)
    {
        return sprite != null ? sprite.bounds.size.y : 0f;
    }

    public bool TryGetTopSupportBounds(out Bounds bounds)
    {
        if (topSupportTransform != null && topSupportCollider != null)
        {
            Vector3 worldCenter = topSupportTransform.TransformPoint(topSupportCollider.offset);
            Vector3 lossyScale = topSupportTransform.lossyScale;
            Vector3 worldSize = new Vector3(
                Mathf.Abs(topSupportCollider.size.x * lossyScale.x),
                Mathf.Abs(topSupportCollider.size.y * lossyScale.y),
                0.01f
            );
            bounds = new Bounds(worldCenter, worldSize);
            return true;
        }

        bounds = default;
        return false;
    }
}
