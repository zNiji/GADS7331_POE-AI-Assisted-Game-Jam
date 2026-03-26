using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSaveSystem
{
    public const int SlotCount = 3;
    private const string SaveVersion = "1";

    // Scene-load handoff: MainMenu selects a slot, GameManager applies it after ResetRun().
    private static int pendingLoadSlotIndex = -1;

    [Serializable]
    private class IntEntry
    {
        public string id;
        public int amount;
    }

    [Serializable]
    private class SaveData
    {
        public string version = SaveVersion;
        public string sceneName;
        public long savedUtcTicks;

        public Vector3 playerPosition;
        public float playerHealth;
        public float playerOxygen;
        public int playerAmmo;

        public List<IntEntry> inventoryEntries = new List<IntEntry>();
        public List<IntEntry> extractedEntries = new List<IntEntry>();
    }

    public struct SaveSlotMeta
    {
        public bool exists;
        public DateTime savedAtUtc;
        public string displayText;
    }

    public static void SetPendingLoadSlot(int slotIndex0Based)
    {
        pendingLoadSlotIndex = slotIndex0Based;
    }

    public static void ClearPendingLoadSlot()
    {
        pendingLoadSlotIndex = -1;
    }

    private static bool TryConsumePendingLoadSlot(out int slotIndex0Based)
    {
        if (pendingLoadSlotIndex >= 0)
        {
            slotIndex0Based = pendingLoadSlotIndex;
            pendingLoadSlotIndex = -1;
            return true;
        }

        slotIndex0Based = -1;
        return false;
    }

    public static string GetSavePath(int slotIndex0Based)
    {
        // slotIndex0Based: 0..2
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slotIndex0Based}.json");
    }

    public static bool HasSave(int slotIndex0Based)
    {
        string path = GetSavePath(slotIndex0Based);
        return File.Exists(path);
    }

    public static SaveSlotMeta GetSlotMeta(int slotIndex0Based)
    {
        SaveSlotMeta meta = new SaveSlotMeta
        {
            exists = false,
            savedAtUtc = default,
            displayText = "Empty"
        };

        string path = GetSavePath(slotIndex0Based);
        if (!File.Exists(path))
        {
            return meta;
        }

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return meta;
            }

            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return meta;
            }

            meta.exists = true;
            meta.savedAtUtc = new DateTime(data.savedUtcTicks, DateTimeKind.Utc);
            meta.displayText = $"Saved {meta.savedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}";
            return meta;
        }
        catch
        {
            return meta;
        }
    }

    public static void SaveToSlot(int slotIndex0Based)
    {
        int safeSlot = Mathf.Clamp(slotIndex0Based, 0, SlotCount - 1);

        PlayerStats ps = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
        InventorySystem inv = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
        ExtractedResourceBank bank = UnityEngine.Object.FindAnyObjectByType<ExtractedResourceBank>();
        PlayerAmmo ammo = UnityEngine.Object.FindAnyObjectByType<PlayerAmmo>();

        if (ps == null)
        {
            Debug.LogWarning("Save failed: missing PlayerStats in scene.");
            return;
        }

        SaveData data = new SaveData();
        data.sceneName = SceneManager.GetActiveScene().name;
        data.savedUtcTicks = DateTime.UtcNow.Ticks;
        data.playerPosition = ps.transform.position;
        data.playerHealth = ps.CurrentHealth;
        data.playerOxygen = ps.CurrentOxygen;

        if (ammo != null)
        {
            data.playerAmmo = ammo.CurrentAmmo;
        }

        if (inv != null)
        {
            Dictionary<string, int> snapshot = inv.GetSnapshot();
            if (snapshot != null)
            {
                foreach (var kvp in snapshot)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value <= 0) continue;
                    data.inventoryEntries.Add(new IntEntry { id = kvp.Key, amount = kvp.Value });
                }
            }
        }

        if (bank != null)
        {
            IReadOnlyDictionary<string, int> bankSnapshot = bank.GetAllBankedResources();
            if (bankSnapshot != null)
            {
                foreach (var kvp in bankSnapshot)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value <= 0) continue;
                    data.extractedEntries.Add(new IntEntry { id = kvp.Key, amount = kvp.Value });
                }
            }
        }

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        string path = GetSavePath(safeSlot);
        File.WriteAllText(path, json);
    }

    public static void TryLoadPendingSlotIntoWorld()
    {
        if (!TryConsumePendingLoadSlot(out int slotIndex0Based))
        {
            return;
        }

        if (!HasSave(slotIndex0Based))
        {
            return;
        }

        TryLoadSlotIntoWorld(slotIndex0Based);
    }

    public static bool TryLoadSlotIntoWorld(int slotIndex0Based)
    {
        int safeSlot = Mathf.Clamp(slotIndex0Based, 0, SlotCount - 1);
        string path = GetSavePath(safeSlot);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return false;
            }

            PlayerStats ps = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
            InventorySystem inv = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
            ExtractedResourceBank bank = UnityEngine.Object.FindAnyObjectByType<ExtractedResourceBank>();
            PlayerAmmo ammo = UnityEngine.Object.FindAnyObjectByType<PlayerAmmo>();

            if (ps != null)
            {
                // Override RespawnPlayer() and ResetForNewRun() effects.
                ps.transform.position = data.playerPosition;
                Rigidbody2D rb = ps.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                ps.SetCurrentHealthAndOxygen(data.playerHealth, data.playerOxygen);
            }

            if (ammo != null)
            {
                ammo.SetCurrentAmmoFromSave(data.playerAmmo);
            }

            if (inv != null)
            {
                inv.ClearRunResources();
                if (data.inventoryEntries != null)
                {
                    for (int i = 0; i < data.inventoryEntries.Count; i++)
                    {
                        IntEntry e = data.inventoryEntries[i];
                        if (e == null || string.IsNullOrWhiteSpace(e.id) || e.amount <= 0) continue;
                        inv.AddItem(e.id, e.amount, notify: true);
                    }
                }
            }

            if (bank != null)
            {
                Dictionary<string, int> extracted = new Dictionary<string, int>();
                if (data.extractedEntries != null)
                {
                    for (int i = 0; i < data.extractedEntries.Count; i++)
                    {
                        IntEntry e = data.extractedEntries[i];
                        if (e == null || string.IsNullOrWhiteSpace(e.id) || e.amount <= 0) continue;
                        extracted[e.id] = e.amount;
                    }
                }

                bank.ReplaceBankedResources(extracted);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}

