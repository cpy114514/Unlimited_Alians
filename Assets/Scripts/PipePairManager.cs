using System.Collections.Generic;
using UnityEngine;

public class PipePairManager : MonoBehaviour
{
    [System.Serializable]
    public class PipePair
    {
        public PipeEntrance entranceA;
        public PipeEntrance entranceB;
    }

    public List<PipePair> pipePairs = new List<PipePair>();
    public Color gizmoColor = new Color(0.25f, 1f, 0.75f, 0.9f);

    public PipeEntrance GetLinkedEntrance(PipeEntrance source)
    {
        if (source == null)
        {
            return null;
        }

        for (int i = 0; i < pipePairs.Count; i++)
        {
            PipePair pair = pipePairs[i];
            if (pair == null)
            {
                continue;
            }

            if (pair.entranceA == source)
            {
                return pair.entranceB;
            }

            if (pair.entranceB == source)
            {
                return pair.entranceA;
            }
        }

        return null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        for (int i = 0; i < pipePairs.Count; i++)
        {
            PipePair pair = pipePairs[i];
            if (pair == null || pair.entranceA == null || pair.entranceB == null)
            {
                continue;
            }

            Gizmos.DrawLine(pair.entranceA.GetExitPosition(), pair.entranceB.GetWaitPosition());
            Gizmos.DrawLine(pair.entranceB.GetExitPosition(), pair.entranceA.GetWaitPosition());
        }
    }
}
