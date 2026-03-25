using System.Collections.Generic;
using UnityEngine;

public class PermanentUpgradeSystem : MonoBehaviour
{
    public static PermanentUpgradeSystem Instance { get; private set; }

    private const string SaveKey = "permanent_upgrades_v1";

    [System.Serializable]
    private class UpgradeLevelEntry
    {
        public string id;
        public int level;
    }

    [System.Serializable]
    private class UpgradeSaveData
    {
        public List<UpgradeLevelEntry> entries = new List<UpgradeLevelEntry>();
    }

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
        Load();
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
        Save();
    }

    public void SetUpgradeLevel(string upgradeId, int level)
    {
        if (string.IsNullOrWhiteSpace(upgradeId))
        {
            return;
        }

        permanentUpgradeLevels[upgradeId] = Mathf.Max(0, level);
        Save();
    }

    public IReadOnlyDictionary<string, int> GetPermanentUpgrades()
    {
        return permanentUpgradeLevels;
    }

    private void Save()
    {
        UpgradeSaveData data = new UpgradeSaveData();
        foreach (KeyValuePair<string, int> kvp in permanentUpgradeLevels)
        {
            data.entries.Add(new UpgradeLevelEntry
            {
                id = kvp.Key,
                level = kvp.Value
            });
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        permanentUpgradeLevels.Clear();

        if (!PlayerPrefs.HasKey(SaveKey))
        {
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        UpgradeSaveData data = JsonUtility.FromJson<UpgradeSaveData>(json);
        if (data == null || data.entries == null)
        {
            return;
        }

        for (int i = 0; i < data.entries.Count; i++)
        {
            UpgradeLevelEntry entry = data.entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
            {
                continue;
            }

            permanentUpgradeLevels[entry.id] = Mathf.Max(0, entry.level);
        }
    }

    public void ClearAllPermanentUpgrades(bool alsoDeletePersistedKey)
    {
        permanentUpgradeLevels.Clear();

        if (alsoDeletePersistedKey)
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
        else
        {
            // Keep key, but update it to an empty set.
            Save();
        }

        // Reapply so any currently active scene reflects the reset.
        if (BaseUpgradeSystem.Instance != null)
        {
            BaseUpgradeSystem.Instance.ReapplyAllUpgradeEffects(true);
        }
    }
}
