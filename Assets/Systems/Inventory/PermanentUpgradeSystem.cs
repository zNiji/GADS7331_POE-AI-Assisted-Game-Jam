using System.Collections.Generic;
using UnityEngine;

public class PermanentUpgradeSystem : MonoBehaviour
{
    public static PermanentUpgradeSystem Instance { get; private set; }

    private readonly Dictionary<string, int> permanentUpgradeLevels = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetUpgradeLevel(string upgradeId)
    {
        if (string.IsNullOrWhiteSpace(upgradeId))
        {
            return 0;
        }

        return permanentUpgradeLevels.TryGetValue(upgradeId, out int level) ? level : 0;
    }

    public void AddUpgradeLevel(string upgradeId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(upgradeId) || amount <= 0)
        {
            return;
        }

        if (!permanentUpgradeLevels.ContainsKey(upgradeId))
        {
            permanentUpgradeLevels[upgradeId] = 0;
        }

        permanentUpgradeLevels[upgradeId] += amount;
    }

    public IReadOnlyDictionary<string, int> GetPermanentUpgrades()
    {
        return permanentUpgradeLevels;
    }
}
