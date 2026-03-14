using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardUI : MonoBehaviour
{
    public GameObject panel;

    public TextMeshProUGUI wasdText;
    public TextMeshProUGUI arrowText;
    public TextMeshProUGUI ijklText;

    [Header("Horizontal Bar")]
    public int targetScore = ScoreManager.WinningScore;
    public float rowSpacing = 96f;
    public float barWidth = 560f;
    public float barHeight = 44f;
    public float barAnimationDuration = 0.25f;

    static readonly PlayerController.ControlType[] fallbackOrder =
    {
        PlayerController.ControlType.WASD,
        PlayerController.ControlType.IJKL,
        PlayerController.ControlType.ArrowKeys
    };

    readonly Dictionary<PlayerController.ControlType, TextMeshProUGUI> labels =
        new Dictionary<PlayerController.ControlType, TextMeshProUGUI>();

    readonly Dictionary<PlayerController.ControlType, RectTransform> barBackgrounds =
        new Dictionary<PlayerController.ControlType, RectTransform>();

    readonly Dictionary<PlayerController.ControlType, RectTransform> barFills =
        new Dictionary<PlayerController.ControlType, RectTransform>();

    readonly Dictionary<PlayerController.ControlType, Image> fillImages =
        new Dictionary<PlayerController.ControlType, Image>();

    readonly Dictionary<PlayerController.ControlType, Image> backgroundImages =
        new Dictionary<PlayerController.ControlType, Image>();

    readonly Dictionary<PlayerController.ControlType, TextMeshProUGUI> scoreTexts =
        new Dictionary<PlayerController.ControlType, TextMeshProUGUI>();

    TextMeshProUGUI titleText;
    Coroutine animateRoutine;
    bool visualsBuilt;

    void Awake()
    {
        CacheLabels();
        EnsureVisualsBuilt();
        Hide();
    }

    public void ShowRoundResults(PlayerController.ControlType? winner, bool matchWon)
    {
        EnsureVisualsBuilt();

        if (panel != null)
        {
            panel.SetActive(true);
        }

        UpdateScores(winner, matchWon, false);
    }

    public void ShowNoWinnerResults()
    {
        EnsureVisualsBuilt();

        if (panel != null)
        {
            panel.SetActive(true);
        }

        UpdateScores(null, false, true);
    }

    public void Hide()
    {
        if (animateRoutine != null)
        {
            StopCoroutine(animateRoutine);
            animateRoutine = null;
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void UpdateScores()
    {
        UpdateScores(null, false, false);
    }

    void UpdateScores(
        PlayerController.ControlType? winner,
        bool matchWon,
        bool noWinner
    )
    {
        if (panel == null || ScoreManager.Instance == null)
        {
            return;
        }

        EnsureVisualsBuilt();

        List<PlayerController.ControlType> visiblePlayers = GetVisiblePlayers();
        if (visiblePlayers.Count == 0)
        {
            visiblePlayers.Add(PlayerController.ControlType.WASD);
        }

        LayoutChart(visiblePlayers, winner, matchWon, noWinner);

        if (animateRoutine != null)
        {
            StopCoroutine(animateRoutine);
        }

        animateRoutine = StartCoroutine(AnimateBars(visiblePlayers));
    }

    void CacheLabels()
    {
        labels.Clear();

        if (wasdText != null)
        {
            labels[PlayerController.ControlType.WASD] = wasdText;
        }

        if (ijklText != null)
        {
            labels[PlayerController.ControlType.IJKL] = ijklText;
        }

        if (arrowText != null)
        {
            labels[PlayerController.ControlType.ArrowKeys] = arrowText;
        }
    }

    void EnsureVisualsBuilt()
    {
        if (visualsBuilt || panel == null)
        {
            return;
        }

        CacheLabels();
        titleText = FindTitleText();

        foreach (KeyValuePair<PlayerController.ControlType, TextMeshProUGUI> entry in labels)
        {
            CreateBarVisuals(entry.Key, entry.Value);
        }

        visualsBuilt = true;
    }

    TextMeshProUGUI FindTitleText()
    {
        if (panel == null)
        {
            return null;
        }

        TextMeshProUGUI[] texts = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI candidate = null;

        foreach (TextMeshProUGUI text in texts)
        {
            if (text == wasdText || text == arrowText || text == ijklText)
            {
                continue;
            }

            if (candidate == null ||
                text.rectTransform.anchoredPosition.y > candidate.rectTransform.anchoredPosition.y)
            {
                candidate = text;
            }
        }

        return candidate;
    }

    void CreateBarVisuals(PlayerController.ControlType type, TextMeshProUGUI labelTemplate)
    {
        GameObject backgroundObject = new GameObject(type + "BarBackground", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(panel.transform, false);

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(0.11f, 0.11f, 0.14f, 0.94f);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();

        GameObject fillObject = new GameObject(type + "BarFill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(backgroundObject.transform, false);

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = GetPlayerColor(type);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 0f);

        CreateSegmentDividers(backgroundRect);

        GameObject scoreObject = new GameObject(type + "ScoreValue", typeof(RectTransform), typeof(TextMeshProUGUI));
        scoreObject.transform.SetParent(panel.transform, false);

        TextMeshProUGUI scoreText = scoreObject.GetComponent<TextMeshProUGUI>();
        scoreText.font = labelTemplate.font;
        scoreText.fontSharedMaterial = labelTemplate.fontSharedMaterial;
        scoreText.fontSize = 28f;
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.color = Color.white;
        scoreText.text = "0/6";

        barBackgrounds[type] = backgroundRect;
        barFills[type] = fillRect;
        fillImages[type] = fillImage;
        backgroundImages[type] = backgroundImage;
        scoreTexts[type] = scoreText;
    }

    void CreateSegmentDividers(RectTransform backgroundRect)
    {
        for (int i = 1; i < targetScore; i++)
        {
            GameObject dividerObject = new GameObject("Divider" + i, typeof(RectTransform), typeof(Image));
            dividerObject.transform.SetParent(backgroundRect, false);

            Image dividerImage = dividerObject.GetComponent<Image>();
            dividerImage.color = new Color(1f, 1f, 1f, 0.18f);

            RectTransform dividerRect = dividerObject.GetComponent<RectTransform>();
            dividerRect.anchorMin = new Vector2(0f, 0f);
            dividerRect.anchorMax = new Vector2(0f, 1f);
            dividerRect.pivot = new Vector2(0.5f, 0.5f);
            dividerRect.anchoredPosition = new Vector2(barWidth * i / targetScore, 0f);
            dividerRect.sizeDelta = new Vector2(4f, 0f);
        }
    }

    List<PlayerController.ControlType> GetVisiblePlayers()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.GetSessionPlayers();
        }

        if (PlayerSessionManager.Instance != null &&
            PlayerSessionManager.Instance.activePlayers.Count > 0)
        {
            List<PlayerController.ControlType> players =
                new List<PlayerController.ControlType>();

            foreach (PlayerController.ControlType type in fallbackOrder)
            {
                if (PlayerSessionManager.Instance.activePlayers.Contains(type))
                {
                    players.Add(type);
                }
            }

            return players;
        }

        return new List<PlayerController.ControlType>(fallbackOrder);
    }

    void LayoutChart(
        List<PlayerController.ControlType> visiblePlayers,
        PlayerController.ControlType? winner,
        bool matchWon,
        bool noWinner
    )
    {
        HashSet<PlayerController.ControlType> visibleSet =
            new HashSet<PlayerController.ControlType>(visiblePlayers);

        foreach (PlayerController.ControlType type in fallbackOrder)
        {
            if (!labels.ContainsKey(type))
            {
                continue;
            }

            bool isVisible = visibleSet.Contains(type);

            labels[type].gameObject.SetActive(isVisible);
            barBackgrounds[type].gameObject.SetActive(isVisible);
            scoreTexts[type].gameObject.SetActive(isVisible);

            if (!isVisible)
            {
                continue;
            }

            int index = visiblePlayers.IndexOf(type);
            float yPosition = GetRowY(index, visiblePlayers.Count);
            bool isWinner = winner.HasValue && winner.Value == type;

            ConfigureLabel(labels[type], type, yPosition);
            ConfigureBar(type, yPosition, isWinner);
            ConfigureScoreText(type, yPosition);
        }

        if (titleText != null)
        {
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 42f;
            titleText.text = GetTitleText(winner, matchWon, noWinner);
        }
    }

    float GetRowY(int index, int count)
    {
        float startY = (count - 1) * rowSpacing * 0.5f;
        return startY - index * rowSpacing - 20f;
    }

    void ConfigureLabel(TextMeshProUGUI label, PlayerController.ControlType type, float yPosition)
    {
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(220f, 44f);
        rect.anchoredPosition = new Vector2(-460f, yPosition);

        label.alignment = TextAlignmentOptions.Left;
        label.fontSize = 28f;
        label.color = GetPlayerColor(type);
        label.text = GetDisplayName(type);
    }

    void ConfigureBar(PlayerController.ControlType type, float yPosition, bool isWinner)
    {
        RectTransform backgroundRect = barBackgrounds[type];
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(barWidth, barHeight);
        backgroundRect.anchoredPosition = new Vector2(-210f, yPosition);
        backgroundRect.localScale = isWinner ? Vector3.one * 1.03f : Vector3.one;

        backgroundImages[type].color = isWinner
            ? new Color(0.16f, 0.16f, 0.2f, 0.98f)
            : new Color(0.11f, 0.11f, 0.14f, 0.94f);

        fillImages[type].color = isWinner
            ? Color.Lerp(GetPlayerColor(type), Color.white, 0.18f)
            : GetPlayerColor(type);
    }

    void ConfigureScoreText(PlayerController.ControlType type, float yPosition)
    {
        RectTransform rect = scoreTexts[type].rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(120f, 44f);
        rect.anchoredPosition = new Vector2(395f, yPosition);
    }

    IEnumerator AnimateBars(List<PlayerController.ControlType> visiblePlayers)
    {
        Dictionary<PlayerController.ControlType, float> startWidths =
            new Dictionary<PlayerController.ControlType, float>();

        Dictionary<PlayerController.ControlType, float> targetWidths =
            new Dictionary<PlayerController.ControlType, float>();

        foreach (PlayerController.ControlType type in visiblePlayers)
        {
            float currentWidth = barFills[type].sizeDelta.x;
            int score = Mathf.Clamp(ScoreManager.Instance.scores[type], 0, targetScore);
            float targetWidth = barWidth * score / Mathf.Max(1f, targetScore);

            startWidths[type] = currentWidth;
            targetWidths[type] = targetWidth;
            scoreTexts[type].text = score + "/" + targetScore;
        }

        float elapsed = 0f;

        while (elapsed < barAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / barAnimationDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            foreach (PlayerController.ControlType type in visiblePlayers)
            {
                float width = Mathf.Lerp(startWidths[type], targetWidths[type], eased);
                RectTransform fillRect = barFills[type];
                fillRect.sizeDelta = new Vector2(width, fillRect.sizeDelta.y);
            }

            yield return null;
        }

        foreach (PlayerController.ControlType type in visiblePlayers)
        {
            RectTransform fillRect = barFills[type];
            fillRect.sizeDelta = new Vector2(targetWidths[type], fillRect.sizeDelta.y);
        }

        animateRoutine = null;
    }

    string GetTitleText(
        PlayerController.ControlType? winner,
        bool matchWon,
        bool noWinner
    )
    {
        if (noWinner)
        {
            return "NO PLAYER WINS\nNO POINTS AWARDED";
        }

        if (!winner.HasValue)
        {
            return "SCOREBOARD";
        }

        return matchWon
            ? GetDisplayName(winner.Value) + " WINS THE MATCH!"
            : GetDisplayName(winner.Value) + " WINS THE ROUND!";
    }

    string GetDisplayName(PlayerController.ControlType type)
    {
        switch (type)
        {
            case PlayerController.ControlType.WASD:
                return "P1";
            case PlayerController.ControlType.IJKL:
                return "P2";
            case PlayerController.ControlType.ArrowKeys:
                return "P3";
        }

        return type.ToString();
    }

    Color GetPlayerColor(PlayerController.ControlType type)
    {
        switch (type)
        {
            case PlayerController.ControlType.WASD:
                return new Color(1f, 0.78f, 0.2f);
            case PlayerController.ControlType.IJKL:
                return new Color(1f, 0.4f, 0.52f);
            case PlayerController.ControlType.ArrowKeys:
                return new Color(0.3f, 0.8f, 1f);
        }

        return Color.white;
    }
}
