using System;
using UnityEngine;

public class PlayerAmmo : MonoBehaviour, IRunResettable
{
    private const int DEFAULT_MAX_AMMO = 80;
    private const int DEFAULT_START_AMMO = 80;

    [Header("Ammo Settings")]
    [SerializeField] private int maxAmmo = 150;
    [SerializeField] private int startAmmo = 80;

    private int baseMaxAmmo;
    private int baseStartAmmo;

    private int currentAmmo;

    public event Action<int, int> OnAmmoChanged;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;

    private void Awake()
    {
        // Enforce the current design defaults even if an older prefab
        // has serialized different values.
        maxAmmo = DEFAULT_MAX_AMMO;
        startAmmo = Mathf.Clamp(DEFAULT_START_AMMO, 0, maxAmmo);

        baseMaxAmmo = maxAmmo;
        baseStartAmmo = startAmmo;
        currentAmmo = startAmmo;
    }

    private void Start()
    {
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    // Returns false if not enough ammo to fire.
    public bool TrySpendAmmo(int amount)
    {
        if (amount <= 0) return true;
        if (currentAmmo < amount) return false;

        currentAmmo -= amount;
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        return true;
    }

    public void AddAmmo(int amount)
    {
        if (amount <= 0) return;

        int before = currentAmmo;
        currentAmmo = Mathf.Clamp(currentAmmo + amount, 0, maxAmmo);
        if (currentAmmo != before)
        {
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }
    }

    public void ResetForNewRun()
    {
        currentAmmo = startAmmo;
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    // Called by the upgrade system when permanent upgrades change ammo capacity.
    // When healToFull is true, we also refresh current ammo to the new starting ammo.
    public void ApplyStartingAmmoBonus(int bonusAmount, bool healToFull)
    {
        int b = Mathf.Max(0, bonusAmount);

        // “Starting ammo” upgrade: increases both max capacity and the run's starting ammo.
        maxAmmo = Mathf.Max(1, baseMaxAmmo + b);
        startAmmo = Mathf.Clamp(baseStartAmmo + b, 0, maxAmmo);

        if (healToFull)
        {
            currentAmmo = startAmmo;
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }
        else
        {
            // Keep current ammo but clamp to new max.
            currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }
    }

    // Used by save/load. If old saves don't include ammo, fallback to startAmmo.
    public void SetCurrentAmmoFromSave(int ammoFromSave)
    {
        if (ammoFromSave <= 0)
        {
            currentAmmo = startAmmo;
        }
        else
        {
            currentAmmo = Mathf.Clamp(ammoFromSave, 0, maxAmmo);
        }
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }
}

