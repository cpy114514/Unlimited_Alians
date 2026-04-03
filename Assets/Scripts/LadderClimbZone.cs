using UnityEngine;

public class LadderClimbZone : MonoBehaviour
{
    public Ladder owner;

    void Awake()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<Ladder>();
        }
    }
}
