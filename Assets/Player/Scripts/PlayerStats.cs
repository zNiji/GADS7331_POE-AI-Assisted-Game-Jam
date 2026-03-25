using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Suit Integrity")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Oxygen")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float oxygenDrainPerSecond = 1f;
    [SerializeField] private float damageShakeDuration = 0.12f;
    [SerializeField] private float damageShakeMagnitude = 0.12f;
    [Header("Spawn Safety")]
    [SerializeField] private float spawnDamageImmunitySeconds = 1f;

    private float currentHealth;
    private float currentOxygen;
    private bool isDead;
    private float baseMaxHealth;
    private float invulnerableUntilTime;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnOxygenChanged;
    public event Action OnDied;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float CurrentOxygen => currentOxygen;
    public float MaxOxygen => maxOxygen;

    private void Awake()
    {
        baseMaxHealth = maxHealth;
        currentHealth = maxHealth;
        currentOxygen = maxOxygen;
        isDead = false;
        invulnerableUntilTime = Time.time + spawnDamageImmunitySeconds;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
    }

    private void Update()
    {
        DrainOxygen(Time.deltaTime * oxygenDrainPerSecond);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || isDead)
        {
            return;
        }

        if (Time.time < invulnerableUntilTime)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (CameraShake2D.Instance != null)
        {
            CameraShake2D.Instance.Shake(damageShakeDuration, damageShakeMagnitude);
        }

        if (currentHealth <= 0f)
        {
            isDead = true;
            OnDied?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void RestoreOxygen(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentOxygen = Mathf.Min(maxOxygen, currentOxygen + amount);
        OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
    }

    private void DrainOxygen(float amount)
    {
        if (amount <= 0f || currentOxygen <= 0f || isDead)
        {
            return;
        }

        currentOxygen = Mathf.Max(0f, currentOxygen - amount);
        OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
    }

    public void ResetForNewRun()
    {
        isDead = false;
        currentHealth = maxHealth;
        currentOxygen = maxOxygen;
        invulnerableUntilTime = Time.time + spawnDamageImmunitySeconds;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
    }

    public void AddMaxHealth(float amount, bool healByAmount)
    {
        if (amount <= 0f)
        {
            return;
        }

        maxHealth += amount;
        currentHealth = healByAmount ? currentHealth + amount : currentHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxHealthFromBonus(float bonusAmount, bool healToFull)
    {
        maxHealth = Mathf.Max(1f, baseMaxHealth + Mathf.Max(0f, bonusAmount));

        if (healToFull)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
