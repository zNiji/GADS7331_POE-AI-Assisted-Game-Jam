using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Player References")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Suit UI")]
    [SerializeField] private Slider suitIntegritySlider;
    [SerializeField] private Slider oxygenSlider;
    [SerializeField] private Text suitIntegrityLabel;
    [SerializeField] private Text oxygenLabel;

    [Header("Resource UI")]
    [SerializeField] private Text resourcesText;
    [SerializeField] private string[] trackedResourceIds = { "Iron", "Crystal", "FuelCell" };

    [Header("Context UI")]
    [SerializeField] private Text biomeLabel;
    [SerializeField] private Text promptText;
    [SerializeField] private Text extractionStatusText;
    [SerializeField] private GameObject pausePanel;

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
        }

        if (playerStats != null)
        {
            playerStats.OnHealthChanged += OnHealthChanged;
            playerStats.OnOxygenChanged += OnOxygenChanged;
            OnHealthChanged(playerStats.CurrentHealth, playerStats.MaxHealth);
            OnOxygenChanged(playerStats.CurrentOxygen, playerStats.MaxOxygen);
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += RefreshResources;
        }

        // Keep HUD resource list aligned with actual mineable resources.
        trackedResourceIds = new[] { "Iron", "Crystal", "Uranium" };

        RefreshResources();
        ShowPrompt(string.Empty);
        SetExtractionStatus(string.Empty);
        SetBiome("Jungle");
        SetPauseVisible(false);

        EnsureInventoryPauseMenu();
        ApplyReadableTextStyle();
    }

    private void ApplyReadableTextStyle()
    {
        // Improve legibility over busy bioluminescent backgrounds.
        StyleText(suitIntegrityLabel);
        StyleText(oxygenLabel);
        StyleText(resourcesText);
        StyleText(biomeLabel);
        StyleText(promptText);
        StyleText(extractionStatusText);
    }

    private static void StyleText(Text text)
    {
        if (text == null) return;

        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.resizeTextForBestFit = false;

        Outline outline = text.GetComponent<Outline>();
        if (outline == null) outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;

        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(0.6f, -0.6f);
        shadow.useGraphicAlpha = true;
    }

    private void EnsureInventoryPauseMenu()
    {
        if (pausePanel == null)
        {
            return;
        }

        if (pausePanel.GetComponent<InventoryPauseMenuUI>() == null)
        {
            pausePanel.AddComponent<InventoryPauseMenuUI>();
        }

        if (pausePanel.GetComponent<PauseMenuReturnToMainMenuUI>() == null)
        {
            pausePanel.AddComponent<PauseMenuReturnToMainMenuUI>();
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= OnHealthChanged;
            playerStats.OnOxygenChanged -= OnOxygenChanged;
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= RefreshResources;
        }
    }

    public void SetBiome(string biomeName)
    {
        if (biomeLabel == null)
        {
            return;
        }

        biomeLabel.text = "Biome: " + biomeName;
    }

    public void ShowPrompt(string message)
    {
        if (promptText == null)
        {
            return;
        }

        promptText.text = message;
        promptText.enabled = !string.IsNullOrWhiteSpace(message);
    }

    public void SetPauseVisible(bool isVisible)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(isVisible);
        }
    }

    public void SetExtractionStatus(string message)
    {
        if (extractionStatusText == null)
        {
            return;
        }

        extractionStatusText.text = message;
        extractionStatusText.enabled = !string.IsNullOrWhiteSpace(message);
    }

    public void RefreshResources()
    {
        if (resourcesText == null)
        {
            return;
        }

        if (InventorySystem.Instance == null)
        {
            resourcesText.text = "Resources: --";
            return;
        }

        string output = "Resources";
        for (int i = 0; i < trackedResourceIds.Length; i++)
        {
            string id = trackedResourceIds[i];
            int count = InventorySystem.Instance.GetCount(id);
            output += "\n" + id + ": " + count;
        }

        resourcesText.text = output;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (suitIntegritySlider != null && max > 0f)
        {
            suitIntegritySlider.value = Mathf.Clamp01(current / max);
        }

        if (suitIntegrityLabel != null)
        {
            suitIntegrityLabel.text = "Suit: " + Mathf.CeilToInt(current) + "/" + Mathf.CeilToInt(max);
        }
    }

    private void OnOxygenChanged(float current, float max)
    {
        if (oxygenSlider != null && max > 0f)
        {
            oxygenSlider.value = Mathf.Clamp01(current / max);
        }

        if (oxygenLabel != null)
        {
            oxygenLabel.text = "O2: " + Mathf.CeilToInt(current) + "%";
        }
    }
}
