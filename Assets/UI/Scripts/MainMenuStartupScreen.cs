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

        SetStartupVisible(true);
        SetMainMenuVisible(false);
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
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogWarning("Gameplay scene name is not set.");
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

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
