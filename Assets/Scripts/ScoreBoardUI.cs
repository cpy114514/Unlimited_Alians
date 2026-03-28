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
    public int targetScore = 6;
    public float rowSpacing = 96f;
    public float barWidth = 560f;
    public float barHeight = 44f;
    public float barAnimationDuration = 0.25f;

    static readonly PlayerController.ControlType[] fallbackOrder =
    {
        PlayerController.ControlType.WASD,
        PlayerController.ControlType.IJKL,
        PlayerController.ControlType.ArrowKeys,
        PlayerController.ControlType.Slot4,
        PlayerController.ControlType.Slot5,
        PlayerController.ControlType.Slot6
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
        List<PlayerController.ControlType> highlightedPlayers =
            new List<PlayerController.ControlType>();

        if (winner.HasValue)
        {
            highlightedPlayers.Add(winner.Value);
        }

        ShowResults(highlightedPlayers, GetRaceTitleText(winner, matchWon, false));
    }

    public void ShowNoWinnerResults()
    {
        ShowResults(
            new List<PlayerController.ControlType>(),
            GetRaceTitleText(null, false, true)
        );
    }

    public void ShowTagRoundResults(ICollection<PlayerController.ControlType> survivors)
    {
        List<PlayerController.ControlType> highlightedPlayers =
            survivors != null
                ? new List<PlayerController.ControlType>(survivors)
                : new List<PlayerController.ControlType>();

        string title;
        if (highlightedPlayers.Count == 0)
        {
            title = "EVERYONE IS IT\nNO SURVIVORS";
        }
        else if (highlightedPlayers.Count == 1)
        {
            title = GetDisplayName(highlightedPlayers[0]) + " SURVIVES!";
        }
        else
        {
            title = "SURVIVORS WIN!";
        }

        ShowTagResults(highlightedPlayers, title);
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
        ShowResults(new List<PlayerController.ControlType>(), "SCOREBOARD");
    }

    void ShowResults(
        ICollection<PlayerController.ControlType> highlightedPlayers,
        string title
    )
    {
        EnsureVisualsBuilt();

        if (panel != null)
        {
            panel.SetActive(true);
        }

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

        LayoutChart(visiblePlayers, highlightedPlayers, title);

        if (animateRoutine != null)
        {
            StopCoroutine(animateRoutine);
        }

        animateRoutine = StartCoroutine(AnimateBars(visiblePlayers));
    }

    void ShowTagResults(
        ICollection<PlayerController.ControlType> survivors,
        string title
    )
    {
        EnsureVisualsBuilt();

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (panel == null)
        {
            return;
        }

        List<PlayerController.ControlType> visiblePlayers = GetVisiblePlayers();
        if (visiblePlayers.Count == 0)
        {
            visiblePlayers.Add(PlayerController.ControlType.WASD);
        }

        LayoutTagChart(visiblePlayers, survivors, title);
    }

    void CacheLabels()
    {
        labels.Clear();
        TextMeshProUGUI template = wasdText != null ? wasdText : (ijklText != null ? ijklText : arrowText);

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

        if (template == null || panel == null)
        {
            return;
        }

        foreach (PlayerController.ControlType type in fallbackOrder)
        {
            if (labels.ContainsKey(type))
            {
                continue;
            }

            labels[type] = CreateRuntimeLabelTemplate(type, template);
        }
    }

    TextMeshProUGUI CreateRuntimeLabelTemplate(
        PlayerController.ControlType type,
        TextMeshProUGUI template
    )
    {
        GameObject labelObject = new GameObject(type + "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(panel.transform, false);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = template.font;
        label.fontSharedMaterial = template.fontSharedMaterial;
        label.fontSize = template.fontSize;
        label.alignment = template.alignment;
        label.color = template.color;
        label.text = string.Empty;

        RectTransform templateRect = template.rectTransform;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = templateRect.anchorMin;
        labelRect.anchorMax = templateRect.anchorMax;
        labelRect.pivot = templateRect.pivot;
        labelRect.sizeDelta = templateRect.sizeDelta;
        labelRect.anchoredPosition = templateRect.anchoredPosition;

        return label;
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
        ICollection<PlayerController.ControlType> highlightedPlayers,
        string title
    )
    {
        HashSet<PlayerController.ControlType> visibleSet =
            new HashSet<PlayerController.ControlType>(visiblePlayers);

        HashSet<PlayerController.ControlType> highlightedSet =
            highlightedPlayers != null
                ? new HashSet<PlayerController.ControlType>(highlightedPlayers)
                : new HashSet<PlayerController.ControlType>();

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
            bool isHighlighted = highlightedSet.Contains(type);

            ConfigureLabel(labels[type], type, yPosition);
            ConfigureBar(type, yPosition, isHighlighted);
            ConfigureScoreText(type, yPosition);
        }

        if (titleText != null)
        {
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 42f;
            titleText.text = title;
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
            float score = Mathf.Max(0f, ScoreManager.Instance.scores[type]);
            float clampedScore = Mathf.Clamp(score, 0f, targetScore);
            float targetWidth = barWidth * clampedScore / Mathf.Max(1f, targetScore);

            startWidths[type] = currentWidth;
            targetWidths[type] = targetWidth;
            scoreTexts[type].text = FormatScore(score) + "/" + FormatScore(targetScore);
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

    void LayoutTagChart(
        List<PlayerController.ControlType> visiblePlayers,
        ICollection<PlayerController.ControlType> survivors,
        string title
    )
    {
        if (animateRoutine != null)
        {
            StopCoroutine(animateRoutine);
            animateRoutine = null;
        }

        HashSet<PlayerController.ControlType> visibleSet =
            new HashSet<PlayerController.ControlType>(visiblePlayers);

        HashSet<PlayerController.ControlType> survivorSet =
            survivors != null
                ? new HashSet<PlayerController.ControlType>(survivors)
                : new HashSet<PlayerController.ControlType>();

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
            bool survived = survivorSet.Contains(type);

            ConfigureLabel(labels[type], type, yPosition);
            ConfigureBar(type, yPosition, survived);
            ConfigureScoreText(type, yPosition);

            RectTransform fillRect = barFills[type];
            float statusWidth = survived ? barWidth : barWidth * 0.34f;
            fillRect.sizeDelta = new Vector2(statusWidth, fillRect.sizeDelta.y);
            fillImages[type].color = survived
                ? Color.Lerp(GetPlayerColor(type), Color.white, 0.18f)
                : new Color(0.95f, 0.38f, 0.3f, 0.96f);
            backgroundImages[type].color = survived
                ? new Color(0.16f, 0.16f, 0.2f, 0.98f)
                : new Color(0.14f, 0.08f, 0.08f, 0.96f);
            scoreTexts[type].text = survived ? "SAFE" : "IT";
            scoreTexts[type].color = survived
                ? new Color(0.92f, 1f, 0.92f, 1f)
                : new Color(1f, 0.82f, 0.8f, 1f);
        }

        if (titleText != null)
        {
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 42f;
            titleText.text = title;
        }
    }

    string GetRaceTitleText(
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
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.GetPlayerDisplayName(type);
        }

        return GameManager.GetDefaultPlayerDisplayName(type);
    }

    Color GetPlayerColor(PlayerController.ControlType type)
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.GetPlayerUiColor(type);
        }

        return GameManager.GetDefaultPlayerUiColor(type);
    }

    string FormatScore(float score)
    {
        if (Mathf.Approximately(score, Mathf.Round(score)))
        {
            return Mathf.RoundToInt(score).ToString();
        }

        return score.ToString("0.##");
    }
}
