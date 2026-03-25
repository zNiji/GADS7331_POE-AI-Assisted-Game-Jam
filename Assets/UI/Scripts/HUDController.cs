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

        RefreshResources();
        ShowPrompt(string.Empty);
        SetExtractionStatus(string.Empty);
        SetBiome("Unknown Sector");
        SetPauseVisible(false);

        EnsureInventoryPauseMenu();
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
