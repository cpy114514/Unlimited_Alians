using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public Sprite frameA;
    public Sprite frameB;
    public float animationSpeed = 8f;
    public Material particleMaterial;

    readonly Vector3 heldOffset = new Vector3(0f, 0.85f, 0f);
    const float heldSpacing = 0.28f;
    const float heldArcHeight = 0.08f;
    const float followSpeed = 18f;
    const float burstLifetime = 1.4f;
    const short burstCount = 14;

    SpriteRenderer spriteRenderer;
    Collider2D triggerCollider;
    Vector3 startPosition;
    Quaternion startRotation;
    PlayerController holder;
    bool collected;
    bool resolved;
    float animationTimer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<Collider2D>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        ResetCoin();
    }

    void Update()
    {
        if (spriteRenderer == null || frameA == null || frameB == null)
        {
            return;
        }

        animationTimer += Time.deltaTime * animationSpeed;
        bool useFirstFrame = Mathf.FloorToInt(animationTimer) % 2 == 0;
        spriteRenderer.sprite = useFirstFrame ? frameA : frameB;

        if (!collected || resolved)
        {
            return;
        }

        if (holder == null)
        {
            HideCoin();
            return;
        }

        Vector3 targetPosition = holder.transform.position + GetHeldOffset();
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * followSpeed
        );
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null || RoundManager.Instance == null ||
            !RoundManager.Instance.CanCollectCoin(player.controlType))
        {
            return;
        }

        collected = true;
        holder = player;
        resolved = false;
        transform.position = player.transform.position + GetHeldOffset();
        RoundManager.Instance.PlayerCollectedCoin(player.controlType);

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }

    public bool IsHeldBy(PlayerController.ControlType player)
    {
        return collected && !resolved && holder != null && holder.controlType == player;
    }

    public void ConsumeAtFinish()
    {
        if (!collected || resolved)
        {
            return;
        }

        resolved = true;

        if (holder != null)
        {
            transform.position = holder.transform.position + GetHeldOffset();
        }

        SpawnBurst();
        HideCoin();
        holder = null;
    }

    public void ClearHeldState(PlayerController.ControlType player)
    {
        if (!IsHeldBy(player))
        {
            return;
        }

        resolved = true;
        HideCoin();
        holder = null;
    }

    public void ResetCoin()
    {
        collected = false;
        resolved = false;
        holder = null;
        animationTimer = 0f;
        transform.position = startPosition;
        transform.rotation = startRotation;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = frameA != null ? frameA : spriteRenderer.sprite;
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
    }

    void HideCoin()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }

    Vector3 GetHeldOffset()
    {
        if (holder == null)
        {
            return heldOffset;
        }

        CoinPickup[] allCoins = FindObjectsOfType<CoinPickup>(true);
        int coinCount = 0;
        int myIndex = 0;

        foreach (CoinPickup coin in allCoins)
        {
            if (coin == null || !coin.collected || coin.resolved || coin.holder != holder)
            {
                continue;
            }

            if (coin.GetInstanceID() < GetInstanceID())
            {
                myIndex++;
            }

            coinCount++;
        }

        if (coinCount <= 1)
        {
            return heldOffset;
        }

        float centeredIndex = myIndex - (coinCount - 1) * 0.5f;
        float horizontalOffset = centeredIndex * heldSpacing;
        float verticalOffset = -Mathf.Abs(centeredIndex) * heldArcHeight;
        return heldOffset + new Vector3(horizontalOffset, verticalOffset, 0f);
    }

    void SpawnBurst()
    {
        GameObject burstObject = new GameObject("CoinBurst");
        burstObject.transform.position = transform.position;

        ParticleSystem particleSystem = burstObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer particleRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
        AssignParticleMaterial(particleRenderer);

        ParticleSystem.MainModule main = particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.7f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.05f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 1.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.13f, 0.22f);
        main.startColor = new Color(1f, 0.84f, 0.22f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.03f;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
            particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.92f, 0.45f), 0f),
                new GradientColorKey(new Color(1f, 0.7f, 0.16f), 0.6f),
                new GradientColorKey(new Color(0.95f, 0.42f, 0.08f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime =
            particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        particleSystem.Play();
        Destroy(burstObject, burstLifetime);
    }

    void AssignParticleMaterial(ParticleSystemRenderer particleRenderer)
    {
        if (particleRenderer == null)
        {
            return;
        }

        if (particleMaterial != null)
        {
            particleRenderer.material = particleMaterial;
            return;
        }

        Shader shader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Sprites/Default");

        if (shader == null)
        {
            return;
        }

        Material material = new Material(shader);
        material.name = "RuntimeCoinBurstMaterial";
        particleRenderer.material = material;
    }
}
