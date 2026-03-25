using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuStartupScreen : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject startupPanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Scene Loading")]
    [SerializeField] private string gameplaySceneName = "Level_01";

    [Header("Startup")]
    [SerializeField] private float inputUnlockDelay = 0.2f;

    private bool showingStartup = true;
    private float startupTime;

    [Header("Save Slots (Main Menu)")]
    [SerializeField] private string loadSavesButtonLabel = "Load Game";

    [Header("New Game (Fresh)")]
    [SerializeField] private string newGameButtonLabel = "New Game (Fresh)";

    private GameObject saveSlotsPanel;
    private GameObject loadSavesButtonGO;
    private GameObject newGameButtonGO;

    private void Start()
    {
        Time.timeScale = 1f;
        startupTime = Time.unscaledTime;
        // Always show the main menu immediately so returning from pause can't get stuck
        // behind the "press any key" startup panel.
        showingStartup = false;
        SetStartupVisible(false);
        SetMainMenuVisible(true);

        // Prevent "No cameras rendering Display 1" overlay by forcing menu cameras
        // to render on the same display as your GameView.
        EnsureCamerasForMenu();

        EnsureSaveSlotsUI();

        ReflowMainMenuButtonsForLoadGame();

        EnsureNewGameButtonUI();
        ReflowMainMenuButtonsForLoadGame();
    }

    private void EnsureNewGameButtonUI()
    {
        if (mainMenuPanel == null) return;

        Transform buttonsRoot = mainMenuPanel.transform.Find("ButtonsRoot");
        if (buttonsRoot == null) return;

        if (newGameButtonGO != null) return;

        Transform existingBtnT = buttonsRoot.Find("NewGameButton");
        newGameButtonGO = existingBtnT != null ? existingBtnT.gameObject : null;

        if (newGameButtonGO == null)
        {
            newGameButtonGO = new GameObject("NewGameButton", typeof(RectTransform), typeof(Image), typeof(Button));
            newGameButtonGO.transform.SetParent(buttonsRoot, false);

            RectTransform rt = newGameButtonGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 40f);
            rt.sizeDelta = new Vector2(520f, 70f);

            Image img = newGameButtonGO.GetComponent<Image>();
            img.color = new Color(0.18f, 0.24f, 0.32f, 1f);

            Button button = newGameButtonGO.GetComponent<Button>();
            ColorBlock cb = button.colors;
            cb.normalColor = img.color;
            cb.highlightedColor = new Color(0.26f, 0.34f, 0.46f, 1f);
            cb.pressedColor = new Color(0.14f, 0.2f, 0.28f, 1f);
            button.colors = cb;

            // Label
            GameObject labelGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(newGameButtonGO.transform, false);
            RectTransform trt = labelGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            Text txt = labelGO.GetComponent<Text>();
            txt.font = GetSafeFont();
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = newGameButtonLabel;

            button.onClick.AddListener(StartNewFreshGame);
        }
    }

    public void StartNewFreshGame()
    {
        // Clears extracted/banked materials and starts a clean run.
        // Note: inventory run resources are already cleared by GameManager.ResetRun().
        Time.timeScale = 1f;
        GameSaveSystem.ClearPendingLoadSlot();

        // Clear permanent upgrades too (fresh start from the beginning).
        if (PermanentUpgradeSystem.Instance != null)
        {
            PermanentUpgradeSystem.Instance.ClearAllPermanentUpgrades(alsoDeletePersistedKey: true);
        }
        else
        {
            PlayerPrefs.DeleteKey("permanent_upgrades_v1");
            PlayerPrefs.Save();
        }

        // Delete the persisted extracted-resource bank.
        if (ExtractedResourceBank.Instance != null)
        {
            ExtractedResourceBank.Instance.ClearAllBankedResources(alsoDeletePersistedKey: true);
        }
        else
        {
            // If the bank isn't instantiated yet (e.g. main menu scene), still clear persisted storage.
            PlayerPrefs.DeleteKey("extracted_resource_bank_v1");
            PlayerPrefs.Save();
        }

        SetStartupVisible(false);
        SetMainMenuVisible(false);

#if UNITY_EDITOR
        TryEnsureBuildSettingsViaReflection();
#endif

        SceneManager.sceneLoaded += StaticOnSceneLoadedForceCameras;
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void EnsureCamerasForMenu()
    {
        int desiredDisplay = (Display.displays != null && Display.displays.Length > 1 && Display.displays[1] != null) ? 1 : 0;

        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cams.Length; i++)
        {
            Camera c = cams[i];
            if (c == null) continue;
            c.gameObject.SetActive(true);
            c.enabled = true;
            c.targetDisplay = desiredDisplay;
        }
    }

    private void Update()
    {
        if (!showingStartup)
        {
            return;
        }

        if (Time.unscaledTime - startupTime < inputUnlockDelay)
        {
            return;
        }

        if (Input.anyKeyDown)
        {
            ShowMainMenu();
        }
    }

    public void ShowMainMenu()
    {
        showingStartup = false;
        SetStartupVisible(false);
        SetMainMenuVisible(true);
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        GameSaveSystem.ClearPendingLoadSlot();
        SetStartupVisible(false);
        SetMainMenuVisible(false);

#if UNITY_EDITOR
        // Unity 6 scene loading can depend on the active Build Profile.
        // But runtime assemblies can't directly reference Editor scripts.
        // Invoke the editor-only menu function via reflection instead.
        TryEnsureBuildSettingsViaReflection();
#endif

        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogWarning("Gameplay scene name is not set.");
            return;
        }

        SceneManager.sceneLoaded += StaticOnSceneLoadedForceCameras;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void LoadSaveSlot1() => LoadSaveSlot(0);
    public void LoadSaveSlot2() => LoadSaveSlot(1);
    public void LoadSaveSlot3() => LoadSaveSlot(2);

    private void LoadSaveSlot(int slotIndex0Based)
    {
        Time.timeScale = 1f;
        GameSaveSystem.SetPendingLoadSlot(slotIndex0Based);

        SetStartupVisible(false);
        SetMainMenuVisible(false);

#if UNITY_EDITOR
        TryEnsureBuildSettingsViaReflection();
#endif

        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogWarning("Gameplay scene name is not set.");
            return;
        }

        SceneManager.sceneLoaded += StaticOnSceneLoadedForceCameras;
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void EnsureSaveSlotsUI()
    {
        if (mainMenuPanel == null)
        {
            return;
        }

        // 1) Add a single button in the main menu.
        Transform buttonsRoot = mainMenuPanel.transform.Find("ButtonsRoot");
        if (buttonsRoot == null)
        {
            buttonsRoot = new GameObject("ButtonsRoot").transform;
            buttonsRoot.SetParent(mainMenuPanel.transform, false);
        }

        if (loadSavesButtonGO == null)
        {
            Transform existingBtnT = buttonsRoot.Find("LoadSavesButton");
            loadSavesButtonGO = existingBtnT != null ? existingBtnT.gameObject : null;
        }

        if (loadSavesButtonGO == null)
        {
            loadSavesButtonGO = new GameObject("LoadSavesButton", typeof(RectTransform), typeof(Image), typeof(Button));
            loadSavesButtonGO.transform.SetParent(buttonsRoot, false);

            RectTransform rt = loadSavesButtonGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Position will be finalized in ReflowMainMenuButtonsForLoadGame().
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(520f, 70f);

            Image img = loadSavesButtonGO.GetComponent<Image>();
            img.color = new Color(0.18f, 0.24f, 0.32f, 1f);

            Button button = loadSavesButtonGO.GetComponent<Button>();
            ColorBlock cb = button.colors;
            cb.normalColor = img.color;
            cb.highlightedColor = new Color(0.26f, 0.34f, 0.46f, 1f);
            cb.pressedColor = new Color(0.14f, 0.2f, 0.28f, 1f);
            button.colors = cb;

            GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(loadSavesButtonGO.transform, false);
            RectTransform lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            Text txt = labelGO.GetComponent<Text>();
            txt.font = GetSafeFont();
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = loadSavesButtonLabel;

            button.onClick.AddListener(OpenSaveSlotsPanel);
        }

        // 2) Create the panel (hidden by default).
        if (saveSlotsPanel == null)
        {
            Transform existingPanelT = mainMenuPanel.transform.Find("SaveSlotsPanel");
            saveSlotsPanel = existingPanelT != null ? existingPanelT.gameObject : null;
        }

        if (saveSlotsPanel == null)
        {
            saveSlotsPanel = new GameObject("SaveSlotsPanel", typeof(RectTransform), typeof(Image));
            saveSlotsPanel.transform.SetParent(mainMenuPanel.transform, false);

            RectTransform prt = saveSlotsPanel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(900f, 520f);
            prt.anchoredPosition = Vector2.zero;

            Image img = saveSlotsPanel.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.85f);

            CreateTextChild(saveSlotsPanel.transform, "SaveSlotsTitle", "Choose a save slot", new Vector2(0f, 170f), 38);

            float startY = 90f;
            float step = -90f;
            for (int i = 0; i < GameSaveSystem.SlotCount; i++)
            {
                int captured = i;
                string btnName = $"LoadSlot{i + 1}Button";

                GameObject buttonGO = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonGO.transform.SetParent(saveSlotsPanel.transform, false);

                RectTransform rt = buttonGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, startY + step * i);
                rt.sizeDelta = new Vector2(520f, 70f);

                Image bimg = buttonGO.GetComponent<Image>();
                bimg.color = new Color(0.18f, 0.24f, 0.32f, 1f);

                Button btn = buttonGO.GetComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = bimg.color;
                cb.highlightedColor = new Color(0.26f, 0.34f, 0.46f, 1f);
                cb.pressedColor = new Color(0.14f, 0.2f, 0.28f, 1f);
                btn.colors = cb;

                // Label
                GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGO.transform.SetParent(buttonGO.transform, false);
                RectTransform lrt = labelGO.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;

                Text txt = labelGO.GetComponent<Text>();
                txt.font = GetSafeFont();
                txt.fontSize = 28;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.text = "";

                btn.onClick.AddListener(() => LoadSaveSlot(captured));
            }

            // Back button
            Button backBtn = CreatePanelButton(saveSlotsPanel.transform, "BackButton", "Back", new Vector2(0f, -170f), new Vector2(300f, 60f));
            backBtn.onClick.AddListener(CloseSaveSlotsPanel);
        }

        saveSlotsPanel.SetActive(false);
    }

    private void ReflowMainMenuButtonsForLoadGame()
    {
        if (mainMenuPanel == null)
        {
            return;
        }

        Transform buttonsRoot = mainMenuPanel.transform.Find("ButtonsRoot");
        if (buttonsRoot == null)
        {
            return;
        }

        // Desired centers with 70px-tall buttons: no overlaps and enough vertical room.
        // Order: Start/HowTo/Options/NewGame/LoadGame/Quit
        SetButtonY(buttonsRoot, "StartGameButton", 280f);
        SetButtonY(buttonsRoot, "HowToButton", 210f);
        SetButtonY(buttonsRoot, "OptionsButton", 140f);
        SetButtonY(buttonsRoot, "NewGameButton", 70f);
        SetButtonY(buttonsRoot, "LoadSavesButton", 0f);
        SetButtonY(buttonsRoot, "QuitButton", -70f);

        // Our load button is created under buttonsRoot too (LoadSavesButton).
        // (Set above)
    }

    private void SetButtonY(Transform buttonsRoot, string buttonName, float y)
    {
        Transform t = buttonsRoot.Find(buttonName);
        if (t == null) return;

        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.sizeDelta = new Vector2(520f, 70f);
        rt.anchoredPosition = new Vector2(0f, y);
    }

    private void OpenSaveSlotsPanel()
    {
        EnsureSaveSlotsUI();

        if (saveSlotsPanel != null)
        {
            RefreshSaveSlotButtonLabels();
            saveSlotsPanel.SetActive(true);
        }
    }

    private void CloseSaveSlotsPanel()
    {
        if (saveSlotsPanel != null)
        {
            saveSlotsPanel.SetActive(false);
        }
    }

    private void RefreshSaveSlotButtonLabels()
    {
        if (saveSlotsPanel == null)
        {
            return;
        }

        for (int i = 0; i < GameSaveSystem.SlotCount; i++)
        {
            Transform btnT = saveSlotsPanel.transform.Find($"LoadSlot{i + 1}Button");
            if (btnT == null) continue;
            Text label = btnT.GetComponentInChildren<Text>();
            if (label == null) continue;

            GameSaveSystem.SaveSlotMeta meta = GameSaveSystem.GetSlotMeta(i);
            label.text = meta.exists ? $"Saved {meta.displayText}\n(Load Slot {i + 1})" : $"Empty\n(Load Slot {i + 1})";
        }
    }

    private void CreateTextChild(Transform parent, string name, string text, Vector2 anchoredPos, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(860f, 100f);

        Text t = go.GetComponent<Text>();
        t.font = GetSafeFont();
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = text;
    }

    private Button CreatePanelButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

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
        txt.font = GetSafeFont();
        txt.fontSize = 24;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = label;

        return btn;
    }

    private Font GetSafeFont()
    {
        try
        {
            return UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            return null;
        }
    }

    private static void StaticOnSceneLoadedForceCameras(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= StaticOnSceneLoadedForceCameras;

        // Cameras can be toggled/enabled during scene initialization; force now and next frame
        // using a temporary helper so we don't depend on MainMenuStartupScreen surviving.
        ForceCamerasToDesiredDisplay();

        GameObject helper = new GameObject("CameraDisplayFixTemp");
        Object.DontDestroyOnLoad(helper);
        helper.AddComponent<CameraDisplayFixTempRunner>().Init();
    }

    private class CameraDisplayFixTempRunner : MonoBehaviour
    {
        public void Init()
        {
            StartCoroutine(Run());
        }

        private System.Collections.IEnumerator Run()
        {
            yield return null; // next frame
            ForceCamerasToDesiredDisplay();
            Destroy(gameObject);
        }
    }

    private static void ForceCamerasToDesiredDisplay()
    {
        int desiredDisplay = 0;
        if (Display.displays != null && Display.displays.Length > 1 && Display.displays[1] != null)
        {
            desiredDisplay = 1;
        }

        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cams.Length; i++)
        {
            Camera c = cams[i];
            if (c == null) continue;

            c.gameObject.SetActive(true);
            c.enabled = true;

            c.targetDisplay = desiredDisplay;

            // Keep consistent 2D rendering defaults.
            c.orthographic = true;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.cullingMask = ~0;
        }
    }

#if UNITY_EDITOR
    private static void TryEnsureBuildSettingsViaReflection()
    {
        try
        {
            // Find the editor type named "SetupMainMenu" without a compile-time reference.
            System.Type setupType = null;
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var asm = assemblies[i];
                if (asm == null) continue;

                try
                {
                    var types = asm.GetTypes();
                    for (int t = 0; t < types.Length; t++)
                    {
                        var candidate = types[t];
                        if (candidate != null && candidate.Name == "SetupMainMenu")
                        {
                            setupType = candidate;
                            break;
                        }
                    }
                }
                catch
                {
                    // Some assemblies may throw on GetTypes; ignore and continue.
                }

                if (setupType != null) break;
            }

            if (setupType == null) return;

            var mi = setupType.GetMethod("EnsureBuildSettings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (mi != null)
            {
                mi.Invoke(null, null);
            }
        }
        catch
        {
            // Ignore; worst case, Unity just relies on your manual build profile setup.
        }
    }
#endif

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetStartupVisible(bool isVisible)
    {
        if (startupPanel != null)
        {
            startupPanel.SetActive(isVisible);
        }
    }

    private void SetMainMenuVisible(bool isVisible)
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(isVisible);
        }
    }
}
