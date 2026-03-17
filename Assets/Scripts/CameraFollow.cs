using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MultiplayerCameraFollow : MonoBehaviour
{
    [Header("Race Camera")]
    [FormerlySerializedAs("smoothTime")]
    public float raceSmoothTime = 0.2f;
    [FormerlySerializedAs("minZoom")]
    public float raceMinZoom = 5f;
    [FormerlySerializedAs("maxZoom")]
    public float raceMaxZoom = 12f;
    [FormerlySerializedAs("zoomLimiter")]
    public float raceZoomLimiter = 10f;
    public float raceZoomLerpSpeed = 5f;
    public Vector2 raceOffset = Vector2.zero;

    [Header("Build Camera")]
    public float buildSmoothTime = 0.14f;
    public float buildMinZoom = 6.5f;
    public float buildMaxZoom = 16f;
    public float buildZoomLimiter = 18f;
    public float buildZoomLerpSpeed = 7f;
    public Vector2 buildOffset = new Vector2(0f, 0.2f);

    Camera cam;
    Vector3 velocity;
    readonly List<Vector3> targets = new List<Vector3>();

    struct CameraSettings
    {
        public float smoothTime;
        public float minZoom;
        public float maxZoom;
        public float zoomLimiter;
        public float zoomLerpSpeed;
        public Vector2 offset;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }

        bool usingBuildTargets = CollectTargets();
        if (targets.Count == 0)
        {
            return;
        }

        CameraSettings settings = usingBuildTargets
            ? GetBuildSettings()
            : GetRaceSettings();

        Move(targets, settings);
        Zoom(targets, settings);
    }

    bool CollectTargets()
    {
        targets.Clear();

        if (BuildPhaseManager.Instance != null &&
            BuildPhaseManager.Instance.TryGetCameraTargetPositions(targets))
        {
            return true;
        }

        foreach (PlayerController player in FindObjectsOfType<PlayerController>())
        {
            if (player != null)
            {
                targets.Add(player.transform.position);
            }
        }

        return false;
    }

    CameraSettings GetRaceSettings()
    {
        return new CameraSettings
        {
            smoothTime = raceSmoothTime,
            minZoom = raceMinZoom,
            maxZoom = raceMaxZoom,
            zoomLimiter = raceZoomLimiter,
            zoomLerpSpeed = raceZoomLerpSpeed,
            offset = raceOffset
        };
    }

    CameraSettings GetBuildSettings()
    {
        return new CameraSettings
        {
            smoothTime = buildSmoothTime,
            minZoom = buildMinZoom,
            maxZoom = buildMaxZoom,
            zoomLimiter = buildZoomLimiter,
            zoomLerpSpeed = buildZoomLerpSpeed,
            offset = buildOffset
        };
    }

    void Move(List<Vector3> targetPositions, CameraSettings settings)
    {
        Vector3 centerPoint = GetCenterPoint(targetPositions);
        Vector3 newPosition = new Vector3(
            centerPoint.x + settings.offset.x,
            centerPoint.y + settings.offset.y,
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            newPosition,
            ref velocity,
            settings.smoothTime
        );
    }

    void Zoom(List<Vector3> targetPositions, CameraSettings settings)
    {
        float greatestDistance = GetGreatestDistance(targetPositions);
        float t = greatestDistance / Mathf.Max(0.01f, settings.zoomLimiter);
        float targetZoom = Mathf.Lerp(settings.minZoom, settings.maxZoom, t);
        targetZoom = Mathf.Clamp(targetZoom, settings.minZoom, settings.maxZoom);

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetZoom,
            Time.deltaTime * settings.zoomLerpSpeed
        );
    }

    Vector3 GetCenterPoint(List<Vector3> targetPositions)
    {
        if (targetPositions.Count == 1)
        {
            return targetPositions[0];
        }

        Bounds bounds = new Bounds(targetPositions[0], Vector3.zero);

        for (int i = 1; i < targetPositions.Count; i++)
        {
            bounds.Encapsulate(targetPositions[i]);
        }

        return bounds.center;
    }

    float GetGreatestDistance(List<Vector3> targetPositions)
    {
        Bounds bounds = new Bounds(targetPositions[0], Vector3.zero);

        for (int i = 1; i < targetPositions.Count; i++)
        {
            bounds.Encapsulate(targetPositions[i]);
        }

        return Mathf.Max(bounds.size.x, bounds.size.y);
    }
}
