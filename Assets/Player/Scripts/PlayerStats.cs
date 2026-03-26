using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
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

    [Header("Out of Bounds (Fall Death)")]
    [SerializeField] private bool enableFallDeath = true;
    [SerializeField] private float fallKillY = -10f;

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

        // If the player falls below the level, treat it as a death.
        // This prevents infinite “out of the map” runs.
        if (enableFallDeath && !isDead && transform.position.y < fallKillY)
        {
            ForceDeath();
        }
    }

    private void ForceDeath()
    {
        isDead = true;
        currentHealth = 0f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Hurt/death audio is handled here so it always triggers.
        if (GameAudioManager.Instance == null)
        {
            GameAudioManager existing = FindAnyObjectByType<GameAudioManager>();
            if (existing == null)
            {
                GameObject go = new GameObject("GameAudioManager");
                go.AddComponent<GameAudioManager>();
            }
        }

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayPlayerDeath(transform.position);
        }

        OnDied?.Invoke();
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

        // Ensure gameplay audio exists when damage happens (scene load ordering can vary).
        if (GameAudioManager.Instance == null)
        {
            GameAudioManager existing = FindAnyObjectByType<GameAudioManager>();
            if (existing == null)
            {
                GameObject go = new GameObject("GameAudioManager");
                go.AddComponent<GameAudioManager>();
            }
        }

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayPlayerHurt(transform.position, 1f);
        }

        if (currentHealth <= 0f)
        {
            isDead = true;

            if (GameAudioManager.Instance != null)
            {
                GameAudioManager.Instance.PlayPlayerDeath(transform.position);
            }

            OnDied?.Invoke();
        }
    }

    // Allows bullets/enemies to damage the player via the shared IDamageable interface.
    void IDamageable.TakeDamage(int amount)
    {
        TakeDamage((float)amount);
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

    public void SetCurrentHealthAndOxygen(float health, float oxygen)
    {
        // Used by save/load to restore exact run state.
        isDead = false;
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        currentOxygen = Mathf.Clamp(oxygen, 0f, maxOxygen);

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
