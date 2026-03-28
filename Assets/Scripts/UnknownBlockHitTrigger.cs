using UnityEngine;

public class UnknownBlockHitTrigger : MonoBehaviour
{
    [HideInInspector]
    public UnknownBlock owner;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null)
        {
            owner.NotifyHitTrigger(other);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (owner != null)
        {
            owner.NotifyHitTrigger(other);
        }
    }
}
