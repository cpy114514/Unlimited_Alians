using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SharedPlayerRosterConfig", menuName = "Game/Player Roster Config")]
public class PlayerRosterConfig : ScriptableObject
{
    public GameObject playerPrefab;
    public List<PlayerAvatarDefinition> playerAvatars = new List<PlayerAvatarDefinition>();

    public PlayerAvatarDefinition GetAvatarDefinition(int index)
    {
        if (index < 0 || index >= playerAvatars.Count)
        {
            return null;
        }

        return playerAvatars[index];
    }
}
