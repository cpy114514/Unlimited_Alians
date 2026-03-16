using UnityEngine;

public class FinishFlag : MonoBehaviour
{
    public Vector2 waitOffsetStart = new Vector2(-0.8f, 0.4f);
    public Vector2 waitOffsetStep = new Vector2(0.8f, 0f);

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            RoundManager.Instance.PlayerReachedFlag(player, this);
        }
    }

    public void MovePlayerToWaitingArea(PlayerController player, int finishIndex)
    {
        if (player == null)
        {
            return;
        }

        player.MoveToWaitingArea(GetWaitingPosition(finishIndex));
    }

    Vector3 GetWaitingPosition(int finishIndex)
    {
        Vector2 offset = waitOffsetStart + waitOffsetStep * Mathf.Max(0, finishIndex);
        return transform.position + (Vector3)offset;
    }
}
