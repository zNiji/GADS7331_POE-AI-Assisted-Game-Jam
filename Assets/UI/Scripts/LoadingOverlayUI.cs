using UnityEngine;
using UnityEngine.UI;

public class LoadingOverlayUI : MonoBehaviour
{
    private static LoadingOverlayUI instance;

    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject root;
    [SerializeField] private Text label;

    public static void Show(string message = "Loading...")
    {
        EnsureInstance();
        if (instance == null) return;

        instance.SetVisible(true, message);
    }

    public static void Hide()
    {
        if (instance == null) return;
        instance.SetVisible(false, string.Empty);
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        LoadingOverlayUI existing = FindAnyObjectByType<LoadingOverlayUI>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject go = new GameObject("LoadingOverlayUI");
        instance = go.AddComponent<LoadingOverlayUI>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildIfMissing();
        SetVisible(false, string.Empty);
    }

    private void BuildIfMissing()
    {
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
        }

        if (gameObject.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (root == null)
        {
            root = new GameObject("Root", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, false);

            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);
            bg.raycastTarget = true;
        }

        if (label == null)
        {
            GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(root.transform, false);

            RectTransform tr = textGO.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.5f, 0.5f);
            tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.sizeDelta = new Vector2(1000f, 120f);
            tr.anchoredPosition = new Vector2(0f, 0f);

            label = textGO.GetComponent<Text>();
            // Newer Unity versions no longer expose Arial.ttf as a builtin resource.
            // LegacyRuntime.ttf is the supported fallback.
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null)
            {
                // Extra safety for older versions / unusual installs.
                f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            label.font = f;
            label.fontSize = 44;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;

            Outline o = textGO.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.95f);
            o.effectDistance = new Vector2(2f, -2f);
        }
    }

    private void SetVisible(bool isVisible, string message)
    {
        if (root != null) root.SetActive(isVisible);
        if (label != null) label.text = message;
    }
}

