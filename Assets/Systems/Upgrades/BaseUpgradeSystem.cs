using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseUpgradeSystem : MonoBehaviour
{
    public static BaseUpgradeSystem Instance { get; private set; }

    [SerializeField] private List<UpgradeDefinition> availableUpgrades = new List<UpgradeDefinition>();
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerUpgradeEffects playerUpgradeEffects;

    public event Action OnUpgradesChanged;

    public IReadOnlyList<UpgradeDefinition> AvailableUpgrades => availableUpgrades;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResolveReferences();
    }

    private void Start()
    {
        EnsureDefaultUpgrades();

        // On initial scene load we want current health to match the (possibly upgraded) max health.
        ReapplyAllUpgradeEffects(true);
    }

    private void EnsureDefaultUpgrades()
    {
        if (availableUpgrades == null)
        {
            availableUpgrades = new List<UpgradeDefinition>();
        }

        // Ensure we always have 3 baseline upgrade options for the death menu.
        // (Some prefabs/scenes may serialize this list empty; without this, only 1-2 upgrades show up.)
        bool hasHealth = HasUpgradeId("upgrade.max_health");
        bool hasDamage = HasUpgradeId("upgrade.damage");
        bool hasMining = HasUpgradeId("upgrade.mining");

        if (hasHealth && hasDamage && hasMining)
        {
            return;
        }

        if (!hasHealth)
        {
            availableUpgrades.Add(BuildHealthUpgrade());
        }

        if (!hasDamage)
        {
            availableUpgrades.Add(BuildDamageUpgrade());
        }

        if (!hasMining)
        {
            availableUpgrades.Add(BuildMiningUpgrade());
        }
    }

    private bool HasUpgradeId(string upgradeId)
    {
        if (string.IsNullOrWhiteSpace(upgradeId))
        {
            return false;
        }

        if (availableUpgrades == null)
        {
            return false;
        }

        for (int i = 0; i < availableUpgrades.Count; i++)
        {
            UpgradeDefinition def = availableUpgrades[i];
            if (def != null && def.upgradeId == upgradeId)
            {
                return true;
            }
        }

        return false;
    }

    private static UpgradeDefinition BuildHealthUpgrade()
    {
        UpgradeDefinition def = ScriptableObject.CreateInstance<UpgradeDefinition>();
        def.name = "RuntimeDefault_Health";
        def.upgradeId = "upgrade.max_health";
        def.displayName = "Max Health";
        def.description = "Increase suit integrity for longer runs.";
        def.effectType = UpgradeEffectType.IncreaseMaxHealth;
        def.effectAmountPerLevel = 15;
        def.maxLevel = 5;
        def.resourceCosts = new List<UpgradeDefinition.ResourceCost>
        {
            new UpgradeDefinition.ResourceCost { resourceId = "Iron", amount = 3 },
            new UpgradeDefinition.ResourceCost { resourceId = "Crystal", amount = 1 }
        };
        return def;
    }

    private static UpgradeDefinition BuildDamageUpgrade()
    {
        UpgradeDefinition def = ScriptableObject.CreateInstance<UpgradeDefinition>();
        def.name = "RuntimeDefault_Damage";
        def.upgradeId = "upgrade.damage";
        def.displayName = "Weapon Damage";
        def.description = "Increase bullet and melee damage.";
        def.effectType = UpgradeEffectType.IncreaseDamage;
        // Stronger per level than the old FuelCell-based definition.
        def.effectAmountPerLevel = 2;
        def.maxLevel = 5;
        // Balance: use only extracted resources (Iron/Crystal), since FuelCell may be unavailable.
        def.resourceCosts = new List<UpgradeDefinition.ResourceCost>
        {
            new UpgradeDefinition.ResourceCost { resourceId = "Iron", amount = 4 },
            new UpgradeDefinition.ResourceCost { resourceId = "Crystal", amount = 1 }
        };
        return def;
    }

    private static UpgradeDefinition BuildMiningUpgrade()
    {
        UpgradeDefinition def = ScriptableObject.CreateInstance<UpgradeDefinition>();
        def.name = "RuntimeDefault_Mining";
        def.upgradeId = "upgrade.mining";
        def.displayName = "Mining Power";
        def.description = "Mine resource nodes faster.";
        def.effectType = UpgradeEffectType.IncreaseMiningSpeed;
        def.effectAmountPerLevel = 1;
        def.maxLevel = 5;
        def.resourceCosts = new List<UpgradeDefinition.ResourceCost>
        {
            new UpgradeDefinition.ResourceCost { resourceId = "Iron", amount = 2 },
            new UpgradeDefinition.ResourceCost { resourceId = "Crystal", amount = 2 }
        };
        return def;
    }

    public int GetCurrentLevel(UpgradeDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.upgradeId) || PermanentUpgradeSystem.Instance == null)
        {
            return 0;
        }

        return PermanentUpgradeSystem.Instance.GetUpgradeLevel(definition.upgradeId);
    }

    public bool IsMaxLevel(UpgradeDefinition definition)
    {
        return definition != null && GetCurrentLevel(definition) >= definition.maxLevel;
    }

    public bool CanAfford(UpgradeDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }
        return CanAffordWithBank(definition) || CanAffordWithRunInventory(definition);
    }

    public bool TryPurchaseUpgrade(UpgradeDefinition definition)
    {
        if (definition == null || IsMaxLevel(definition) || !CanAfford(definition) || PermanentUpgradeSystem.Instance == null)
        {
            return false;
        }
        // Prefer banked resources when available, otherwise spend run inventory.
        return CanAffordWithBank(definition)
            ? TryPurchaseFromBank(definition)
            : TryPurchaseFromRunInventory(definition);
    }

    // Death menu: ONLY spend extracted/banked resources (never run inventory).
    public bool CanAffordExtractedOnly(UpgradeDefinition definition)
    {
        return definition != null && CanAffordWithBank(definition);
    }

    public bool TryPurchaseUpgradeExtractedOnly(UpgradeDefinition definition)
    {
        if (definition == null || IsMaxLevel(definition) || PermanentUpgradeSystem.Instance == null)
        {
            return false;
        }

        if (!CanAffordWithBank(definition))
        {
            return false;
        }

        return TryPurchaseFromBank(definition);
    }

    private bool CanAffordWithBank(UpgradeDefinition definition)
    {
        ExtractedResourceBank bank = ExtractedResourceBank.Instance;
        if (bank == null)
        {
            return false;
        }

        for (int i = 0; i < definition.resourceCosts.Count; i++)
        {
            UpgradeDefinition.ResourceCost cost = definition.resourceCosts[i];
            if (string.IsNullOrWhiteSpace(cost.resourceId) || cost.amount <= 0)
            {
                continue;
            }

            if (bank.GetAmount(cost.resourceId) < cost.amount)
            {
                return false;
            }
        }

        return true;
    }

    private bool CanAffordWithRunInventory(UpgradeDefinition definition)
    {
        if (InventorySystem.Instance == null)
        {
            return false;
        }

        for (int i = 0; i < definition.resourceCosts.Count; i++)
        {
            UpgradeDefinition.ResourceCost cost = definition.resourceCosts[i];
            if (string.IsNullOrWhiteSpace(cost.resourceId) || cost.amount <= 0)
            {
                continue;
            }

            if (!InventorySystem.Instance.HasResource(cost.resourceId, cost.amount))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryPurchaseFromBank(UpgradeDefinition definition)
    {
        ExtractedResourceBank bank = ExtractedResourceBank.Instance;
        if (bank == null)
        {
            return false;
        }

        for (int i = 0; i < definition.resourceCosts.Count; i++)
        {
            UpgradeDefinition.ResourceCost cost = definition.resourceCosts[i];
            if (string.IsNullOrWhiteSpace(cost.resourceId) || cost.amount <= 0)
            {
                continue;
            }

            if (!bank.TrySpendResource(cost.resourceId, cost.amount))
            {
                return false;
            }
        }

        PermanentUpgradeSystem.Instance.AddUpgradeLevel(definition.upgradeId, 1);
        ReapplyAllUpgradeEffects(false);
        OnUpgradesChanged?.Invoke();
        return true;
    }

    private bool TryPurchaseFromRunInventory(UpgradeDefinition definition)
    {
        if (InventorySystem.Instance == null)
        {
            return false;
        }

        for (int i = 0; i < definition.resourceCosts.Count; i++)
        {
            UpgradeDefinition.ResourceCost cost = definition.resourceCosts[i];
            if (string.IsNullOrWhiteSpace(cost.resourceId) || cost.amount <= 0)
            {
                continue;
            }

            InventorySystem.Instance.SpendResource(cost.resourceId, cost.amount);
        }

        PermanentUpgradeSystem.Instance.AddUpgradeLevel(definition.upgradeId, 1);
        ReapplyAllUpgradeEffects(false);
        OnUpgradesChanged?.Invoke();
        return true;
    }

    public bool TryApplyFreeUpgrade(UpgradeDefinition definition)
    {
        if (definition == null || IsMaxLevel(definition) || PermanentUpgradeSystem.Instance == null)
        {
            return false;
        }

        PermanentUpgradeSystem.Instance.AddUpgradeLevel(definition.upgradeId, 1);
        ReapplyAllUpgradeEffects(false);
        OnUpgradesChanged?.Invoke();
        return true;
    }

    public void ReapplyAllUpgradeEffects(bool healToFull)
    {
        ResolveReferences();

        int totalHealthBonus = 0;
        int totalMiningBonus = 0;
        int totalDamageBonus = 0;

        for (int i = 0; i < availableUpgrades.Count; i++)
        {
            UpgradeDefinition definition = availableUpgrades[i];
            if (definition == null)
            {
                continue;
            }

            int level = GetCurrentLevel(definition);
            int amount = Mathf.Max(0, definition.effectAmountPerLevel) * Mathf.Max(0, level);

            switch (definition.effectType)
            {
                case UpgradeEffectType.IncreaseMaxHealth:
                    totalHealthBonus += amount;
                    break;
                case UpgradeEffectType.IncreaseMiningSpeed:
                    totalMiningBonus += amount;
                    break;
                case UpgradeEffectType.IncreaseDamage:
                    totalDamageBonus += amount;
                    break;
            }
        }

        if (playerStats != null)
        {
            playerStats.SetMaxHealthFromBonus(totalHealthBonus, healToFull);
        }

        if (playerUpgradeEffects != null)
        {
            playerUpgradeEffects.SetMiningPowerBonus(totalMiningBonus);
            playerUpgradeEffects.SetDamageBonus(totalDamageBonus);
        }
    }

    private void ResolveReferences()
    {
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
        }

        if (playerUpgradeEffects == null)
        {
            playerUpgradeEffects = FindAnyObjectByType<PlayerUpgradeEffects>();
        }
    }
}
