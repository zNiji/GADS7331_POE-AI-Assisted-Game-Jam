using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeEffectType
{
    IncreaseMaxHealth,
    IncreaseMiningSpeed,
    IncreaseDamage,
    IncreaseStartingAmmo
}

[CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "Game/Upgrades/Upgrade Definition")]
public class UpgradeDefinition : ScriptableObject
{
    [Serializable]
    public class ResourceCost
    {
        public string resourceId = "Iron";
        public int amount = 1;
    }

    [Header("Identity")]
    public string upgradeId = "upgrade.max_health";
    public string displayName = "Max Health";
    [TextArea] public string description = "Increase player survivability.";

    [Header("Progression")]
    public UpgradeEffectType effectType = UpgradeEffectType.IncreaseMaxHealth;
    public int effectAmountPerLevel = 10;
    public int maxLevel = 5;

    [Header("Cost")]
    public List<ResourceCost> resourceCosts = new List<ResourceCost>();
}
