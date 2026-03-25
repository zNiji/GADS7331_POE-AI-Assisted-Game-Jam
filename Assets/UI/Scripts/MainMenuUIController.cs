using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIController : MonoBehaviour
{
    [SerializeField] private GameObject howToPanel;
    [SerializeField] private GameObject optionsPanel;

    private MainMenuStartupScreen startupScreen;

    private void Awake()
    {
        startupScreen = GetComponent<MainMenuStartupScreen>();

        if (howToPanel == null)
        {
            // The panels live under MainMenuPanel (not necessarily under this controller),
            // so fall back to a scene-wide lookup by name.
            howToPanel = transform.Find("HowToPanel") != null ? transform.Find("HowToPanel").gameObject : GameObject.Find("HowToPanel");
        }

        if (optionsPanel == null)
        {
            optionsPanel = transform.Find("OptionsPanel") != null ? transform.Find("OptionsPanel").gameObject : GameObject.Find("OptionsPanel");
        }
    }

    private void Start()
    {
        HideAll();
    }

    private void HideAll()
    {
        if (howToPanel != null) howToPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void ShowHowTo()
    {
        HideAll();
        if (howToPanel != null) howToPanel.SetActive(true);
    }

    public void ShowOptions()
    {
        HideAll();
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        HideAll();
        if (startupScreen != null)
        {
            startupScreen.ShowMainMenu();
        }
    }
}

