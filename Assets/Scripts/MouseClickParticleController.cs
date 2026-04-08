using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MouseClickParticleController : MonoBehaviour
{
    class Particle
    {
        public RectTransform rect;
        public Image image;
        public Vector2 position;
        public Vector2 velocity;
        public float age;
        public float lifetime;
        public float angularVelocity;
        public Color startColor;
        public Color endColor;
    }

    static MouseClickParticleController instance;
    static bool globalEnabled = true;

    [Header("Particles")]
    public int particlesPerClick = 18;
    public float minLifetime = 0.58f;
    public float maxLifetime = 1.05f;
    public float minSpeed = 80f;
    public float maxSpeed = 250f;
    public float gravity = -820f;
    public float minSize = 5f;
    public float maxSize = 12f;
    public int sortingOrder = 7000;
    public Color brightYellow = new Color(1f, 0.92f, 0.18f, 1f);
    public Color deepYellow = new Color(1f, 0.52f, 0.03f, 1f);
    public Color fadeColor = new Color(0.38f, 0.18f, 0.02f, 0f);

    Canvas canvas;
    RectTransform root;
    readonly List<Particle> particles = new List<Particle>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject root = new GameObject("MouseClickParticleController");
        instance = root.AddComponent<MouseClickParticleController>();
        DontDestroyOnLoad(root);
        globalEnabled = PlayerPrefs.GetInt(SettingsMenuController.ClickParticlesKey, 1) == 1;
    }

    public static void SetGlobalEnabled(bool enabled)
    {
        globalEnabled = enabled;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureCanvas();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (globalEnabled && Input.GetMouseButtonDown(0))
        {
            Spawn(Input.mousePosition);
        }

        UpdateParticles(Time.unscaledDeltaTime);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureCanvas();
    }

    void EnsureCanvas()
    {
        if (canvas != null)
        {
            RemoveParticleCanvasRaycaster();
            return;
        }

        GameObject canvasObject = new GameObject(
            "MouseClickParticleCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root = canvasObject.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        RemoveParticleCanvasRaycaster();
    }

    void RemoveParticleCanvasRaycaster()
    {
        if (canvas == null)
        {
            return;
        }

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            Destroy(raycaster);
        }
    }

    void Spawn(Vector2 screenPosition)
    {
        EnsureCanvas();
        if (root == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, null, out Vector2 localPosition);
        for (int i = 0; i < particlesPerClick; i++)
        {
            Particle particle = CreateParticle();
            float angle = Random.Range(30f, 150f) * Mathf.Deg2Rad;
            float speed = Random.Range(minSpeed, maxSpeed);
            float size = Random.Range(minSize, maxSize);
            Color particleStartColor = Color.Lerp(deepYellow, brightYellow, Random.value);
            Color particleEndColor = Color.Lerp(fadeColor, particleStartColor, 0.18f);
            particleEndColor.a = 0f;

            particle.position = localPosition;
            particle.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            particle.age = 0f;
            particle.lifetime = Random.Range(minLifetime, maxLifetime);
            particle.angularVelocity = Random.Range(-360f, 360f);
            particle.startColor = particleStartColor;
            particle.endColor = particleEndColor;

            particle.rect.anchoredPosition = localPosition;
            particle.rect.sizeDelta = new Vector2(size, size);
            particle.rect.localScale = Vector3.one;
            particle.rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            particle.image.color = particle.startColor;
            particle.rect.gameObject.SetActive(true);
        }
    }

    Particle CreateParticle()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            if (!particles[i].rect.gameObject.activeSelf)
            {
                return particles[i];
            }
        }

        GameObject particleObject = new GameObject(
            "ClickParticle",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        particleObject.transform.SetParent(root, false);

        Image image = particleObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = brightYellow;

        Particle particle = new Particle
        {
            rect = particleObject.GetComponent<RectTransform>(),
            image = image
        };
        particles.Add(particle);
        return particle;
    }

    void UpdateParticles(float deltaTime)
    {
        for (int i = 0; i < particles.Count; i++)
        {
            Particle particle = particles[i];
            if (!particle.rect.gameObject.activeSelf)
            {
                continue;
            }

            particle.age += deltaTime;
            if (particle.age >= particle.lifetime)
            {
                particle.rect.gameObject.SetActive(false);
                continue;
            }

            float t = Mathf.Clamp01(particle.age / Mathf.Max(0.01f, particle.lifetime));
            particle.velocity += Vector2.up * gravity * deltaTime;
            particle.position += particle.velocity * deltaTime;
            particle.rect.anchoredPosition = particle.position;
            particle.rect.localRotation *= Quaternion.Euler(0f, 0f, particle.angularVelocity * deltaTime);
            particle.image.color = Color.Lerp(particle.startColor, particle.endColor, t);
            float scale = Mathf.Lerp(1f, 0.45f, t);
            particle.rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
