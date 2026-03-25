using System.Collections.Generic;
using UnityEngine;

public class ExtractedResourceBank : MonoBehaviour
{
    public static ExtractedResourceBank Instance { get; private set; }

    private const string SaveKey = "extracted_resource_bank_v1";

    [System.Serializable]
    private class ResourceEntry
    {
        public string id;
        public int amount;
    }

    [System.Serializable]
    private class SaveData
    {
        public List<ResourceEntry> entries = new List<ResourceEntry>();
    }

    private readonly Dictionary<string, int> bankedResources = new Dictionary<string, int>();

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

    public void AddResources(IReadOnlyDictionary<string, int> resources)
    {
        if (resources == null)
        {
            return;
        }

        foreach (KeyValuePair<string, int> kvp in resources)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value <= 0)
            {
                continue;
            }

            if (!bankedResources.ContainsKey(kvp.Key))
            {
                bankedResources[kvp.Key] = 0;
            }

            bankedResources[kvp.Key] += kvp.Value;
        }

        Save();
    }

    public int GetAmount(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return 0;
        }

        return bankedResources.TryGetValue(resourceId, out int amount) ? amount : 0;
    }

    public bool TrySpendResource(string resourceId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(resourceId) || amount <= 0)
        {
            return false;
        }

        if (!bankedResources.TryGetValue(resourceId, out int existing) || existing < amount)
        {
            return false;
        }

        int remaining = existing - amount;
        if (remaining <= 0)
        {
            bankedResources.Remove(resourceId);
        }
        else
        {
            bankedResources[resourceId] = remaining;
        }

        Save();
        return true;
    }

    public IReadOnlyDictionary<string, int> GetAllBankedResources()
    {
        return bankedResources;
    }

    public void ReplaceBankedResources(IReadOnlyDictionary<string, int> resources)
    {
        bankedResources.Clear();

        if (resources != null)
        {
            foreach (KeyValuePair<string, int> kvp in resources)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value <= 0) continue;
                bankedResources[kvp.Key] = kvp.Value;
            }
        }

        Save();
    }

    public void ClearAllBankedResources(bool alsoDeletePersistedKey)
    {
        bankedResources.Clear();

        if (alsoDeletePersistedKey)
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }

    private void Save()
    {
        SaveData data = new SaveData();
        foreach (KeyValuePair<string, int> kvp in bankedResources)
        {
            data.entries.Add(new ResourceEntry
            {
                id = kvp.Key,
                amount = kvp.Value
            });
        }

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        bankedResources.Clear();

        if (!PlayerPrefs.HasKey(SaveKey))
        {
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null || data.entries == null)
        {
            return;
        }

        for (int i = 0; i < data.entries.Count; i++)
        {
            ResourceEntry entry = data.entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.amount <= 0)
            {
                continue;
            }

            bankedResources[entry.id] = entry.amount;
        }
    }
}
