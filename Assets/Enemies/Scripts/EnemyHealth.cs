using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable, IRunResettable
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private ParticleSystem deathParticles;

    [Header("Ammo Drops")]
    [SerializeField] private GameObject bulletAmmoPickupPrefab;
    [SerializeField] private float ammoDropChanceBase = 0.12f;
    [SerializeField] private float ammoDropChanceMaxMultiplier = 1.7f;
    [SerializeField] private int ammoDropMin = 3;
    [SerializeField] private int ammoDropMax = 8;
    [SerializeField] private float ammoAmountMaxBoost = 0.7f;

    private float ammoDropChance;
    private bool didDropAmmoThisDeath;

    private int currentHealth;
    private Collider2D cachedCollider;

    private void Awake()
    {
        currentHealth = maxHealth;
        cachedCollider = GetComponent<Collider2D>();
        ammoDropChance = Mathf.Clamp01(ammoDropChanceBase);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            if (!didDropAmmoThisDeath)
            {
                didDropAmmoThisDeath = true;
                TryDropAmmoPickup();
            }

            if (GameAudioManager.Instance != null)
            {
                GameAudioManager.Instance.PlayEnemyDeath(transform.position);
            }

            if (deathParticles != null)
            {
                ParticleSystem spawned = Instantiate(deathParticles, transform.position, Quaternion.identity);
                Destroy(spawned.gameObject, 2f);
            }

            gameObject.SetActive(false);
        }
    }

    private void TryDropAmmoPickup()
    {
        if (bulletAmmoPickupPrefab == null) return;

        if (Random.value > ammoDropChance) return;

        // Each ammo pickup grants a fixed amount (tuning via drop chance only).
        int finalAmount = 10;

        GameObject instance = Instantiate(bulletAmmoPickupPrefab, transform.position, Quaternion.identity);
        BulletAmmoPickup pickup = instance != null ? instance.GetComponent<BulletAmmoPickup>() : null;
        if (pickup != null)
        {
            pickup.SetAmmoAmount(finalAmount);
        }
    }

    public void ResetForNewRun()
    {
        currentHealth = maxHealth;
        gameObject.SetActive(true);
        didDropAmmoThisDeath = false;

        if (cachedCollider != null)
        {
            cachedCollider.enabled = true;
        }
    }

    // Called by the level spawner to scale enemy difficulty.
    public void ApplyDifficulty(float healthMultiplier)
    {
        if (healthMultiplier <= 0f)
        {
            return;
        }

        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * healthMultiplier));
        currentHealth = maxHealth;
        gameObject.SetActive(true);

        if (cachedCollider != null)
        {
            cachedCollider.enabled = true;
        }

        // Scale ammo drop feel with enemy difficulty.
        float t = Mathf.Clamp01((healthMultiplier - 1f) / 2.2f);
        ammoDropChance = Mathf.Clamp01(ammoDropChanceBase * Mathf.Lerp(1f, ammoDropChanceMaxMultiplier, t));
    }
}
