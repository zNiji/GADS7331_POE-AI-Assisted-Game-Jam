using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable, IRunResettable
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private ParticleSystem deathParticles;

    private int currentHealth;
    private Collider2D cachedCollider;

    private void Awake()
    {
        currentHealth = maxHealth;
        cachedCollider = GetComponent<Collider2D>();
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
            if (deathParticles != null)
            {
                ParticleSystem spawned = Instantiate(deathParticles, transform.position, Quaternion.identity);
                Destroy(spawned.gameObject, 2f);
            }

            gameObject.SetActive(false);
        }
    }

    public void ResetForNewRun()
    {
        currentHealth = maxHealth;
        gameObject.SetActive(true);

        if (cachedCollider != null)
        {
            cachedCollider.enabled = true;
        }
    }
}
