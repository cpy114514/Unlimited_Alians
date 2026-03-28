using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelPortal : MonoBehaviour
{
    static LevelPortal countdownOwner;

    public string levelSceneName;
    public float countdownTime = 3f;
    public TextMeshProUGUI countdownText;
    public Image countdownBackground;

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
        HideCountdown();
    }

    void Update()
    {
        targetPos = playersOnPortal.Count > 0
            ? originalPos - new Vector3(0, pressDepth, 0)
            : originalPos;

        platform.localPosition = Vector3.Lerp(
            platform.localPosition,
            targetPos,
            Time.deltaTime * pressSpeed
        );

        int requiredPlayers = FindObjectsOfType<PlayerController>().Length;

        if (requiredPlayers == 0)
        {
            timer = 0f;
            HideCountdown();
            return;
        }

        if (playersOnPortal.Count >= requiredPlayers)
        {
            timer += Time.deltaTime;

            float timeLeft = countdownTime - timer;
            ShowCountdown(Mathf.Ceil(timeLeft).ToString());

            if (timer >= countdownTime)
            {
                PlayerController[] currentPlayers = FindObjectsOfType<PlayerController>();
                List<PlayerSessionManager.SessionPlayer> sessionPlayers =
                    new List<PlayerSessionManager.SessionPlayer>();

                foreach (PlayerController.ControlType slot in new[]
                {
                    PlayerController.ControlType.WASD,
                    PlayerController.ControlType.IJKL,
                    PlayerController.ControlType.ArrowKeys,
                    PlayerController.ControlType.Slot4,
                    PlayerController.ControlType.Slot5,
                    PlayerController.ControlType.Slot6
                })
                {
                    foreach (PlayerController player in currentPlayers)
                    {
                        if (player == null || player.controlType != slot)
                        {
                            continue;
                        }

                        sessionPlayers.Add(new PlayerSessionManager.SessionPlayer
                        {
                            slot = player.controlType,
                            binding = player.inputBinding,
                            prefabIndex = player.playerPrefabIndex
                        });
                    }
                }

                if (PlayerSessionManager.Instance != null)
                {
                    PlayerSessionManager.Instance.SetSessionPlayers(sessionPlayers);
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
            HideCountdown();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

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
        if (!other.gameObject.activeInHierarchy)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

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

    void ShowCountdown(string value)
    {
        countdownOwner = this;

        if (countdownText != null)
        {
            countdownText.text = value;
            countdownText.gameObject.SetActive(true);
        }

        if (countdownBackground != null)
        {
            countdownBackground.gameObject.SetActive(true);
        }
    }

    void HideCountdown()
    {
        if (countdownOwner != null && countdownOwner != this)
        {
            return;
        }

        if (countdownOwner == this)
        {
            countdownOwner = null;
        }

        if (countdownText != null)
        {
            countdownText.text = string.Empty;
            countdownText.gameObject.SetActive(false);
        }

        if (countdownBackground != null)
        {
            countdownBackground.gameObject.SetActive(false);
        }
    }
}
