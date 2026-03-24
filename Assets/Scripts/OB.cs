using UnityEngine;

public class KillBlock : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        BlueBeetleEnemy beetle = other.GetComponentInParent<BlueBeetleEnemy>();

        if (player != null)
        {
            if (RoundManager.Instance != null &&
                RoundManager.Instance.IsPlayerResolved(player.controlType))
            {
                return;
            }

            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.PlayerDied(player.controlType);
            }

            Destroy(player.gameObject);
        }

        if (beetle != null)
        {
            beetle.HitByHazard();
        }
    }
}
