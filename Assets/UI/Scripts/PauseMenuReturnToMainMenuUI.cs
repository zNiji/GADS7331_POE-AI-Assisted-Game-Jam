using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuReturnToMainMenuUI : MonoBehaviour
{
    [SerializeField] private Button returnButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private GameObject savePromptPanel;
    private GameObject saveSlotsPanel;
    private bool promptCreated;
    private bool saveSlotsCreated;
    private Font promptFont;

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
        EnsureSavePromptUI();

        if (savePromptPanel != null)
        {
            savePromptPanel.SetActive(true);
        }
    }

    private void EnsureSavePromptUI()
    {
        if (promptCreated)
        {
            return;
        }

        promptCreated = true;

        Transform existing = transform.Find("SavePromptPanel");
        savePromptPanel = existing != null ? existing.gameObject : null;
        if (savePromptPanel != null)
        {
            return;
        }

        // Create a simple overlay panel on top of the pause panel.
        savePromptPanel = new GameObject("SavePromptPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        savePromptPanel.transform.SetParent(transform, false);
        savePromptPanel.SetActive(false);

        RectTransform prt = savePromptPanel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(620f, 320f);
        prt.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image img = savePromptPanel.GetComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.85f);

        Text title = CreateTextChild(savePromptPanel.transform, "SavePromptTitle", "Save before returning?\n", new Vector2(0f, 120f), 30);
        title.text = "Save before returning?\n";

        // Save slots button (opens a separate panel, as requested).
        Button chooseSlots = CreateButtonChild(savePromptPanel.transform, "ChooseSaveSlotsButton", "Choose Save Slot...", new Vector2(0f, 10f), new Vector2(560f, 56f));
        chooseSlots.onClick.AddListener(OpenSaveSlotsPanel);

        // Don't save button
        Button noSave = CreateButtonChild(savePromptPanel.transform, "NoSaveButton", "Don't Save & Return", new Vector2(0f, -70f), new Vector2(560f, 56f));
        noSave.onClick.AddListener(ReturnToMainMenuWithoutSaving);
    }

    private void EnsureSaveSlotsUI()
    {
        if (saveSlotsCreated)
        {
            return;
        }

        saveSlotsCreated = true;

        Transform existing = transform.Find("SaveSlotsPanel");
        saveSlotsPanel = existing != null ? existing.gameObject : null;
        if (saveSlotsPanel != null)
        {
            return;
        }

        // Create a second overlay panel that only contains the 3 slot buttons.
        saveSlotsPanel = new GameObject("SaveSlotsPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        saveSlotsPanel.transform.SetParent(transform, false);
        saveSlotsPanel.SetActive(false);

        RectTransform prt = saveSlotsPanel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(720f, 420f);
        prt.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image img = saveSlotsPanel.GetComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.85f);

        CreateTextChild(saveSlotsPanel.transform, "SaveSlotsTitle", "Choose a slot to save", new Vector2(0f, 170f), 28);

        float startY = 90f;
        float step = -80f;
        for (int i = 0; i < GameSaveSystem.SlotCount; i++)
        {
            int captured = i;
            string btnName = $"SaveSlot{captured + 1}Button";
            Vector2 pos = new Vector2(0f, startY + step * i);

            Button b = CreateButtonChild(saveSlotsPanel.transform, btnName, $"Save Slot {captured + 1}", pos, new Vector2(620f, 66f));
            b.onClick.AddListener(() => SaveAndReturn(captured));
        }

        // Back button
        Button back = CreateButtonChild(saveSlotsPanel.transform, "BackToPromptButton", "Back", new Vector2(0f, -140f), new Vector2(300f, 56f));
        back.onClick.AddListener(BackToSavePrompt);
    }

    private void OpenSaveSlotsPanel()
    {
        EnsureSaveSlotsUI();

        if (savePromptPanel != null) savePromptPanel.SetActive(false);
        if (saveSlotsPanel != null) saveSlotsPanel.SetActive(true);

        // Refresh slot button labels with saved meta (if available).
        for (int i = 0; i < GameSaveSystem.SlotCount; i++)
        {
            Transform btnT = saveSlotsPanel != null ? saveSlotsPanel.transform.Find($"SaveSlot{i + 1}Button") : null;
            if (btnT == null) continue;
            Button btn = btnT.GetComponent<Button>();
            if (btn == null) continue;
            Text label = btn.GetComponentInChildren<Text>();
            if (label == null) continue;

            GameSaveSystem.SaveSlotMeta meta = GameSaveSystem.GetSlotMeta(i);
            label.text = meta.exists ? $"Save Slot {i + 1}\n{meta.displayText}" : $"Save Slot {i + 1}\nEmpty";
        }
    }

    private void BackToSavePrompt()
    {
        if (saveSlotsPanel != null) saveSlotsPanel.SetActive(false);
        if (savePromptPanel != null) savePromptPanel.SetActive(true);
    }

    private Text CreateTextChild(Transform parent, string name, string text, Vector2 anchoredPos, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(580f, 60f);
        rt.anchoredPosition = anchoredPos;

        Text t = go.GetComponent<Text>();
        t.font = GetSafeFont();
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = text;
        return t;
    }

    private Button CreateButtonChild(Transform parent, string name, string labelText, Vector2 anchoredPos, Vector2 size)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        UnityEngine.UI.Image img = buttonGO.GetComponent<UnityEngine.UI.Image>();
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
        txt.font = GetSafeFont();
        txt.fontSize = 24;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = labelText;

        return btn;
    }

    private Font GetSafeFont()
    {
        if (promptFont != null) return promptFont;

        try
        {
            promptFont = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            promptFont = null;
        }

        return promptFont;
    }

    private void SaveAndReturn(int slotIndex0Based)
    {
        GameSaveSystem.SaveToSlot(slotIndex0Based);
        ReturnToMainMenuWithoutSaving();
    }

    private void ReturnToMainMenuWithoutSaving()
    {
        if (savePromptPanel != null)
        {
            savePromptPanel.SetActive(false);
        }

        if (saveSlotsPanel != null)
        {
            saveSlotsPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPause(false);
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}

