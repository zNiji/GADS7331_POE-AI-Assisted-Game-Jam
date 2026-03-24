using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseUpgradeSystem : MonoBehaviour
{
    public static BaseUpgradeSystem Instance { get; private set; }

    [SerializeField] private List<UpgradeDefinition> availableUpgrades = new List<UpgradeDefinition>();
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerUpgradeEffects playerUpgradeEffects;

    private readonly Dictionary<string, int> currentLevels = new Dictionary<string, int>();

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

    public int GetCurrentLevel(UpgradeDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.upgradeId))
        {
            return 0;
        }

        return currentLevels.TryGetValue(definition.upgradeId, out int level) ? level : 0;
    }

    public bool IsMaxLevel(UpgradeDefinition definition)
    {
        return definition != null && GetCurrentLevel(definition) >= definition.maxLevel;
    }

    public bool CanAfford(UpgradeDefinition definition)
    {
        if (definition == null || InventorySystem.Instance == null)
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

    public bool TryPurchaseUpgrade(UpgradeDefinition definition)
    {
        if (definition == null || IsMaxLevel(definition) || !CanAfford(definition))
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

        int newLevel = GetCurrentLevel(definition) + 1;
        currentLevels[definition.upgradeId] = newLevel;
        ApplyEffect(definition);
        OnUpgradesChanged?.Invoke();
        return true;
    }

    private void ApplyEffect(UpgradeDefinition definition)
    {
        ResolveReferences();
        int amount = Mathf.Max(0, definition.effectAmountPerLevel);

        switch (definition.effectType)
        {
            case UpgradeEffectType.IncreaseMaxHealth:
                if (playerStats != null)
                {
                    playerStats.AddMaxHealth(amount, true);
                }
                break;
            case UpgradeEffectType.IncreaseMiningSpeed:
                if (playerUpgradeEffects != null)
                {
                    playerUpgradeEffects.AddMiningPowerBonus(amount);
                }
                break;
            case UpgradeEffectType.IncreaseDamage:
                if (playerUpgradeEffects != null)
                {
                    playerUpgradeEffects.AddDamageBonus(amount);
                }
                break;
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
