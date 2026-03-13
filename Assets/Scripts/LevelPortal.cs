using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelPortal : MonoBehaviour
{
    public string levelSceneName;
    public float countdownTime = 3f;
    public TextMeshProUGUI countdownText;

    readonly HashSet<PlayerController> playersOnPortal =
        new HashSet<PlayerController>();

    float timer;

    void Update()
    {
        int requiredPlayers = FindObjectsOfType<PlayerController>().Length;

        if (requiredPlayers == 0)
        {
            return;
        }

        if (playersOnPortal.Count >= requiredPlayers)
        {
            timer += Time.deltaTime;

            float timeLeft = countdownTime - timer;
            countdownText.text = Mathf.Ceil(timeLeft).ToString();

            if (timer >= countdownTime)
            {
                var currentPlayers = FindObjectsOfType<PlayerController>();

                PlayerSessionManager.Instance.activePlayers.Clear();

                foreach (PlayerController player in currentPlayers)
                {
                    PlayerSessionManager.Instance.activePlayers.Add(player.controlType);
                }

                if (SceneManager.GetActiveScene().name == "Lobby")
                {
                    ScoreManager.ResetScores();
                }

                SceneManager.LoadScene(levelSceneName);
            }
        }
        else
        {
            timer = 0f;
            countdownText.text = "";
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            return;
        }

        if (!playersOnPortal.Contains(player))
        {
            playersOnPortal.Add(player);
            Debug.Log(player.name + " entered portal. Count: " + playersOnPortal.Count);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            return;
        }

        if (playersOnPortal.Contains(player))
        {
            playersOnPortal.Remove(player);
            Debug.Log(player.name + " left portal. Count: " + playersOnPortal.Count);
        }
    }
}
