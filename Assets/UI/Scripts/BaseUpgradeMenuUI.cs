using System.Text;
using UnityEngine;

public class BaseUpgradeMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseUpgradeSystem upgradeSystem;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private UpgradeOptionRowUI rowPrefab;

    [Header("Input")]
    [SerializeField] private KeyCode toggleMenuKey = KeyCode.B;

    private readonly System.Collections.Generic.List<UpgradeOptionRowUI> rows =
        new System.Collections.Generic.List<UpgradeOptionRowUI>();

    private bool isOpen;

    private void Awake()
    {
        if (upgradeSystem == null)
        {
            upgradeSystem = FindAnyObjectByType<BaseUpgradeSystem>();
        }
    }

    private void OnEnable()
    {
        if (upgradeSystem != null)
        {
            upgradeSystem.OnUpgradesChanged += Refresh;
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += Refresh;
        }

        SetMenuOpen(false);
        BuildRows();
        Refresh();
    }

    private void OnDisable()
    {
        if (upgradeSystem != null)
        {
            upgradeSystem.OnUpgradesChanged -= Refresh;
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleMenuKey))
        {
            SetMenuOpen(!isOpen);
        }
    }

    public void SetMenuOpen(bool open)
    {
        isOpen = open;
        if (menuRoot != null)
        {
            menuRoot.SetActive(open);
        }

        if (isOpen)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (upgradeSystem == null)
        {
            return;
        }

        BuildRows();

        for (int i = 0; i < upgradeSystem.AvailableUpgrades.Count; i++)
        {
            UpgradeDefinition definition = upgradeSystem.AvailableUpgrades[i];
            if (definition == null || i >= rows.Count)
            {
                continue;
            }

            int level = upgradeSystem.GetCurrentLevel(definition);
            bool isMaxLevel = upgradeSystem.IsMaxLevel(definition);
            bool canAfford = upgradeSystem.CanAfford(definition);
            string costsText = BuildCostText(definition, level);

            rows[i].Bind(
                definition,
                level,
                canAfford,
                isMaxLevel,
                costsText,
                () => upgradeSystem.TryPurchaseUpgrade(definition)
            );
        }
    }

    private void BuildRows()
    {
        if (upgradeSystem == null || rowContainer == null || rowPrefab == null)
        {
            return;
        }

        while (rows.Count < upgradeSystem.AvailableUpgrades.Count)
        {
            UpgradeOptionRowUI created = Instantiate(rowPrefab, rowContainer);
            rows.Add(created);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            bool shouldBeVisible = i < upgradeSystem.AvailableUpgrades.Count;
            rows[i].gameObject.SetActive(shouldBeVisible);
        }
    }

    private string BuildCostText(UpgradeDefinition definition, int currentLevel)
    {
        if (definition == null || definition.resourceCosts == null || definition.resourceCosts.Count == 0)
        {
            return (upgradeSystem != null && upgradeSystem.RequiresFinalTierOreForNextLevel(definition))
                ? "Cost: Zenithite x1"
                : "Cost: Free";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("Cost: ");

        for (int i = 0; i < definition.resourceCosts.Count; i++)
        {
            UpgradeDefinition.ResourceCost cost = definition.resourceCosts[i];
            if (string.IsNullOrWhiteSpace(cost.resourceId) || cost.amount <= 0)
            {
                continue;
            }

            if (builder.Length > 6)
            {
                builder.Append(", ");
            }

            builder.Append(cost.resourceId);
            builder.Append(" x");
            builder.Append(cost.amount);
        }

        if (upgradeSystem != null && upgradeSystem.RequiresFinalTierOreForNextLevel(definition))
        {
            if (builder.Length > 6)
            {
                builder.Append(", ");
            }
            builder.Append("Zenithite x1");
        }

        return builder.ToString();
    }
}
