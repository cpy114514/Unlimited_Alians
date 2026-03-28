using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    static readonly PlayerController.ControlType[] slotOrder =
    {
        PlayerController.ControlType.WASD,
        PlayerController.ControlType.IJKL,
        PlayerController.ControlType.ArrowKeys,
        PlayerController.ControlType.Slot4,
        PlayerController.ControlType.Slot5,
        PlayerController.ControlType.Slot6
    };

    [Header("Base Player")]
    public PlayerRosterConfig sharedPlayerRosterConfig;
    public GameObject playerPrefab;

    [Header("Player Avatars")]
    public List<PlayerAvatarDefinition> playerAvatars = new List<PlayerAvatarDefinition>();

    public Transform[] spawnPoints;
    public float holdDuration = 1.2f;

    readonly Dictionary<PlayerController.ControlType, GameObject> players =
        new Dictionary<PlayerController.ControlType, GameObject>();

    readonly Dictionary<GameInput.BindingId, PlayerController.ControlType> joinedBindings =
        new Dictionary<GameInput.BindingId, PlayerController.ControlType>();

    readonly Dictionary<GameInput.BindingId, float> holdTimers =
        new Dictionary<GameInput.BindingId, float>();

    void Update()
    {
        HandleJoin();
        HandleHoldLeave();
        SyncSessionData();
    }

    void HandleJoin()
    {
        foreach (GameInput.BindingId binding in GameInput.JoinBindings)
        {
            if (!GameInput.GetLobbyJoinPressed(binding) || joinedBindings.ContainsKey(binding))
            {
                continue;
            }

            TryJoin(binding);
        }
    }

    void HandleHoldLeave()
    {
        List<GameInput.BindingId> bindings = new List<GameInput.BindingId>(joinedBindings.Keys);

        foreach (GameInput.BindingId binding in bindings)
        {
            if (!joinedBindings.TryGetValue(binding, out PlayerController.ControlType slot) ||
                !players.ContainsKey(slot))
            {
                continue;
            }

            if (GameInput.GetRotateHeld(binding))
            {
                if (!holdTimers.ContainsKey(binding))
                {
                    holdTimers[binding] = 0f;
                }

                holdTimers[binding] += Time.deltaTime;

                if (holdTimers[binding] >= holdDuration)
                {
                    Destroy(players[slot]);
                    players.Remove(slot);
                    joinedBindings.Remove(binding);
                    holdTimers.Remove(binding);
                }
            }
            else if (holdTimers.ContainsKey(binding))
            {
                holdTimers[binding] = 0f;
            }
        }
    }

    void TryJoin(GameInput.BindingId binding)
    {
        PlayerController.ControlType? nextSlot = GetNextFreeSlot();
        if (!nextSlot.HasValue)
        {
            return;
        }

        int slotIndex = GetSlotIndex(nextSlot.Value);
        if (slotIndex < 0 || slotIndex >= spawnPoints.Length)
        {
            return;
        }

        GameObject prefab = GetPrefab(slotIndex, nextSlot.Value);
        if (prefab == null)
        {
            Debug.LogError("LobbyManager: missing player avatar prefab for slot index " + slotIndex);
            return;
        }

        GameObject playerObject = Instantiate(prefab, spawnPoints[slotIndex].position, Quaternion.identity);
        PlayerController controller = playerObject.GetComponent<PlayerController>();
        if (controller == null)
        {
            Debug.LogError("LobbyManager: spawned prefab does not contain PlayerController.");
            Destroy(playerObject);
            return;
        }

        controller.controlType = nextSlot.Value;
        controller.inputBinding = binding;
        controller.playerPrefabIndex = slotIndex;
        ApplyAvatarDefinition(controller, slotIndex);

        players[nextSlot.Value] = playerObject;
        joinedBindings[binding] = nextSlot.Value;
    }

    void SyncSessionData()
    {
        if (PlayerSessionManager.Instance == null)
        {
            return;
        }

        List<PlayerSessionManager.SessionPlayer> sessionPlayers =
            new List<PlayerSessionManager.SessionPlayer>();

        foreach (PlayerController.ControlType slot in slotOrder)
        {
            if (!players.TryGetValue(slot, out GameObject playerObject) || playerObject == null)
            {
                continue;
            }

            PlayerController controller = playerObject.GetComponent<PlayerController>();
            if (controller == null)
            {
                continue;
            }

            sessionPlayers.Add(new PlayerSessionManager.SessionPlayer
            {
                slot = slot,
                binding = controller.inputBinding,
                prefabIndex = controller.playerPrefabIndex
            });
        }

        PlayerSessionManager.Instance.SetSessionPlayers(sessionPlayers);
    }

    PlayerController.ControlType? GetNextFreeSlot()
    {
        int maxSlots = Mathf.Min(spawnPoints != null ? spawnPoints.Length : 0, slotOrder.Length);

        for (int i = 0; i < maxSlots; i++)
        {
            if (!players.ContainsKey(slotOrder[i]))
            {
                return slotOrder[i];
            }
        }

        return null;
    }

    int GetSlotIndex(PlayerController.ControlType slot)
    {
        for (int i = 0; i < slotOrder.Length; i++)
        {
            if (slotOrder[i] == slot)
            {
                return i;
            }
        }

        return -1;
    }

    GameObject GetPrefab(int prefabIndex, PlayerController.ControlType slot)
    {
        if (sharedPlayerRosterConfig != null && sharedPlayerRosterConfig.playerPrefab != null)
        {
            return sharedPlayerRosterConfig.playerPrefab;
        }

        if (playerPrefab != null)
        {
            return playerPrefab;
        }

        return null;
    }

    void ApplyAvatarDefinition(PlayerController controller, int prefabIndex)
    {
        if (controller == null)
        {
            return;
        }

        List<PlayerAvatarDefinition> avatars = sharedPlayerRosterConfig != null
            ? sharedPlayerRosterConfig.playerAvatars
            : playerAvatars;

        if (avatars == null ||
            prefabIndex < 0 ||
            prefabIndex >= avatars.Count)
        {
            return;
        }

        PlayerAvatarDefinition avatar = avatars[prefabIndex];
        if (avatar == null)
        {
            return;
        }

        controller.ApplyAvatarAnimation(
            avatar.idleSprite,
            avatar.runSpriteA,
            avatar.runSpriteB
        );
    }
}
