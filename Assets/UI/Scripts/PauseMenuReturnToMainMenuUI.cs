using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuReturnToMainMenuUI : MonoBehaviour
{
    [SerializeField] private Button returnButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        EnsureButton();
    }

    private void EnsureButton()
    {
        if (returnButton != null)
        {
            return;
        }

        Transform existing = transform.Find("ReturnToMainMenuButton");
        if (existing != null)
        {
            returnButton = existing.GetComponent<Button>();
            return;
        }

        GameObject buttonGO = new GameObject("ReturnToMainMenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(transform, false);

        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(520f, 60f);
        rt.anchoredPosition = new Vector2(0f, 20f);

        Image img = buttonGO.GetComponent<Image>();
        img.color = new Color(0.18f, 0.24f, 0.32f, 1f);

        Button btn = buttonGO.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = img.color;
        cb.highlightedColor = new Color(0.26f, 0.34f, 0.46f, 1f);
        cb.pressedColor = new Color(0.14f, 0.2f, 0.28f, 1f);
        btn.colors = cb;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(buttonGO.transform, false);
        RectTransform lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        Text txt = labelGO.GetComponent<Text>();
        try
        {
            txt.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            // leave default
        }
        txt.fontSize = 26;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = "Return to Main Menu";

        returnButton = btn;
        returnButton.onClick.RemoveAllListeners();
        returnButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        // If your GameManager persists, make sure timeScale is restored.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPause(false);
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}

