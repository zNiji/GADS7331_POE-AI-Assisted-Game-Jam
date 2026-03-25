using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Events;
using UnityEngine.Events;

public static class SetupMainMenu
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/Level_01.unity";

    [MenuItem("Tools/Frontier Extraction/Ensure Build Settings (MainMenu + Level_01)")]
    public static void EnsureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        void AddIfMissing(string path)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == path)
                {
                    scenes[i].enabled = true;
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        AddIfMissing(MainMenuScenePath);
        AddIfMissing(GameplayScenePath);

        EditorBuildSettings.scenes = scenes.ToArray();

        // Unity 6 uses Build Profiles for scene inclusion.
        // If an active build profile exists, update it too so SceneManager.LoadScene works.
        try
        {
            BuildProfile activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile != null)
            {
                activeProfile.overrideGlobalScenes = true;
                activeProfile.scenes = scenes.ToArray();
                EditorUtility.SetDirty(activeProfile);
                AssetDatabase.SaveAssets();
            }
        }
        catch
        {
            // Fall back silently to EditorBuildSettings only.
        }

        Debug.Log("Build settings updated: MainMenu + Level_01 enabled.");
    }

    [MenuItem("Tools/Frontier Extraction/Generate Main Menu UI")]
    public static void GenerateUi()
    {
        EnsureBuildSettings();

        if (!System.IO.File.Exists(MainMenuScenePath))
        {
            Debug.LogError("Missing scene: " + MainMenuScenePath);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        if (!scene.isLoaded)
        {
            Debug.LogError("Failed to open: " + MainMenuScenePath);
            return;
        }

        GameObject controllerGO = GameObject.Find("MainMenuController");
        if (controllerGO == null)
        {
            Debug.LogError("Couldn't find MainMenuController in MainMenu scene.");
            return;
        }

        MainMenuStartupScreen startup = controllerGO.GetComponent<MainMenuStartupScreen>();
        if (startup == null)
        {
            Debug.LogError("MainMenuController missing MainMenuStartupScreen component.");
            return;
        }

        // Panels are already referenced by MainMenuStartupScreen.
        GameObject startupPanel = controllerGO.transform.Find("StartupPanel") != null
            ? controllerGO.transform.Find("StartupPanel").gameObject
            : FindByNameInScene("StartupPanel");

        GameObject mainMenuPanel = controllerGO.transform.Find("MainMenuPanel") != null
            ? controllerGO.transform.Find("MainMenuPanel").gameObject
            : FindByNameInScene("MainMenuPanel");

        if (startupPanel == null || mainMenuPanel == null)
        {
            Debug.LogError("Missing StartupPanel/MainMenuPanel objects in MainMenu scene.");
            return;
        }

        // Add a MainMenuUIController for how-to/options pages.
        MainMenuUIController uiController = controllerGO.GetComponent<MainMenuUIController>();
        if (uiController == null)
        {
            uiController = controllerGO.AddComponent<MainMenuUIController>();
        }

        // Build startup content
        EnsureTextChild(startupPanel.transform, "StartupText", "Frontier Extraction\nPress Any Key", new Vector2(0f, 20f), 44);

        // Build main menu buttons
        GameObject buttonsRoot = EnsureChild(mainMenuPanel.transform, "ButtonsRoot");
        // Anchor buttons around the vertical center so they stay visible on different resolutions.
        EnsureButton(buttonsRoot.transform, "StartGameButton", "Start Game", new Vector2(0f, 140f), new UnityAction(startup.StartGame));
        EnsureButton(buttonsRoot.transform, "HowToButton", "How To Play", new Vector2(0f, 70f), new UnityAction(uiController.ShowHowTo));
        EnsureButton(buttonsRoot.transform, "OptionsButton", "Options", new Vector2(0f, 0f), new UnityAction(uiController.ShowOptions));
        EnsureButton(buttonsRoot.transform, "QuitButton", "Quit", new Vector2(0f, -90f), new UnityAction(startup.QuitGame));

        // HowTo + Options panels
        GameObject howTo = EnsurePanel(mainMenuPanel.transform, "HowToPanel", new Vector2(0f, -10f), new Vector2(900f, 600f));
        howTo.SetActive(false);
        EnsureTextChild(howTo.transform, "HowToTitle", "How To Play", new Vector2(0f, 220f), 42);
        EnsureTextChild(howTo.transform, "HowToBody",
            "Controls:\n- A/D: Move\n- Space: Jump\n- Left Click: Shoot\n- E: Mine\n- X: Extract\n\nTip: Extract before dying to keep resources.",
            new Vector2(0f, 70f), 26);
        EnsureButton(howTo.transform, "HowToBackButton", "Back", new Vector2(0f, -250f), new UnityAction(uiController.ReturnToMainMenu));

        GameObject options = EnsurePanel(mainMenuPanel.transform, "OptionsPanel", new Vector2(0f, -10f), new Vector2(900f, 600f));
        options.SetActive(false);
        EnsureTextChild(options.transform, "OptionsTitle", "Options", new Vector2(0f, 220f), 42);
        EnsureTextChild(options.transform, "OptionsBody", "Coming soon (volume, controls, etc).", new Vector2(0f, 70f), 26);
        EnsureButton(options.transform, "OptionsBackButton", "Back", new Vector2(0f, -250f), new UnityAction(uiController.ReturnToMainMenu));

        // MainMenuUIController finds panels by name in Awake (HowToPanel, OptionsPanel).

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Main Menu UI generated.");
    }

    private static GameObject FindByNameInScene(string name)
    {
        Object[] objs = Object.FindObjectsOfType(typeof(GameObject));
        for (int i = 0; i < objs.Length; i++)
        {
            if (objs[i] is GameObject go && go != null && go.name == name)
            {
                return go;
            }
        }
        return null;
    }

    private static GameObject EnsureChild(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        if (t != null) return t.gameObject;

        GameObject go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        return go;
    }

    private static GameObject EnsurePanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        Transform existing = parent.Find(name);
        GameObject panel;
        if (existing != null)
        {
            panel = existing.gameObject;
        }
        else
        {
            panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
        }

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = panel.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.7f);
        return panel;
    }

    private static void EnsureTextChild(Transform parent, string name, string text, Vector2 anchoredPos, int fontSize)
    {
        Transform existing = parent.Find(name);
        Text t;
        if (existing != null && existing.GetComponent<Text>() != null)
        {
            t = existing.GetComponent<Text>();
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            t = go.GetComponent<Text>();
        }

        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(860f, 200f);

        t.text = text;
        t.font = GetSafeFont();
        t.fontSize = fontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
    }

    private static Font GetSafeFont()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        return f;
    }

    private static void EnsureButton(Transform parent, string name, string label, Vector2 anchoredPos, UnityAction onClick)
    {
        Transform existing = parent.Find(name);
        GameObject buttonGO;
        Button button;
        if (existing != null)
        {
            buttonGO = existing.gameObject;
            button = buttonGO.GetComponent<Button>();
        }
        else
        {
            buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);
            button = buttonGO.GetComponent<Button>();
        }

        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(520f, 70f);

        Image img = buttonGO.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0.18f, 0.24f, 0.32f, 1f);
        }

        button.onClick.RemoveAllListeners();
        if (onClick != null)
        {
            UnityEventTools.AddPersistentListener(button.onClick, onClick);
        }

        Text text = null;
        Transform textT = buttonGO.transform.Find("Text");
        if (textT != null) text = textT.GetComponent<Text>();
        if (text == null)
        {
            GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(buttonGO.transform, false);
            text = textGO.GetComponent<Text>();
        }

        RectTransform trt = text.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = Vector2.zero;

        text.font = GetSafeFont();
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
    }
}

