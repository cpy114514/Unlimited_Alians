using System.Collections.Generic;
using UnityEngine;

public class PlayerSessionManager : MonoBehaviour
{
    static bool creatingDedicatedInstance;

    [System.Serializable]
    public class SessionPlayer
    {
        public PlayerController.ControlType slot;
        public GameInput.BindingId binding;
        public int prefabIndex;
        public string displayName;
        public Color uiColor = Color.white;
        public Sprite idleSprite;
        public Sprite runSpriteA;
        public Sprite runSpriteB;

        public SessionPlayer Clone()
        {
            return new SessionPlayer
            {
                slot = slot,
                binding = binding,
                prefabIndex = prefabIndex,
                displayName = displayName,
                uiColor = uiColor,
                idleSprite = idleSprite,
                runSpriteA = runSpriteA,
                runSpriteB = runSpriteB
            };
        }
    }

    public static PlayerSessionManager Instance;

    public List<PlayerController.ControlType> activePlayers =
        new List<PlayerController.ControlType>();

    public List<SessionPlayer> joinedPlayers =
        new List<SessionPlayer>();

    void Awake()
    {
        if (creatingDedicatedInstance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        if (Instance == null)
        {
            if (NeedsDedicatedPersistentObject())
            {
                PromoteToDedicatedPersistentObject();
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    bool NeedsDedicatedPersistentObject()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && behaviours[i] != this)
            {
                return true;
            }
        }

        return false;
    }

    void PromoteToDedicatedPersistentObject()
    {
        List<SessionPlayer> preservedPlayers = GetSessionPlayersCopy();
        List<PlayerController.ControlType> preservedActivePlayers =
            new List<PlayerController.ControlType>(activePlayers);

        creatingDedicatedInstance = true;
        GameObject persistentObject = new GameObject("PlayerSessionManager");
        PlayerSessionManager persistentManager =
            persistentObject.AddComponent<PlayerSessionManager>();
        creatingDedicatedInstance = false;

        if (preservedPlayers.Count > 0)
        {
            persistentManager.SetSessionPlayers(preservedPlayers);
            return;
        }

        persistentManager.activePlayers.Clear();
        persistentManager.activePlayers.AddRange(preservedActivePlayers);
    }

    public void SetSessionPlayers(IEnumerable<SessionPlayer> players)
    {
        joinedPlayers.Clear();
        activePlayers.Clear();

        if (players == null)
        {
            return;
        }

        foreach (SessionPlayer player in players)
        {
            if (player == null)
            {
                continue;
            }

            SessionPlayer clone = player.Clone();
            joinedPlayers.Add(clone);
            activePlayers.Add(clone.slot);
        }
    }

    public void ClearSession()
    {
        joinedPlayers.Clear();
        activePlayers.Clear();
    }

    public List<SessionPlayer> GetSessionPlayersCopy()
    {
        List<SessionPlayer> copy = new List<SessionPlayer>();

        foreach (SessionPlayer player in joinedPlayers)
        {
            if (player != null)
            {
                copy.Add(player.Clone());
            }
        }

        return copy;
    }
}
