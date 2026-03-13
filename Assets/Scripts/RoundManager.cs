using System.Collections;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    public float nextRoundDelay = 3f;

    bool roundEnding;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }

    public void PlayerWin(PlayerController.ControlType player)
    {
        if (roundEnding)
        {
            return;
        }

        roundEnding = true;

        bool matchWon = false;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(player);
            matchWon = ScoreManager.Instance.GetScore(player) >= ScoreManager.WinningScore;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetAllPlayerControl(false);
        }

        Time.timeScale = 0f;

        ScoreboardUI board = FindObjectOfType<ScoreboardUI>();
        if (board != null)
        {
            board.ShowRoundResults(player, matchWon);
        }

        StartCoroutine(NextRound(matchWon));
    }

    IEnumerator NextRound(bool matchWon)
    {
        yield return new WaitForSecondsRealtime(nextRoundDelay);

        ScoreboardUI board = FindObjectOfType<ScoreboardUI>();
        if (board != null)
        {
            board.Hide();
        }

        if (matchWon)
        {
            ScoreManager.ResetScores();
        }

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRound();
        }

        roundEnding = false;
    }
}
