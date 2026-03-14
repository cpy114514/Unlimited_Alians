using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelPortal : MonoBehaviour
{
    public string levelSceneName;
    public float countdownTime = 3f;
    public TextMeshProUGUI countdownText;

    [Header("Platform Press Effect")]
    public Transform platform;
    public float pressDepth = 0.2f;
    public float pressSpeed = 8f;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip pressSound;
    public AudioClip releaseSound;

    readonly HashSet<PlayerController> playersOnPortal =
        new HashSet<PlayerController>();

    float timer;

    Vector3 originalPos;
    Vector3 targetPos;

    void Start()
    {
        originalPos = platform.localPosition;
        targetPos = originalPos;
    }

    void Update()
    {
        // 只要有玩家就压下，不会因为玩家数量继续往下
        targetPos = playersOnPortal.Count > 0
            ? originalPos - new Vector3(0, pressDepth, 0)
            : originalPos;

        platform.localPosition = Vector3.Lerp(
            platform.localPosition,
            targetPos,
            Time.deltaTime * pressSpeed
        );

        int requiredPlayers = FindObjectsOfType<PlayerController>().Length;

        if (requiredPlayers == 0) return;

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
        if (player == null) return;

        bool wasEmpty = playersOnPortal.Count == 0;

        if (!playersOnPortal.Contains(player))
        {
            playersOnPortal.Add(player);

            if (wasEmpty && pressSound != null)
            {
                audioSource.PlayOneShot(pressSound);
            }

            Debug.Log(player.name + " entered portal. Count: " + playersOnPortal.Count);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.gameObject.activeInHierarchy) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (playersOnPortal.Contains(player))
        {
            playersOnPortal.Remove(player);

            if (playersOnPortal.Count == 0 && releaseSound != null)
            {
                audioSource.PlayOneShot(releaseSound);
            }

            Debug.Log(player.name + " left portal. Count: " + playersOnPortal.Count);
        }
    }
}