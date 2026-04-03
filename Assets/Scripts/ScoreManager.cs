using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public const float WinningScore = 6f;

    public static ScoreManager Instance;

    static readonly Dictionary<PlayerController.ControlType, float> persistedScores =
        new Dictionary<PlayerController.ControlType, float>();

    public Dictionary<PlayerController.ControlType, float> scores =
        new Dictionary<PlayerController.ControlType, float>();

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

    public void AddScore(PlayerController.ControlType player, float amount)
    {
        EnsureScores();
        scores[player] += amount;
    }

    public void SetScore(PlayerController.ControlType player, float amount)
    {
        EnsureScores();
        scores[player] = amount;
    }

    public float GetScore(PlayerController.ControlType player)
    {
        EnsureScores();
        return scores[player];
    }

    public bool TryGetMatchWinner(
        IEnumerable<PlayerController.ControlType> priorityOrder,
        out PlayerController.ControlType winner
    )
    {
        winner = default;

        if (!TryGetMatchLeaders(priorityOrder, out List<PlayerController.ControlType> leaders, out _))
        {
            return false;
        }

        winner = leaders[0];
        return true;
    }

    public bool TryGetMatchLeaders(
        IEnumerable<PlayerController.ControlType> priorityOrder,
        out List<PlayerController.ControlType> leaders,
        out float bestScore
    )
    {
        EnsureScores();

        leaders = new List<PlayerController.ControlType>();
        bestScore = WinningScore;

        foreach (PlayerController.ControlType type in EnumeratePriorityOrder(priorityOrder))
        {
            float score = scores[type];

            if (score + 0.0001f < WinningScore)
            {
                continue;
            }

            if (leaders.Count == 0 || score > bestScore + 0.0001f)
            {
                leaders.Clear();
                leaders.Add(type);
                bestScore = score;
                continue;
            }

            if (Mathf.Abs(score - bestScore) <= 0.0001f)
            {
                leaders.Add(type);
            }
        }

        return leaders.Count > 0;
    }

    public static void ResetScores()
    {
        EnsureScores();

        foreach (PlayerController.ControlType type in
                System.Enum.GetValues(typeof(PlayerController.ControlType)))
        {
            persistedScores[type] = 0f;
        }
    }

    static void EnsureScores()
    {
        foreach (PlayerController.ControlType type in
                System.Enum.GetValues(typeof(PlayerController.ControlType)))
        {
            if (!persistedScores.ContainsKey(type))
            {
                persistedScores[type] = 0f;
            }
        }
    }

    static IEnumerable<PlayerController.ControlType> EnumeratePriorityOrder(
        IEnumerable<PlayerController.ControlType> priorityOrder
    )
    {
        HashSet<PlayerController.ControlType> seen =
            new HashSet<PlayerController.ControlType>();

        if (priorityOrder != null)
        {
            foreach (PlayerController.ControlType type in priorityOrder)
            {
                if (seen.Add(type))
                {
                    yield return type;
                }
            }
        }

        foreach (PlayerController.ControlType type in
                 System.Enum.GetValues(typeof(PlayerController.ControlType)))
        {
            if (seen.Add(type))
            {
                yield return type;
            }
        }
    }
}
