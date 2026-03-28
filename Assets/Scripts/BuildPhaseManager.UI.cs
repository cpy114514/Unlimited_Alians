using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public partial class BuildPhaseManager
{
    void EnsureUi()
    {
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>(true);
        }

        if (canvas == null)
        {
            canvas = CreateRuntimeCanvas();
        }

        if (canvas == null || overlayPanel != null)
        {
            return;
        }

        overlayPanel = CreateUiObject("BuildOverlay", canvas.transform).gameObject;
        overlayImage = overlayPanel.AddComponent<Image>();
        overlayImage.color = new Color(0.06f, 0.06f, 0.08f, 0.84f);
        RectTransform panelRect = overlayPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        titleText = CreateLabel("Title", overlayPanel.transform, 42f, TextAlignmentOptions.Center);
        titleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, -48f);
        titleText.rectTransform.sizeDelta = new Vector2(960f, 60f);

        hintText = CreateLabel("Hint", overlayPanel.transform, 24f, TextAlignmentOptions.Center);
        hintText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        hintText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        hintText.rectTransform.pivot = new Vector2(0.5f, 1f);
        hintText.rectTransform.anchoredPosition = new Vector2(0f, -98f);
        hintText.rectTransform.sizeDelta = new Vector2(1100f, 60f);
        hintText.enableWordWrapping = true;

        int maxCardSelectionSegments = Enum.GetValues(typeof(PlayerController.ControlType)).Length;

        const int maxPartyBoxCards = 9;

        for (int i = 0; i < maxPartyBoxCards; i++)
        {
            RectTransform cardRect = CreateUiObject("Card" + i, overlayPanel.transform);
            cardRect.sizeDelta = new Vector2(250f, 120f);

            Image cardImage = cardRect.gameObject.AddComponent<Image>();
            cardImage.color = new Color(0.17f, 0.17f, 0.2f, 0.96f);
            cardImages.Add(cardImage);

            RectTransform markerRoot = CreateUiObject("CardMarkerRoot" + i, cardRect);
            markerRoot.anchorMin = new Vector2(0f, 1f);
            markerRoot.anchorMax = new Vector2(1f, 1f);
            markerRoot.pivot = new Vector2(0.5f, 1f);
            markerRoot.anchoredPosition = new Vector2(0f, -8f);
            markerRoot.sizeDelta = new Vector2(-16f, 8f);

            List<Image> selectionSegments = new List<Image>();
            for (int segmentIndex = 0; segmentIndex < maxCardSelectionSegments; segmentIndex++)
            {
                Image segment = CreateUiObject("SelectionSegment" + segmentIndex, markerRoot).gameObject.AddComponent<Image>();
                segment.raycastTarget = false;
                segment.gameObject.SetActive(false);
                selectionSegments.Add(segment);
            }
            cardSelectionSegments.Add(selectionSegments);

            RectTransform previewRoot = CreateUiObject("CardPreviewRoot" + i, cardRect);
            previewRoot.anchorMin = Vector2.zero;
            previewRoot.anchorMax = Vector2.one;
            previewRoot.offsetMin = new Vector2(16f, 52f);
            previewRoot.offsetMax = new Vector2(-16f, -18f);
            cardPreviewRoots.Add(previewRoot);

            Image previewSprite = CreateUiObject("CardPreviewSprite" + i, previewRoot).gameObject.AddComponent<Image>();
            previewSprite.preserveAspect = true;
            previewSprite.raycastTarget = false;
            previewSprite.gameObject.SetActive(false);
            RectTransform previewSpriteRect = previewSprite.rectTransform;
            previewSpriteRect.anchorMin = Vector2.zero;
            previewSpriteRect.anchorMax = Vector2.one;
            previewSpriteRect.offsetMin = Vector2.zero;
            previewSpriteRect.offsetMax = Vector2.zero;
            cardPreviewSprites.Add(previewSprite);

            List<Image> previewCells = new List<Image>();
            for (int cellIndex = 0; cellIndex < 9; cellIndex++)
            {
                Image cellImage = CreateUiObject("CardPreviewCell" + cellIndex, previewRoot).gameObject.AddComponent<Image>();
                cellImage.raycastTarget = false;
                cellImage.gameObject.SetActive(false);
                previewCells.Add(cellImage);
            }
            cardPreviewCells.Add(previewCells);

            TextMeshProUGUI cardText = CreateLabel("CardText" + i, cardRect, 28f, TextAlignmentOptions.Center);
            cardText.rectTransform.anchorMin = Vector2.zero;
            cardText.rectTransform.anchorMax = Vector2.one;
            cardText.rectTransform.offsetMin = new Vector2(12f, 10f);
            cardText.rectTransform.offsetMax = new Vector2(-12f, -58f);
            cardText.alignment = TextAlignmentOptions.BottomGeoAligned;
            cardTexts.Add(cardText);
        }

        foreach (PlayerController.ControlType type in System.Enum.GetValues(typeof(PlayerController.ControlType)))
        {
            TextMeshProUGUI status = CreateLabel(type + "Status", overlayPanel.transform, 26f, TextAlignmentOptions.Left);
            status.gameObject.SetActive(false);
            playerStatusTexts[type] = status;
        }
    }

    RectTransform CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    TextMeshProUGUI CreateLabel(
        string name,
        Transform parent,
        float fontSize,
        TextAlignmentOptions alignment
    )
    {
        RectTransform rect = CreateUiObject(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.font = TMP_Settings.defaultFontAsset;
        text.text = string.Empty;
        return text;
    }

    void SetCardSpritePreview(Image previewImage, Sprite sprite)
    {
        previewImage.sprite = sprite != null ? sprite : (blockSprite != null ? blockSprite : squareSprite);
        previewImage.color = Color.white;
        previewImage.gameObject.SetActive(true);
    }

    Vector2Int[] GetPreviewNormalizedCells(Vector2Int[] sourceCells)
    {
        if (sourceCells == null || sourceCells.Length == 0)
        {
            return new[] { Vector2Int.zero };
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;

        for (int i = 0; i < sourceCells.Length; i++)
        {
            minX = Mathf.Min(minX, sourceCells[i].x);
            minY = Mathf.Min(minY, sourceCells[i].y);
        }

        Vector2Int[] normalized = new Vector2Int[sourceCells.Length];
        for (int i = 0; i < sourceCells.Length; i++)
        {
            normalized[i] = new Vector2Int(sourceCells[i].x - minX, sourceCells[i].y - minY);
        }

        return normalized;
    }

    void RefreshCardPreviewUi(int cardIndex, BuildItemDefinition definition)
    {
        if (cardIndex < 0 || cardIndex >= cardPreviewRoots.Count)
        {
            return;
        }

        RectTransform previewRoot = cardPreviewRoots[cardIndex];
        Image spritePreview = cardPreviewSprites[cardIndex];
        List<Image> cellPreviews = cardPreviewCells[cardIndex];

        spritePreview.gameObject.SetActive(false);
        for (int i = 0; i < cellPreviews.Count; i++)
        {
            cellPreviews[i].gameObject.SetActive(false);
        }

        if (definition == null)
        {
            return;
        }

        if (definition.isCoin)
        {
            SetCardSpritePreview(spritePreview, coinTemplate != null ? coinTemplate.frameA : null);
            return;
        }

        if (definition.isTrampoline)
        {
            SetCardSpritePreview(spritePreview, trampolineTemplate != null ? trampolineTemplate.idleSprite : null);
            return;
        }

        if (definition.isLauncher)
        {
            SetCardSpritePreview(spritePreview, launcherTemplate != null ? launcherTemplate.launcherSprite : null);
            return;
        }

        if (definition.isPortal)
        {
            SetCardSpritePreview(spritePreview, portalDoorSprite);
            return;
        }

        Vector2Int[] previewCells = GetPreviewNormalizedCells(definition.cells);
        int maxX = 0;
        int maxY = 0;
        for (int i = 0; i < previewCells.Length; i++)
        {
            maxX = Mathf.Max(maxX, previewCells[i].x);
            maxY = Mathf.Max(maxY, previewCells[i].y);
        }

        float previewWidth = previewRoot.rect.width > 1f ? previewRoot.rect.width : 218f;
        float previewHeight = previewRoot.rect.height > 1f ? previewRoot.rect.height : 50f;
        float shapeWidth = maxX + 1;
        float shapeHeight = maxY + 1;
        float cellSize = Mathf.Min(previewWidth / (shapeWidth + 0.4f), previewHeight / (shapeHeight + 0.35f));
        float totalWidth = shapeWidth * cellSize;
        float totalHeight = shapeHeight * cellSize;
        Vector2 origin = new Vector2(
            (previewWidth - totalWidth) * 0.5f,
            (previewHeight - totalHeight) * 0.5f
        );

        for (int i = 0; i < previewCells.Length && i < cellPreviews.Count; i++)
        {
            Image cellImage = cellPreviews[i];
            cellImage.sprite = blockSprite != null ? blockSprite : squareSprite;
            cellImage.color = Color.white;
            RectTransform rect = cellImage.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * cellSize;
            rect.anchoredPosition = new Vector2(
                origin.x + previewCells[i].x * cellSize + cellSize * 0.5f,
                origin.y + previewCells[i].y * cellSize + cellSize * 0.5f
            );
            cellImage.gameObject.SetActive(true);
        }
    }

    void RefreshCardSelectionSegments(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= currentPool.Count || cardIndex >= cardSelectionSegments.Count)
        {
            return;
        }

        List<PlayerController.ControlType> hoveredPlayers = new List<PlayerController.ControlType>();
        foreach (KeyValuePair<PlayerController.ControlType, PlayerBuildState> playerEntry in playerStates)
        {
            PlayerBuildState state = playerEntry.Value;
            if (state.selected || GetSelectionIndex(state.selectionGrid) != cardIndex)
            {
                continue;
            }

            hoveredPlayers.Add(playerEntry.Key);
        }

        List<Image> segments = cardSelectionSegments[cardIndex];
        for (int i = 0; i < segments.Count; i++)
        {
            Image segment = segments[i];
            if (i >= hoveredPlayers.Count)
            {
                segment.gameObject.SetActive(false);
                continue;
            }

            float min = i / (float)hoveredPlayers.Count;
            float max = (i + 1) / (float)hoveredPlayers.Count;
            RectTransform rect = segment.rectTransform;
            rect.anchorMin = new Vector2(min, 0f);
            rect.anchorMax = new Vector2(max, 1f);
            rect.offsetMin = new Vector2(i == 0 ? 0f : 2f, 0f);
            rect.offsetMax = new Vector2(i == hoveredPlayers.Count - 1 ? 0f : -2f, 0f);
            segment.color = GetPlayerColor(hoveredPlayers[i]);
            segment.gameObject.SetActive(true);
        }
    }

    void ShowOverlay()
    {
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(true);
        }
    }

    void HideOverlay()
    {
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }
    }

    void SyncOverlayVisibility()
    {
        if (phase == BuildPhase.Selection || phase == BuildPhase.Placement)
        {
            ShowOverlay();
            return;
        }

        HideOverlay();
    }

    Canvas CreateRuntimeCanvas()
    {
        GameObject canvasObject = new GameObject(
            "RuntimeBuildCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        Canvas runtimeCanvas = canvasObject.GetComponent<Canvas>();
        runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Debug.LogWarning("BuildPhaseManager: no Canvas found in scene, created RuntimeBuildCanvas.");
        return runtimeCanvas;
    }

    void RefreshSelectionUi()
    {
        if (overlayPanel == null)
        {
            return;
        }

        titleText.text = "Party Box";
        hintText.text = "Move: Keyboard / Gamepad   Confirm: E / U / Enter / A";

        const int columns = 3;
        const float cardWidth = 250f;
        const float cardSpacingX = 280f;
        const float cardSpacingY = 150f;

        for (int i = 0; i < cardImages.Count; i++)
        {
            bool visible = i < currentPool.Count;
            cardImages[i].gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            int row = i / columns;
            int column = i % columns;
            RectTransform rect = cardImages[i].rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(cardWidth, 120f);
            rect.anchoredPosition = new Vector2(
                (column - 1) * cardSpacingX,
                120f - row * cardSpacingY
            );

            PoolEntry entry = currentPool[i];
            cardImages[i].color = entry.taken
                ? new Color(0.18f, 0.18f, 0.18f, 0.75f)
                : new Color(0.17f, 0.17f, 0.2f, 0.96f);

            string takenBy = entry.owner.HasValue ? "\n" + GetDisplayName(entry.owner.Value) : string.Empty;
            cardTexts[i].text = entry.definition.displayName + takenBy;
            cardTexts[i].color = entry.owner.HasValue ? GetPlayerColor(entry.owner.Value) : Color.white;
            RefreshCardPreviewUi(i, entry.definition);
            RefreshCardSelectionSegments(i);
        }

        LayoutPlayerStatuses(false);
    }

    void RefreshPlacementUi()
    {
        foreach (List<Image> segments in cardSelectionSegments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].gameObject.SetActive(false);
            }
        }

        LayoutPlayerStatuses(true);
    }

    void LayoutPlayerStatuses(bool placementPhase)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        int index = 0;
        foreach (TextMeshProUGUI statusEntry in playerStatusTexts.Values)
        {
            if (statusEntry != null)
            {
                statusEntry.gameObject.SetActive(false);
            }
        }

        IReadOnlyList<PlayerController.ControlType> sessionPlayers = GameManager.Instance.GetSessionPlayers();
        int playerCount = sessionPlayers.Count;
        if (playerCount <= 0)
        {
            return;
        }

        int columns = playerCount <= 3 ? playerCount : Mathf.CeilToInt(playerCount / 2f);
        int rows = Mathf.CeilToInt(playerCount / (float)columns);
        float horizontalSpacing = columns >= 3 ? 320f : 360f;
        float verticalSpacing = 46f;
        float bottomMargin = 26f;

        foreach (PlayerController.ControlType player in sessionPlayers)
        {
            TextMeshProUGUI status = playerStatusTexts[player];
            status.gameObject.SetActive(true);
            status.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            status.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            status.rectTransform.pivot = new Vector2(0.5f, 0f);
            status.rectTransform.sizeDelta = new Vector2(columns >= 3 ? 300f : 340f, 44f);

            int row = rows == 1 ? 0 : index / columns;
            int column = rows == 1 ? index : index % columns;
            int itemsInRow = rows == 1
                ? playerCount
                : (row == rows - 1 ? playerCount - row * columns : columns);
            float centeredRowOffset = (column - (itemsInRow - 1) * 0.5f) * horizontalSpacing;
            float anchoredY = bottomMargin + (rows - 1 - row) * verticalSpacing;
            status.rectTransform.anchoredPosition = new Vector2(centeredRowOffset, anchoredY);
            status.color = GetPlayerColor(player);

            PlayerBuildState state = playerStates[player];
            if (!placementPhase)
            {
                status.text = state.selected
                    ? GetDisplayName(player) + ": " + currentPool[state.selectedEntryIndex].definition.displayName
                    : GetDisplayName(player) + ": choosing...";
            }
            else
            {
                status.text = state.placed
                    ? GetDisplayName(player) + ": placed"
                    : GetDisplayName(player) + ": placing " +
                      currentPool[state.selectedEntryIndex].definition.displayName;
            }

            index++;
        }
    }
}
