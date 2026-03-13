using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public const int WinningScore = 6;

    public static ScoreManager Instance;

    static readonly Dictionary<PlayerController.ControlType, int> persistedScores =
        new Dictionary<PlayerController.ControlType, int>();

    public Dictionary<PlayerController.ControlType, int> scores =
        new Dictionary<PlayerController.ControlType, int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureScores();
        scores = persistedScores;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddScore(PlayerController.ControlType player)
    {
        EnsureScores();
        scores[player]++;
    }

    public int GetScore(PlayerController.ControlType player)
    {
        EnsureScores();
        return scores[player];
    }

    public static void ResetScores()
    {
        EnsureScores();

        foreach (PlayerController.ControlType type in
                 System.Enum.GetValues(typeof(PlayerController.ControlType)))
        {
            persistedScores[type] = 0;
        }
    }

    static void EnsureScores()
    {
        foreach (PlayerController.ControlType type in
                 System.Enum.GetValues(typeof(PlayerController.ControlType)))
        {
            if (!persistedScores.ContainsKey(type))
            {
                persistedScores[type] = 0;
            }
        }
    }
}
