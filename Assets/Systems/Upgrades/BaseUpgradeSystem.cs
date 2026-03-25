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
        ReapplyAllUpgradeEffects(false);
    }

    private void Start()
    {
        ReapplyAllUpgradeEffects(false);
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
