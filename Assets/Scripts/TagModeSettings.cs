using TMPro;
using UnityEngine;

public class TagModeSettings : MonoBehaviour
{
    [Min(0.25f)]
    public float roundDurationMinutes = 2f;

    [Min(0f)]
    public float respawnDelay = 1f;

    [Min(0f)]
    public float respawnInvulnerability = 1.25f;

    [Min(0f)]
    public float passCooldown = 0.35f;

    [Min(1f)]
    public float itMoveSpeedMultiplier = 1.15f;

    [Min(0.04f)]
    public float selectionStepInterval = 0.08f;

    [Min(0f)]
    public float selectionShuffleDuration = 1.15f;

    [Min(0f)]
    public float selectionRevealDuration = 0.85f;

    [Min(0f)]
    public float endFocusDuration = 1f;

    [Min(0f)]
    public float endBlastTextDuration = 1.35f;

    public float endFocusSmoothTime = 0.12f;
    public float endFocusMinZoom = 4.6f;
    public float endFocusMaxZoom = 8f;
    public float endFocusZoomLimiter = 6f;
    public float endFocusZoomLerpSpeed = 7f;
    public Vector2 endFocusOffset = new Vector2(0f, 0.45f);
    public Vector2 blastHorizontalVelocity = new Vector2(7f, 11f);
    public Sprite blastExplosionSprite;
    public Vector3 blastExplosionOffset = new Vector3(0f, 0.35f, 0f);
    public Vector3 blastExplosionStartScale = new Vector3(0.7f, 0.7f, 1f);
    public Vector3 blastExplosionEndScale = new Vector3(2.4f, 2.4f, 1f);
    [Min(0.05f)]
    public float blastExplosionDuration = 0.38f;
    public Color blastExplosionColor = Color.white;

    public Sprite tagMarkerSprite;
    public Vector3 tagMarkerOffset = new Vector3(0f, 1.1f, 0f);
    public Vector3 tagMarkerScale = new Vector3(0.7f, 0.7f, 1f);
    public Color tagMarkerColor = Color.white;
    public Color protectedMarkerColor = new Color(1f, 0.85f, 0.55f, 1f);

    [Header("Canvas References")]
    public GameObject tagHudRoot;
    public TextMeshProUGUI tagHudText;
    public GameObject tagSelectionRoot;
    public TextMeshProUGUI tagSelectionTitleText;
    public TextMeshProUGUI tagSelectionPlayersText;
    public GameObject tagEventRoot;
    public TextMeshProUGUI tagEventText;
}
