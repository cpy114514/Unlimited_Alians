using System.Collections.Generic;
using UnityEngine;

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
    }

    void Start()
    {
        InitializeSessionPlayers();
        SpawnMissingPlayersAtSpawns();
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
