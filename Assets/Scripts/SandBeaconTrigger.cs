using UnityEngine;

public class SandBeaconTrigger : MonoBehaviour
{
    public SandBeacon owner;

    void Awake()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<SandBeacon>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        NotifyOwner(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        NotifyOwner(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (owner == null || other == null)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        owner.NotifyPlayerExit(player);
    }

    void NotifyOwner(Collider2D other)
    {
        if (owner == null || other == null)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        owner.TryHandlePlayer(player);
    }
}
