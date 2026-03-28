using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PipeEntrance : MonoBehaviour
{
    public enum EntranceDirection
    {
        Up,
        Left,
        Right,
        Down
    }

    static readonly Dictionary<int, float> playerCooldownUntil =
        new Dictionary<int, float>();
    static readonly HashSet<int> travellingPlayers =
        new HashSet<int>();

    [Header("Connection")]
    public PipePairManager pairManager;
    public EntranceDirection entranceDirection = EntranceDirection.Up;

    [Header("Parts")]
    public PipeEntryTrigger entryTrigger;
    public Transform waitPoint;
    public Transform exitPoint;

    [Header("Travel")]
    public float travelDelay = 0.35f;
    public float enterMoveDuration = 0.12f;
    public float exitMoveDuration = 0.14f;
    public float teleportCooldown = 0.75f;
    public bool disableControlDuringTravel = true;

    [Header("Debug")]
    public Color gizmoColor = new Color(0.3f, 0.95f, 0.35f, 0.9f);

    void Awake()
    {
        EnsurePairManagerReference();
        CacheChildReferences();
    }

    void OnValidate()
    {
        EnsurePairManagerReference();
        CacheChildReferences();

        if (entryTrigger != null)
        {
            entryTrigger.owner = this;
        }
    }

    public void HandleEntryTrigger(Collider2D other)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (BuildPhaseManager.Instance != null && !BuildPhaseManager.Instance.IsRaceActive)
        {
            return;
        }

        PipeEntrance linkedEntrance = GetLinkedEntrance();
        if (linkedEntrance == null)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || !CanPlayerEnter(player))
        {
            return;
        }

        int playerId = player.GetInstanceID();
        if (travellingPlayers.Contains(playerId))
        {
            return;
        }

        if (playerCooldownUntil.TryGetValue(playerId, out float cooldownUntil) &&
            Time.time < cooldownUntil)
        {
            return;
        }

        StartCoroutine(TravelPlayer(player, linkedEntrance));
    }

    public PipeEntrance GetLinkedEntrance()
    {
        EnsurePairManagerReference();
        return pairManager != null ? pairManager.GetLinkedEntrance(this) : null;
    }

    public Vector3 GetWaitPosition()
    {
        return waitPoint != null ? waitPoint.position : transform.position;
    }

    public Vector3 GetExitPosition()
    {
        return exitPoint != null ? exitPoint.position : transform.position;
    }

    bool CanPlayerEnter(PlayerController player)
    {
        if (player == null)
        {
            return false;
        }

        switch (entranceDirection)
        {
            case EntranceDirection.Up:
                return GameInput.GetVertical(player.inputBinding) <= -0.45f;
            case EntranceDirection.Left:
                return GameInput.GetHorizontal(player.inputBinding) >= 0.45f;
            case EntranceDirection.Right:
                return GameInput.GetHorizontal(player.inputBinding) <= -0.45f;
            case EntranceDirection.Down:
                return true;
        }

        return false;
    }

    IEnumerator TravelPlayer(PlayerController player, PipeEntrance linkedEntrance)
    {
        if (player == null || linkedEntrance == null)
        {
            yield break;
        }

        int playerId = player.GetInstanceID();
        travellingPlayers.Add(playerId);
        playerCooldownUntil[playerId] = Time.time + teleportCooldown;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (disableControlDuringTravel)
        {
            player.SetControlEnabled(false);
        }

        Vector3 sourceWaitPosition = GetWaitPosition();
        yield return MovePlayerTo(player, rb, player.transform.position, sourceWaitPosition, enterMoveDuration);

        float endTime = Time.time + Mathf.Max(0f, travelDelay);

        while (Time.time < endTime)
        {
            if (player == null)
            {
                travellingPlayers.Remove(playerId);
                yield break;
            }

            player.transform.position = sourceWaitPosition;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            yield return null;
        }

        if (player != null && linkedEntrance != null)
        {
            Vector3 destinationWaitPosition = linkedEntrance.GetWaitPosition();
            Vector3 destinationExitPosition = linkedEntrance.GetExitPosition();

            player.TeleportTo(destinationWaitPosition);
            playerCooldownUntil[playerId] = Time.time + teleportCooldown;

            yield return MovePlayerTo(
                player,
                rb,
                destinationWaitPosition,
                destinationExitPosition,
                exitMoveDuration
            );
        }

        travellingPlayers.Remove(playerId);

        if (player != null &&
            disableControlDuringTravel &&
            (BuildPhaseManager.Instance == null || BuildPhaseManager.Instance.IsRaceActive))
        {
            player.SetControlEnabled(true);
        }
    }

    IEnumerator MovePlayerTo(
        PlayerController player,
        Rigidbody2D rb,
        Vector3 from,
        Vector3 to,
        float duration
    )
    {
        if (player == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            player.transform.position = to;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (player == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            player.transform.position = Vector3.Lerp(from, to, t);

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            yield return null;
        }

        if (player != null)
        {
            player.transform.position = to;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(GetWaitPosition(), 0.08f);
        Gizmos.DrawWireSphere(GetExitPosition(), 0.08f);

        PipeEntrance linkedEntrance = GetLinkedEntrance();
        if (linkedEntrance != null)
        {
            Gizmos.DrawLine(GetExitPosition(), linkedEntrance.GetWaitPosition());
        }
    }

    void CacheChildReferences()
    {
        if (entryTrigger == null)
        {
            entryTrigger = GetComponentInChildren<PipeEntryTrigger>(true);
        }

        if (waitPoint == null)
        {
            Transform child = transform.Find("WaitPoint");
            if (child != null)
            {
                waitPoint = child;
            }
        }

        if (exitPoint == null)
        {
            Transform child = transform.Find("ExitPoint");
            if (child != null)
            {
                exitPoint = child;
            }
        }
    }

    void EnsurePairManagerReference()
    {
        if (pairManager != null)
        {
            return;
        }

        PipePairManager[] managers = Resources.FindObjectsOfTypeAll<PipePairManager>();
        for (int i = 0; i < managers.Length; i++)
        {
            PipePairManager candidate = managers[i];
            if (candidate != null && candidate.gameObject.scene.IsValid())
            {
                pairManager = candidate;
                break;
            }
        }
    }
}
