using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public int count;
    }

    [SerializeField] private List<InventoryEntry> startingItems = new List<InventoryEntry>();

    private readonly Dictionary<string, int> items = new Dictionary<string, int>();

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (InventoryEntry entry in startingItems)
        {
            if (string.IsNullOrWhiteSpace(entry.itemId) || entry.count <= 0)
            {
                continue;
            }

            AddItem(entry.itemId, entry.count, false);
        }
    }

    public void AddItem(string itemId, int amount = 1, bool notify = true)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return;
        }

        if (!items.ContainsKey(itemId))
        {
            items[itemId] = 0;
        }

        items[itemId] += amount;

        if (notify)
        {
            OnInventoryChanged?.Invoke();
        }
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (!items.ContainsKey(itemId) || amount <= 0)
        {
            return false;
        }

        if (items[itemId] < amount)
        {
            return false;
        }

        items[itemId] -= amount;
        if (items[itemId] == 0)
        {
            items.Remove(itemId);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool SpendResource(string itemId, int amount = 1)
    {
        return RemoveItem(itemId, amount);
    }

    public bool HasResource(string itemId, int minimumAmount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || minimumAmount <= 0)
        {
            return false;
        }

        return GetCount(itemId) >= minimumAmount;
    }

    public int GetCount(string itemId)
    {
        return items.TryGetValue(itemId, out int count) ? count : 0;
    }

    public IReadOnlyDictionary<string, int> GetItems()
    {
        return items;
    }

    public Dictionary<string, int> GetSnapshot()
    {
        return new Dictionary<string, int>(items);
    }

    public void ClearRunResources()
    {
        if (items.Count == 0)
        {
            return;
        }

        items.Clear();
        OnInventoryChanged?.Invoke();
    }
}
