using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapRenderer))]
[RequireComponent(typeof(TilemapCollider2D))]
public class TouchHideTilemap : MonoBehaviour
{
    [Header("Behavior")]
    public bool hideOnlyWhenConnected = true;
    public bool includeDiagonalConnections;
    public bool hidePermanently = true;
    public float reappearDelay = 0f;

    const float ContactSampleFactor = 0.22f;

    Tilemap tilemap;
    TilemapCollider2D tilemapCollider;
    readonly List<HiddenGroup> hiddenGroups = new List<HiddenGroup>();
    readonly List<Vector3Int> touchedCellsBuffer = new List<Vector3Int>();
    readonly HashSet<Vector3Int> processedCells = new HashSet<Vector3Int>();
    readonly HashSet<Vector3Int> touchedCellSet = new HashSet<Vector3Int>();
    readonly HashSet<PlayerController> overlappingPlayers = new HashSet<PlayerController>();

    struct HiddenTileState
    {
        public TileBase tile;
        public Color color;
        public Matrix4x4 transform;
        public TileFlags flags;
    }

    class HiddenGroup
    {
        public readonly Dictionary<Vector3Int, HiddenTileState> tiles =
            new Dictionary<Vector3Int, HiddenTileState>();

        public float restoreAt;
        public Bounds worldBounds;
    }

    void Awake()
    {
        CacheComponents();
    }

    void OnValidate()
    {
        CacheComponents();
        reappearDelay = Mathf.Max(0f, reappearDelay);
    }

    void Update()
    {
        if (hidePermanently || hiddenGroups.Count == 0)
        {
            return;
        }

        PruneInvalidPlayers();

        float now = Time.time;
        for (int i = hiddenGroups.Count - 1; i >= 0; i--)
        {
            HiddenGroup group = hiddenGroups[i];
            if (group == null || now < group.restoreAt)
            {
                continue;
            }

            RestoreGroup(group);
            hiddenGroups.RemoveAt(i);
        }
    }

    public void HandlePlayerCollision(PlayerController player, Collision2D collision)
    {
        if (player == null || collision == null)
        {
            return;
        }

        Collider2D playerCollider = collision.otherCollider != null
            ? collision.otherCollider
            : player.GetComponent<Collider2D>();

        RegisterOverlappingPlayer(player);
        HandlePlayerOverlap(player, playerCollider, collision);
    }

    public void HandlePlayerTrigger(PlayerController player, Collider2D playerCollider)
    {
        if (player == null)
        {
            return;
        }

        RegisterOverlappingPlayer(player);
        HandlePlayerOverlap(player, playerCollider, null);
    }

    public void HandlePlayerCollisionExit(PlayerController player)
    {
        HandlePlayerTriggerExit(player);
    }

    public void HandlePlayerTriggerExit(PlayerController player)
    {
        if (player == null || hidePermanently)
        {
            return;
        }

        if (!IsPlayerStillOverlappingHiddenArea(player))
        {
            overlappingPlayers.Remove(player);
            QueueRestoreIfClear();
        }
    }

    void CacheComponents()
    {
        if (tilemap == null)
        {
            tilemap = GetComponent<Tilemap>();
        }

        if (tilemapCollider == null)
        {
            tilemapCollider = GetComponent<TilemapCollider2D>();
        }
    }

    void HandlePlayerOverlap(PlayerController player, Collider2D playerCollider, Collision2D collision)
    {
        CacheComponents();
        if (tilemap == null || playerCollider == null)
        {
            return;
        }

        CollectTouchedCells(collision, playerCollider, touchedCellsBuffer);
        if (touchedCellsBuffer.Count == 0)
        {
            return;
        }

        processedCells.Clear();
        bool hidAnyCells = false;
        for (int i = 0; i < touchedCellsBuffer.Count; i++)
        {
            Vector3Int seedCell = touchedCellsBuffer[i];
            if (processedCells.Contains(seedCell) || !tilemap.HasTile(seedCell))
            {
                continue;
            }

            List<Vector3Int> connectedCells = CollectConnectedCells(seedCell);
            for (int cellIndex = 0; cellIndex < connectedCells.Count; cellIndex++)
            {
                processedCells.Add(connectedCells[cellIndex]);
            }

            if (connectedCells.Count == 0)
            {
                continue;
            }

            if (hideOnlyWhenConnected && connectedCells.Count <= 1)
            {
                continue;
            }

            if (HideConnectedCells(connectedCells))
            {
                hidAnyCells = true;
            }
        }

        if (hidAnyCells)
        {
            RefreshTilemapCollider();
            player.RefreshCollisionStateAfterEnvironmentChange();
        }
    }

    void CollectTouchedCells(Collision2D collision, Collider2D playerCollider, List<Vector3Int> results)
    {
        results.Clear();
        touchedCellSet.Clear();

        if (collision != null)
        {
            CollectTouchedCellsFromContacts(collision, results);
        }

        if (results.Count > 0 || playerCollider == null)
        {
            return;
        }

        CollectTouchedCellsFromBounds(playerCollider.bounds, results);
    }

    void CollectTouchedCellsFromBounds(Bounds playerBounds, List<Vector3Int> results)
    {
        Vector3 minPoint = new Vector3(playerBounds.min.x, playerBounds.min.y, 0f);
        Vector3 maxPoint = new Vector3(playerBounds.max.x, playerBounds.max.y, 0f);
        Vector3Int minCell = tilemap.WorldToCell(minPoint);
        Vector3Int maxCell = tilemap.WorldToCell(maxPoint);

        for (int x = minCell.x - 1; x <= maxCell.x + 1; x++)
        {
            for (int y = minCell.y - 1; y <= maxCell.y + 1; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!tilemap.HasTile(cell))
                {
                    continue;
                }

                if (!GetCellWorldBounds(cell).Intersects(playerBounds))
                {
                    continue;
                }

                TryAddTouchedCell(cell, results);
            }
        }
    }

    void CollectTouchedCellsFromContacts(Collision2D collision, List<Vector3Int> results)
    {
        float sampleDistance = GetContactSampleDistance();
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            TryAddTouchedCell(contact.point, results);

            Vector2 normalOffset = contact.normal * sampleDistance;
            TryAddTouchedCell(contact.point + normalOffset, results);
            TryAddTouchedCell(contact.point - normalOffset, results);
        }
    }

    float GetContactSampleDistance()
    {
        Vector3 baseSize = tilemap.layoutGrid != null
            ? tilemap.layoutGrid.cellSize
            : Vector3.one;
        Vector3 scale = transform.lossyScale;
        float worldWidth = Mathf.Abs(baseSize.x * scale.x);
        float worldHeight = Mathf.Abs(baseSize.y * scale.y);
        float minWorldSize = Mathf.Max(0.01f, Mathf.Min(worldWidth, worldHeight));
        return minWorldSize * ContactSampleFactor;
    }

    void TryAddTouchedCell(Vector2 worldPoint, List<Vector3Int> results)
    {
        TryAddTouchedCell(tilemap.WorldToCell(worldPoint), results);
    }

    void TryAddTouchedCell(Vector3Int cell, List<Vector3Int> results)
    {
        if (!tilemap.HasTile(cell) || !touchedCellSet.Add(cell))
        {
            return;
        }

        results.Add(cell);
    }

    Bounds GetCellWorldBounds(Vector3Int cell)
    {
        Vector3 center = tilemap.GetCellCenterWorld(cell);
        Vector3 baseSize = tilemap.layoutGrid != null
            ? tilemap.layoutGrid.cellSize
            : Vector3.one;
        Vector3 scale = transform.lossyScale;
        Vector3 size = new Vector3(
            Mathf.Abs(baseSize.x * scale.x),
            Mathf.Abs(baseSize.y * scale.y),
            0.05f
        );
        return new Bounds(center, size);
    }

    List<Vector3Int> CollectConnectedCells(Vector3Int startCell)
    {
        List<Vector3Int> cells = new List<Vector3Int>();
        if (!tilemap.HasTile(startCell))
        {
            return cells;
        }

        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        queue.Enqueue(startCell);
        visited.Add(startCell);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            if (!tilemap.HasTile(current))
            {
                continue;
            }

            cells.Add(current);

            EnqueueNeighbor(current + Vector3Int.left, visited, queue);
            EnqueueNeighbor(current + Vector3Int.right, visited, queue);
            EnqueueNeighbor(current + Vector3Int.up, visited, queue);
            EnqueueNeighbor(current + Vector3Int.down, visited, queue);

            if (!includeDiagonalConnections)
            {
                continue;
            }

            EnqueueNeighbor(current + new Vector3Int(-1, -1, 0), visited, queue);
            EnqueueNeighbor(current + new Vector3Int(-1, 1, 0), visited, queue);
            EnqueueNeighbor(current + new Vector3Int(1, -1, 0), visited, queue);
            EnqueueNeighbor(current + new Vector3Int(1, 1, 0), visited, queue);
        }

        return cells;
    }

    void EnqueueNeighbor(Vector3Int cell, HashSet<Vector3Int> visited, Queue<Vector3Int> queue)
    {
        if (visited.Contains(cell) || !tilemap.HasTile(cell))
        {
            return;
        }

        visited.Add(cell);
        queue.Enqueue(cell);
    }

    bool HideConnectedCells(List<Vector3Int> cells)
    {
        if (cells == null || cells.Count == 0)
        {
            return false;
        }

        HiddenGroup hiddenGroup = hidePermanently ? null : new HiddenGroup();
        if (hiddenGroup != null)
        {
            hiddenGroup.restoreAt = float.PositiveInfinity;
            hiddenGroup.worldBounds = CreateBoundsFromCells(cells);
        }

        bool changedAnyTile = false;
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int cell = cells[i];
            TileBase tile = tilemap.GetTile(cell);
            if (tile == null)
            {
                continue;
            }

            if (hiddenGroup != null)
            {
                hiddenGroup.tiles[cell] = new HiddenTileState
                {
                    tile = tile,
                    color = tilemap.GetColor(cell),
                    transform = tilemap.GetTransformMatrix(cell),
                    flags = tilemap.GetTileFlags(cell)
                };
            }

            tilemap.SetTile(cell, null);
            changedAnyTile = true;
        }

        if (hiddenGroup != null && hiddenGroup.tiles.Count > 0)
        {
            hiddenGroups.Add(hiddenGroup);
        }

        return changedAnyTile;
    }

    void RestoreGroup(HiddenGroup group)
    {
        foreach (KeyValuePair<Vector3Int, HiddenTileState> entry in group.tiles)
        {
            Vector3Int cell = entry.Key;
            HiddenTileState state = entry.Value;
            tilemap.SetTile(cell, state.tile);
            tilemap.SetTileFlags(cell, TileFlags.None);
            tilemap.SetColor(cell, state.color);
            tilemap.SetTransformMatrix(cell, state.transform);
            tilemap.SetTileFlags(cell, state.flags);
        }

        RefreshTilemapCollider();
    }

    void RefreshTilemapCollider()
    {
        if (tilemapCollider != null)
        {
            tilemapCollider.ProcessTilemapChanges();
        }

        Physics2D.SyncTransforms();
    }

    void RegisterOverlappingPlayer(PlayerController player)
    {
        if (player == null || hidePermanently)
        {
            return;
        }

        overlappingPlayers.Add(player);
        CancelPendingRestore();
    }

    void QueueRestoreIfClear()
    {
        if (hidePermanently || overlappingPlayers.Count > 0 || hiddenGroups.Count == 0)
        {
            return;
        }

        float restoreAt = Time.time + reappearDelay;
        for (int i = 0; i < hiddenGroups.Count; i++)
        {
            HiddenGroup group = hiddenGroups[i];
            if (group == null)
            {
                continue;
            }

            group.restoreAt = restoreAt;
        }
    }

    void CancelPendingRestore()
    {
        if (hidePermanently || hiddenGroups.Count == 0)
        {
            return;
        }

        for (int i = 0; i < hiddenGroups.Count; i++)
        {
            HiddenGroup group = hiddenGroups[i];
            if (group == null)
            {
                continue;
            }

            group.restoreAt = float.PositiveInfinity;
        }
    }

    void PruneInvalidPlayers()
    {
        if (overlappingPlayers.Count == 0)
        {
            return;
        }

        overlappingPlayers.RemoveWhere(player => player == null || !player.isActiveAndEnabled);
        overlappingPlayers.RemoveWhere(player => !IsPlayerStillOverlappingHiddenArea(player));
        if (overlappingPlayers.Count == 0)
        {
            QueueRestoreIfClear();
        }
    }

    bool IsPlayerStillOverlappingHiddenArea(PlayerController player)
    {
        if (player == null || hiddenGroups.Count == 0)
        {
            return false;
        }

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        playerBounds.Expand(-0.02f);

        for (int i = 0; i < hiddenGroups.Count; i++)
        {
            HiddenGroup group = hiddenGroups[i];
            if (group == null || group.tiles.Count == 0)
            {
                continue;
            }

            if (group.worldBounds.Intersects(playerBounds))
            {
                return true;
            }
        }

        return false;
    }

    Bounds CreateBoundsFromCells(List<Vector3Int> cells)
    {
        if (cells == null || cells.Count == 0)
        {
            return new Bounds(transform.position, Vector3.zero);
        }

        Bounds bounds = GetCellWorldBounds(cells[0]);
        for (int i = 1; i < cells.Count; i++)
        {
            bounds.Encapsulate(GetCellWorldBounds(cells[i]));
        }

        return bounds;
    }
}
