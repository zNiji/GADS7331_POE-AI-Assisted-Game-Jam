using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Suit Integrity")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Oxygen")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float oxygenDrainPerSecond = 1f;

    private float currentHealth;
    private float currentOxygen;
    private bool isDead;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnOxygenChanged;
    public event Action OnDied;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float CurrentOxygen => currentOxygen;
    public float MaxOxygen => maxOxygen;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentOxygen = maxOxygen;
        isDead = false;
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

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

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
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
    }
}
