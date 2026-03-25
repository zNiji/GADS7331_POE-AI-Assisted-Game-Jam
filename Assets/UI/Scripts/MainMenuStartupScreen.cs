using UnityEngine;
using UnityEngine.SceneManagement;

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
