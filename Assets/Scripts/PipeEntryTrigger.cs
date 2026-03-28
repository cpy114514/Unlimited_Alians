using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Collider2D))]
public class PipeEntryTrigger : MonoBehaviour
{
    public PipeEntrance owner;

    void OnValidate()
    {
        EnsureTrigger();
    }

    void Awake()
    {
        EnsureTrigger();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null)
        {
            owner.HandleEntryTrigger(other);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (owner != null)
        {
            owner.HandleEntryTrigger(other);
        }
    }

    void EnsureTrigger()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }
}
