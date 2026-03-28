using System.Collections.Generic;
using UnityEngine;

public class PlayerSessionManager : MonoBehaviour
{
    [System.Serializable]
    public class SessionPlayer
    {
        public PlayerController.ControlType slot;
        public GameInput.BindingId binding;
        public int prefabIndex;

        public SessionPlayer Clone()
        {
            return new SessionPlayer
            {
                slot = slot,
                binding = binding,
                prefabIndex = prefabIndex
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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
