using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeathUpgradeMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseUpgradeSystem upgradeSystem;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Button optionAButton;
    [SerializeField] private Button optionBButton;
    [SerializeField] private Button optionCButton;
    [SerializeField] private Text optionALabel;
    [SerializeField] private Text optionBLabel;
    [SerializeField] private Text optionCLabel;
    [SerializeField] private Button continueButton;

    private readonly List<UpgradeDefinition> currentChoices = new List<UpgradeDefinition>();
    private GameManager gameManager;
    private bool isVisible;

    private void Awake()
    {
        if (upgradeSystem == null)
        {
            upgradeSystem = FindAnyObjectByType<BaseUpgradeSystem>();
        }

        HideImmediate();
    }

    public void Show(GameManager owner)
    {
        gameManager = owner;
        if (upgradeSystem == null)
        {
            upgradeSystem = FindAnyObjectByType<BaseUpgradeSystem>();
        }

        BuildChoices();
        BindButtons();

        if (titleText != null)
        {
            titleText.text = "Run Failed";
        }

        if (descriptionText != null)
        {
            descriptionText.text = currentChoices.Count > 0
                ? "Choose one permanent upgrade before redeploying."
                : "No upgrades available. Continue to redeploy.";
        }

        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
        }
        isVisible = true;
    }

    public void HideImmediate()
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }
        isVisible = false;
    }

    private void Update()
    {
        if (!isVisible)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TrySelectIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TrySelectIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TrySelectIndex(2);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ContinueWithoutUpgrade();
        }
    }

    private void BuildChoices()
    {
        currentChoices.Clear();

        if (upgradeSystem == null || upgradeSystem.AvailableUpgrades == null)
        {
            return;
        }

        for (int i = 0; i < upgradeSystem.AvailableUpgrades.Count; i++)
        {
            UpgradeDefinition def = upgradeSystem.AvailableUpgrades[i];
            if (def == null || upgradeSystem.IsMaxLevel(def))
            {
                continue;
            }

            // Death upgrades must consume extracted/banked resources only.
            if (!upgradeSystem.CanAffordExtractedOnly(def))
            {
                continue;
            }

            currentChoices.Add(def);
            if (currentChoices.Count >= 3)
            {
                break;
            }
        }
    }

    private void BindButtons()
    {
        SetupOption(optionAButton, optionALabel, 0);
        SetupOption(optionBButton, optionBLabel, 1);
        SetupOption(optionCButton, optionCLabel, 2);

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(ContinueWithoutUpgrade);
        }
    }

    private void SetupOption(Button button, Text label, int index)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();

        if (index >= currentChoices.Count)
        {
            button.gameObject.SetActive(false);
            return;
        }

        UpgradeDefinition def = currentChoices[index];
        button.gameObject.SetActive(true);
        button.onClick.AddListener(() => SelectUpgrade(def));

        if (label != null)
        {
            int currentLevel = upgradeSystem != null ? upgradeSystem.GetCurrentLevel(def) : 0;
            label.text = def.displayName + "\n(Lv " + currentLevel + " -> " + (currentLevel + 1) + ")\n" + BuildCostText(def);
        }
    }

    private void SelectUpgrade(UpgradeDefinition definition)
    {
        if (upgradeSystem != null && definition != null)
        {
            // Uses extracted/banked resources only.
            upgradeSystem.TryPurchaseUpgradeExtractedOnly(definition);
        }

        ContinueWithoutUpgrade();
    }

    private void TrySelectIndex(int index)
    {
        if (index < 0 || index >= currentChoices.Count)
        {
            return;
        }

        SelectUpgrade(currentChoices[index]);
    }

    private void ContinueWithoutUpgrade()
    {
        HideImmediate();

        if (gameManager != null)
        {
            gameManager.CompleteDeathUpgradeAndRespawn();
        }
    }

    private static string BuildCostText(UpgradeDefinition definition)
    {
        if (definition == null || definition.resourceCosts == null || definition.resourceCosts.Count == 0)
        {
            return "Cost: Free";
        }

        string output = "Cost: ";
        bool wroteAny = false;
        for (int i = 0; i < definition.resourceCosts.Count; i++)
        {
            UpgradeDefinition.ResourceCost cost = definition.resourceCosts[i];
            if (cost == null || string.IsNullOrWhiteSpace(cost.resourceId) || cost.amount <= 0)
            {
                continue;
            }

            if (wroteAny)
            {
                output += ", ";
            }

            output += cost.resourceId + " x" + cost.amount;
            wroteAny = true;
        }

        return wroteAny ? output : "Cost: Free";
    }
}
