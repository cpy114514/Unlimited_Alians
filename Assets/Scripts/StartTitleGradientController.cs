using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartTitleGradientController : MonoBehaviour
{
    static StartTitleGradientController instance;

    [Header("Scene")]
    public string startSceneName = "Start";

    [Header("Gradient")]
    public Color black = new Color(0.02f, 0.02f, 0.02f, 1f);
    public Color white = new Color(0.58f, 0.58f, 0.58f, 1f);
    public float angle = 0f;
    public float scale = 1.3f;
    public float scrollSpeed = 0.18f;

    TextMeshProUGUI titleText;
    Image titleImage;
    Material titleMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject root = new GameObject("StartTitleGradientController");
        instance = root.AddComponent<StartTitleGradientController>();
        DontDestroyOnLoad(root);
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
        if (titleText != null)
        {
            ApplyTextGradient();
            return;
        }

        if (titleMaterial == null || titleImage == null)
        {
            return;
        }

        titleMaterial.SetFloat("_GradientOffset", Mathf.Repeat(Time.unscaledTime * scrollSpeed, 1f));
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == startSceneName)
        {
            StartCoroutine(ApplyAfterLayout());
            return;
        }

        ClearTitle();
    }

    IEnumerator ApplyAfterLayout()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        ApplyToStartTitle();
    }

    void ApplyToStartTitle()
    {
        ClearTitle();

        titleText = FindStartTitleText();
        if (titleText != null)
        {
            titleText.enableVertexGradient = true;
            titleText.color = Color.white;
            ApplyTextGradient();
            return;
        }

        titleImage = FindStartTitleImage();
        if (titleImage == null)
        {
            return;
        }

        Shader shader = Shader.Find("UI/StartTitleBlackWhiteGradient");
        if (shader == null)
        {
            return;
        }

        titleMaterial = new Material(shader)
        {
            name = "Runtime Start Title Black White Gradient"
        };
        titleMaterial.hideFlags = HideFlags.DontSave;
        titleMaterial.SetColor("_Black", black);
        titleMaterial.SetColor("_White", white);
        titleMaterial.SetFloat("_GradientAngle", angle);
        titleMaterial.SetFloat("_GradientScale", scale);
        titleImage.material = titleMaterial;
    }

    void ApplyTextGradient()
    {
        float t = Mathf.PingPong(Time.unscaledTime * scrollSpeed, 1f);
        Color left = Color.Lerp(black, white, t);
        Color right = Color.Lerp(black, white, 1f - t);
        titleText.colorGradient = new VertexGradient(left, right, left, right);
    }

    TextMeshProUGUI FindStartTitleText()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Canvas[] canvases = FindObjectsOfType<Canvas>();

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.gameObject.scene != activeScene)
            {
                continue;
            }

            TextMeshProUGUI[] texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int textIndex = 0; textIndex < texts.Length; textIndex++)
            {
                TextMeshProUGUI text = texts[textIndex];
                if (text != null &&
                    text.gameObject.activeInHierarchy &&
                    text.name.ToLowerInvariant().Contains("title"))
                {
                    return text;
                }
            }
        }

        return null;
    }

    Image FindStartTitleImage()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Image best = null;
        float bestScore = 0f;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.gameObject.scene != activeScene)
            {
                continue;
            }

            for (int childIndex = 0; childIndex < canvas.transform.childCount; childIndex++)
            {
                Transform child = canvas.transform.GetChild(childIndex);
                if (child == null || !child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Image image = child.GetComponent<Image>();
                RectTransform rect = child as RectTransform;
                if (image == null ||
                    rect == null ||
                    child.GetComponent<Button>() != null ||
                    child.childCount > 0 ||
                    image.sprite == null)
                {
                    continue;
                }

                if (rect.anchoredPosition.y < 120f)
                {
                    continue;
                }

                float area = Mathf.Abs(rect.rect.width * rect.rect.height);
                if (area > bestScore)
                {
                    bestScore = area;
                    best = image;
                }
            }
        }

        return best;
    }

    void ClearTitle()
    {
        if (titleText != null)
        {
            titleText.enableVertexGradient = false;
            titleText = null;
        }

        if (titleImage != null)
        {
            titleImage.material = null;
            titleImage = null;
        }

        if (titleMaterial != null)
        {
            Destroy(titleMaterial);
            titleMaterial = null;
        }
    }
}
