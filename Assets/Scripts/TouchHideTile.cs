using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class TouchHideTile : MonoBehaviour
{
    [Header("Colliders")]
    public Vector2 solidSize = new Vector2(0.96f, 0.96f);
    public Vector2 solidLocalPosition = Vector2.zero;
    public Vector2 triggerSize = new Vector2(1f, 1f);
    public Vector2 triggerLocalPosition = Vector2.zero;

    [Header("Connected Group")]
    public bool hideOnlyWhenConnected = true;
    public string connectionGroupId = string.Empty;
    public Vector2 connectionCellSize = Vector2.one;
    public float connectionTolerance = 0.1f;
    public bool includeDiagonalConnections;

    [Header("State")]
    public float reappearDelay = 0f;

    class GroupState
    {
        public TouchHideTile leader;
        public readonly List<TouchHideTile> members = new List<TouchHideTile>();
        public readonly Dictionary<PlayerController, int> overlapCounts =
            new Dictionary<PlayerController, int>();
        public float reappearDelay;
        public float reappearTimer;
        public bool canHide;
    }

    static readonly List<TouchHideTile> activeTiles = new List<TouchHideTile>();
    static readonly HashSet<int> dirtySceneHandles = new HashSet<int>();

    [Header("Generated References")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] BoxCollider2D solidCollider;
    [SerializeField] BoxCollider2D triggerCollider;
    [SerializeField] TouchHideTileTrigger triggerRelay;
    [SerializeField] GameObject solidObject;
    [SerializeField] GameObject triggerObject;
    GroupState groupState;
    [SerializeField, HideInInspector] bool authoredSpriteVisible = true;
    [SerializeField, HideInInspector] bool authoredSolidColliderVisible = true;

    void Awake()
    {
        CacheComponents();
        CaptureAuthoredVisibleState();
        EnsureGroupAssignments();
        if (Application.isPlaying)
        {
            ApplyVisibleState(true);
        }
    }

    void OnEnable()
    {
        CacheComponents();
        if (!Application.isPlaying)
        {
            CaptureAuthoredVisibleState();
        }

        RegisterTile();
        if (Application.isPlaying)
        {
            ApplyVisibleState(true);
        }
    }

    void OnDisable()
    {
        UnregisterTile();
        groupState = null;
        if (Application.isPlaying)
        {
            ApplyVisibleState(true);
        }
    }

    void OnValidate()
    {
        CacheComponents();
        solidSize = ClampSize(solidSize);
        triggerSize = ClampSize(triggerSize);
        connectionCellSize = ClampSize(connectionCellSize);
        connectionTolerance = Mathf.Clamp(connectionTolerance, 0f, Mathf.Max(connectionCellSize.x, connectionCellSize.y));
        reappearDelay = Mathf.Max(0f, reappearDelay);
        CaptureAuthoredVisibleState();
        MarkSceneDirty();
    }

    void Update()
    {
        EnsureGroupAssignments();

        if (groupState == null)
        {
            ApplyVisibleState(true);
            return;
        }

        if (groupState.leader != this)
        {
            return;
        }

        UpdateGroupState(groupState);
    }

    public void NotifyTriggerEnter(Collider2D other)
    {
        if (!TryGetPlayer(other, out PlayerController player))
        {
            return;
        }

        EnsureGroupAssignments();
        if (groupState == null || !groupState.canHide)
        {
            return;
        }

        groupState.overlapCounts.TryGetValue(player, out int count);
        groupState.overlapCounts[player] = count + 1;
        groupState.reappearTimer = groupState.reappearDelay;
        ApplyGroupVisibleState(groupState, false);
    }

    public void NotifyTriggerExit(Collider2D other)
    {
        if (!TryGetPlayer(other, out PlayerController player))
        {
            return;
        }

        EnsureGroupAssignments();
        if (groupState == null || !groupState.canHide)
        {
            return;
        }

        if (!groupState.overlapCounts.TryGetValue(player, out int count))
        {
            return;
        }

        count--;
        if (count <= 0)
        {
            groupState.overlapCounts.Remove(player);
        }
        else
        {
            groupState.overlapCounts[player] = count;
        }

        if (groupState.overlapCounts.Count == 0)
        {
            groupState.reappearTimer = groupState.reappearDelay;
        }
    }

    void CacheComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureSolidCollider();
        EnsureTriggerCollider();
    }

    void EnsureSolidCollider()
    {
        bool createdObject = false;
        if (solidObject == null)
        {
            Transform existing = transform.Find("SolidCollider");
            solidObject = existing != null ? existing.gameObject : null;
        }

        if (solidObject == null)
        {
            solidObject = new GameObject("SolidCollider");
            solidObject.transform.SetParent(transform, false);
            createdObject = true;
        }

        bool createdCollider = false;
        solidCollider = solidObject.GetComponent<BoxCollider2D>();
        if (solidCollider == null)
        {
            solidCollider = solidObject.AddComponent<BoxCollider2D>();
            createdCollider = true;
        }

        if (createdObject || createdCollider)
        {
            solidCollider.isTrigger = false;
            solidCollider.enabled = true;
            solidObject.layer = GetGroundLayer();
            solidObject.transform.localPosition = solidLocalPosition;
            solidCollider.size = solidSize;
            solidCollider.offset = Vector2.zero;
            solidObject.transform.localScale = Vector3.one;
        }
    }

    void EnsureTriggerCollider()
    {
        bool createdObject = false;
        if (triggerObject == null)
        {
            Transform existing = transform.Find("TouchTrigger");
            triggerObject = existing != null ? existing.gameObject : null;
        }

        if (triggerObject == null)
        {
            triggerObject = new GameObject("TouchTrigger");
            triggerObject.transform.SetParent(transform, false);
            createdObject = true;
        }

        triggerRelay = triggerObject.GetComponent<TouchHideTileTrigger>();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(triggerObject);
            triggerRelay = triggerObject.GetComponent<TouchHideTileTrigger>();
        }
#endif
        if (triggerRelay == null)
        {
            triggerRelay = triggerObject.AddComponent<TouchHideTileTrigger>();
        }

        bool createdCollider = false;
        triggerCollider = triggerObject.GetComponent<BoxCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = triggerObject.AddComponent<BoxCollider2D>();
            createdCollider = true;
        }

        triggerRelay.owner = this;

        if (createdObject || createdCollider)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = true;
            triggerObject.layer = gameObject.layer;
            triggerObject.transform.localPosition = triggerLocalPosition;
            triggerCollider.size = triggerSize;
            triggerCollider.offset = Vector2.zero;
            triggerObject.transform.localScale = Vector3.one;
        }
    }

    void ApplyVisibleState(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible && authoredSpriteVisible;
        }

        if (solidCollider != null)
        {
            solidCollider.enabled = visible && authoredSolidColliderVisible;
        }
    }

    void CaptureAuthoredVisibleState()
    {
        if (spriteRenderer != null)
        {
            authoredSpriteVisible = spriteRenderer.enabled;
        }

        if (solidCollider != null)
        {
            authoredSolidColliderVisible = solidCollider.enabled;
        }
    }

    void RegisterTile()
    {
        if (!activeTiles.Contains(this))
        {
            activeTiles.Add(this);
        }

        MarkSceneDirty();
    }

    void UnregisterTile()
    {
        activeTiles.Remove(this);
        MarkSceneDirty();
    }

    void MarkSceneDirty()
    {
        if (!gameObject.scene.IsValid())
        {
            return;
        }

        dirtySceneHandles.Add(gameObject.scene.handle);
    }

    void EnsureGroupAssignments()
    {
        if (!gameObject.scene.IsValid())
        {
            return;
        }

        if (groupState != null && !dirtySceneHandles.Contains(gameObject.scene.handle))
        {
            return;
        }

        RebuildGroupsForScene(gameObject.scene.handle);
    }

    static void RebuildGroupsForScene(int sceneHandle)
    {
        List<TouchHideTile> sceneTiles = new List<TouchHideTile>();
        for (int i = 0; i < activeTiles.Count; i++)
        {
            TouchHideTile tile = activeTiles[i];
            if (tile == null || !tile.isActiveAndEnabled || tile.gameObject.scene.handle != sceneHandle)
            {
                continue;
            }

            tile.groupState = null;
            sceneTiles.Add(tile);
        }

        HashSet<TouchHideTile> assignedTiles = new HashSet<TouchHideTile>();
        for (int i = 0; i < sceneTiles.Count; i++)
        {
            TouchHideTile startTile = sceneTiles[i];
            if (startTile == null || assignedTiles.Contains(startTile))
            {
                continue;
            }

            List<TouchHideTile> cluster = CollectConnectedTiles(startTile, sceneTiles, assignedTiles);
            if (cluster.Count == 0)
            {
                continue;
            }

            GroupState state = new GroupState();
            state.leader = cluster[0];
            state.reappearDelay = 0f;

            for (int memberIndex = 0; memberIndex < cluster.Count; memberIndex++)
            {
                TouchHideTile member = cluster[memberIndex];
                if (member == null)
                {
                    continue;
                }

                state.members.Add(member);
                state.reappearDelay = Mathf.Max(state.reappearDelay, member.reappearDelay);
                member.groupState = state;
            }

            bool anyTileAllowsSingleHide = false;
            for (int memberIndex = 0; memberIndex < state.members.Count; memberIndex++)
            {
                if (!state.members[memberIndex].hideOnlyWhenConnected)
                {
                    anyTileAllowsSingleHide = true;
                    break;
                }
            }

            state.canHide = state.members.Count > 1 || anyTileAllowsSingleHide;

            if (!state.canHide)
            {
                ApplyGroupVisibleState(state, true);
            }
        }

        dirtySceneHandles.Remove(sceneHandle);
    }

    static List<TouchHideTile> CollectConnectedTiles(
        TouchHideTile startTile,
        List<TouchHideTile> candidates,
        HashSet<TouchHideTile> assignedTiles
    )
    {
        List<TouchHideTile> cluster = new List<TouchHideTile>();
        Queue<TouchHideTile> pending = new Queue<TouchHideTile>();

        pending.Enqueue(startTile);
        assignedTiles.Add(startTile);

        while (pending.Count > 0)
        {
            TouchHideTile current = pending.Dequeue();
            cluster.Add(current);

            for (int i = 0; i < candidates.Count; i++)
            {
                TouchHideTile candidate = candidates[i];
                if (candidate == null || assignedTiles.Contains(candidate) || !current.CanConnectTo(candidate))
                {
                    continue;
                }

                assignedTiles.Add(candidate);
                pending.Enqueue(candidate);
            }
        }

        return cluster;
    }

    bool CanConnectTo(TouchHideTile other)
    {
        if (other == null || other == this)
        {
            return false;
        }

        if (other.gameObject.scene.handle != gameObject.scene.handle)
        {
            return false;
        }

        if (GetConnectionKey() != other.GetConnectionKey())
        {
            return false;
        }

        Vector2 currentStep = GetClampedCellSize(connectionCellSize);
        Vector2 otherStep = GetClampedCellSize(other.connectionCellSize);
        Vector2 step = new Vector2(
            Mathf.Max(currentStep.x, otherStep.x),
            Mathf.Max(currentStep.y, otherStep.y)
        );
        float tolerance = Mathf.Max(connectionTolerance, other.connectionTolerance);
        Vector2 delta = (Vector2)(other.transform.position - transform.position);
        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);

        bool horizontalNeighbor =
            Mathf.Abs(absX - step.x) <= tolerance &&
            absY <= tolerance;
        bool verticalNeighbor =
            Mathf.Abs(absY - step.y) <= tolerance &&
            absX <= tolerance;

        if (horizontalNeighbor || verticalNeighbor)
        {
            return true;
        }

        if (!includeDiagonalConnections && !other.includeDiagonalConnections)
        {
            return false;
        }

        bool diagonalNeighbor =
            Mathf.Abs(absX - step.x) <= tolerance &&
            Mathf.Abs(absY - step.y) <= tolerance;
        return diagonalNeighbor;
    }

    string GetConnectionKey()
    {
        if (!string.IsNullOrWhiteSpace(connectionGroupId))
        {
            return connectionGroupId.Trim();
        }

        return spriteRenderer != null && spriteRenderer.sprite != null
            ? spriteRenderer.sprite.GetInstanceID().ToString()
            : gameObject.name;
    }

    void UpdateGroupState(GroupState state)
    {
        if (state == null)
        {
            ApplyVisibleState(true);
            return;
        }

        if (!state.canHide)
        {
            ApplyGroupVisibleState(state, true);
            return;
        }

        PruneInvalidPlayers(state);

        if (state.overlapCounts.Count > 0)
        {
            state.reappearTimer = state.reappearDelay;
            ApplyGroupVisibleState(state, false);
            return;
        }

        if (state.reappearTimer > 0f)
        {
            state.reappearTimer = Mathf.Max(0f, state.reappearTimer - Time.deltaTime);
            if (state.reappearTimer > 0f)
            {
                ApplyGroupVisibleState(state, false);
                return;
            }
        }

        ApplyGroupVisibleState(state, true);
    }

    static void ApplyGroupVisibleState(GroupState state, bool visible)
    {
        if (state == null)
        {
            return;
        }

        for (int i = 0; i < state.members.Count; i++)
        {
            TouchHideTile member = state.members[i];
            if (member == null)
            {
                continue;
            }

            member.ApplyVisibleState(visible);
        }
    }

    static void PruneInvalidPlayers(GroupState state)
    {
        if (state == null || state.overlapCounts.Count == 0)
        {
            return;
        }

        List<PlayerController> invalidPlayers = null;
        foreach (KeyValuePair<PlayerController, int> pair in state.overlapCounts)
        {
            if (pair.Key != null && pair.Key.gameObject.activeInHierarchy)
            {
                continue;
            }

            invalidPlayers ??= new List<PlayerController>();
            invalidPlayers.Add(pair.Key);
        }

        if (invalidPlayers == null)
        {
            return;
        }

        for (int i = 0; i < invalidPlayers.Count; i++)
        {
            state.overlapCounts.Remove(invalidPlayers[i]);
        }
    }

    bool TryGetPlayer(Collider2D other, out PlayerController player)
    {
        player = other != null ? other.GetComponentInParent<PlayerController>() : null;
        return player != null;
    }

    int GetGroundLayer()
    {
        int layer = LayerMask.NameToLayer("Ground");
        return layer >= 0 ? layer : gameObject.layer;
    }

    static Vector2 ClampSize(Vector2 size)
    {
        return new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
    }

    static Vector2 GetClampedCellSize(Vector2 size)
    {
        return new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
    }
}
