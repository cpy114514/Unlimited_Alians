using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    static readonly PlayerController.ControlType[] preferredPlayerOrder =
    {
        PlayerController.ControlType.WASD,
        PlayerController.ControlType.IJKL,
        PlayerController.ControlType.ArrowKeys
    };

    public GameObject wasdPrefab;
    public GameObject arrowPrefab;
    public GameObject ijklPrefab;

    public Transform[] spawnPoints;

    [Header("Build Phase")]
    public Vector2Int buildMinPlacementCell = new Vector2Int(-4, -10);
    public Vector2Int buildMaxPlacementCell = new Vector2Int(39, 8);
    public Vector2 buildExclusionHalfExtents = new Vector2(1.5f, 1.5f);

    readonly Dictionary<PlayerController.ControlType, PlayerController> playersByType =
        new Dictionary<PlayerController.ControlType, PlayerController>();

    readonly List<PlayerController.ControlType> sessionPlayers =
        new List<PlayerController.ControlType>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (GetComponent<BuildPhaseManager>() == null)
        {
            gameObject.AddComponent<BuildPhaseManager>();
        }
    }

    void Start()
    {
        InitializeSessionPlayers();
        SpawnMissingPlayersAtSpawns();

        if (BuildPhaseManager.Instance != null)
        {
            BuildPhaseManager.Instance.BeginRoundSetup(false);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void InitializeSessionPlayers()
    {
        sessionPlayers.Clear();

        List<PlayerController.ControlType> requestedPlayers =
            new List<PlayerController.ControlType>();

        if (PlayerSessionManager.Instance != null &&
            PlayerSessionManager.Instance.activePlayers.Count > 0)
        {
            requestedPlayers.AddRange(PlayerSessionManager.Instance.activePlayers);
        }
        else
        {
            requestedPlayers.Add(PlayerController.ControlType.WASD);
            Debug.LogWarning("No active lobby session found. Defaulting to a single WASD player.");
        }

        foreach (PlayerController.ControlType controlType in preferredPlayerOrder)
        {
            if (requestedPlayers.Contains(controlType))
            {
                sessionPlayers.Add(controlType);
            }
        }

        if (sessionPlayers.Count > spawnPoints.Length)
        {
            Debug.LogWarning("Not enough spawn points for all joined players.");
        }
    }

    void SpawnMissingPlayersAtSpawns()
    {
        int playerCount = Mathf.Min(sessionPlayers.Count, spawnPoints.Length);

        for (int i = 0; i < playerCount; i++)
        {
            PlayerController.ControlType controlType = sessionPlayers[i];

            if (playersByType.TryGetValue(controlType, out PlayerController existingPlayer) &&
                existingPlayer != null)
            {
                existingPlayer.ResetForNextRound(spawnPoints[i].position);
                continue;
            }

            SpawnPlayer(controlType, spawnPoints[i].position);
        }
    }

    PlayerController SpawnPlayer(PlayerController.ControlType type, Vector3 position)
    {
        GameObject prefab = GetPrefab(type);

        if (prefab == null)
        {
            Debug.LogError("Missing prefab for " + type);
            return null;
        }

        GameObject playerObject = Instantiate(prefab, position, Quaternion.identity);
        PlayerController player = playerObject.GetComponent<PlayerController>();

        if (player == null)
        {
            Debug.LogError("Spawned prefab does not have a PlayerController: " + prefab.name);
            Destroy(playerObject);
            return null;
        }

        player.controlType = type;
        player.ResetForNextRound(position);
        playersByType[type] = player;

        return player;
    }

    public void SetAllPlayerControl(bool enabled)
    {
        foreach (PlayerController.ControlType controlType in sessionPlayers)
        {
            if (!playersByType.TryGetValue(controlType, out PlayerController player) || player == null)
            {
                continue;
            }

            player.SetControlEnabled(enabled);
        }
    }

    public void MarkPlayerDead(PlayerController.ControlType player)
    {
        playersByType[player] = null;
    }

    public int GetSessionPlayerCount()
    {
        return sessionPlayers.Count;
    }

    public bool TryGetPlayer(
        PlayerController.ControlType type,
        out PlayerController player
    )
    {
        return playersByType.TryGetValue(type, out player) && player != null;
    }

    public int GetAlivePlayerCount()
    {
        int alivePlayers = 0;

        foreach (PlayerController.ControlType controlType in sessionPlayers)
        {
            if (playersByType.TryGetValue(controlType, out PlayerController player) && player != null)
            {
                alivePlayers++;
            }
        }

        return alivePlayers;
    }

    public void ResetRound()
    {
        SpawnMissingPlayersAtSpawns();
    }

    public List<PlayerController.ControlType> GetSessionPlayers()
    {
        return new List<PlayerController.ControlType>(sessionPlayers);
    }

    public void GetBuildPlacementConfig(
        out Vector2Int minPlacementCell,
        out Vector2Int maxPlacementCell,
        out Vector2 exclusionHalfExtents
    )
    {
        minPlacementCell = buildMinPlacementCell;
        maxPlacementCell = buildMaxPlacementCell;
        exclusionHalfExtents = buildExclusionHalfExtents;
    }

    void OnDrawGizmosSelected()
    {
        DrawBuildPlacementBoundsGizmo();
        DrawProtectedZoneGizmos();
    }

    void DrawBuildPlacementBoundsGizmo()
    {
        Vector2Int minCell = new Vector2Int(
            Mathf.Min(buildMinPlacementCell.x, buildMaxPlacementCell.x),
            Mathf.Min(buildMinPlacementCell.y, buildMaxPlacementCell.y)
        );
        Vector2Int maxCell = new Vector2Int(
            Mathf.Max(buildMinPlacementCell.x, buildMaxPlacementCell.x),
            Mathf.Max(buildMinPlacementCell.y, buildMaxPlacementCell.y)
        );

        Vector3 size = new Vector3(
            maxCell.x - minCell.x + 1f,
            maxCell.y - minCell.y + 1f,
            0f
        );
        Vector3 center = new Vector3(
            minCell.x + size.x * 0.5f,
            minCell.y + size.y * 0.5f,
            0f
        );

        Gizmos.color = new Color(0.18f, 0.95f, 0.88f, 0.2f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(0.18f, 0.95f, 0.88f, 0.95f);
        Gizmos.DrawWireCube(center, size);

#if UNITY_EDITOR
        Handles.color = new Color(0.18f, 0.95f, 0.88f, 0.95f);
        Handles.Label(
            center + new Vector3(0f, size.y * 0.5f + 0.35f, 0f),
            "Build Range"
        );
#endif
    }

    void DrawProtectedZoneGizmos()
    {
        Vector3 zoneSize = new Vector3(
            Mathf.Max(0f, buildExclusionHalfExtents.x) * 2f,
            Mathf.Max(0f, buildExclusionHalfExtents.y) * 2f,
            0f
        );

        if (zoneSize.x <= 0f || zoneSize.y <= 0f)
        {
            return;
        }

        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null)
                {
                    continue;
                }

                DrawProtectedZone(spawnPoints[i].position, zoneSize, "Spawn Block");
            }
        }

        FinishFlag finishFlag = FindObjectOfType<FinishFlag>(true);
        if (finishFlag != null)
        {
            DrawProtectedZone(finishFlag.transform.position, zoneSize, "Finish Block");
        }
    }

    void DrawProtectedZone(Vector3 center, Vector3 size, string label)
    {
        Gizmos.color = new Color(1f, 0.42f, 0.24f, 0.18f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(1f, 0.42f, 0.24f, 0.95f);
        Gizmos.DrawWireCube(center, size);

#if UNITY_EDITOR
        Handles.color = new Color(1f, 0.42f, 0.24f, 0.95f);
        Handles.Label(
            center + new Vector3(0f, size.y * 0.5f + 0.18f, 0f),
            label
        );
#endif
    }

    GameObject GetPrefab(PlayerController.ControlType type)
    {
        switch (type)
        {
            case PlayerController.ControlType.WASD:
                return wasdPrefab;
            case PlayerController.ControlType.ArrowKeys:
                return arrowPrefab;
            case PlayerController.ControlType.IJKL:
                return ijklPrefab;
        }

        return null;
    }
}
