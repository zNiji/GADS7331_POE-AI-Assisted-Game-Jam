using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ResourceNode : MonoBehaviour, IRunResettable
{
    [Header("Resource Settings")]
    [SerializeField] private string resourceId = "Iron";
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int dropAmount = 1;
    [SerializeField] private ResourceItem resourceDropPrefab;

    [Header("Mining")]
    [SerializeField] private KeyCode mineKey = KeyCode.E;
    [SerializeField] private bool requirePlayerTag = true;
    [SerializeField] private string promptMessage = "Press E to mine";
    [SerializeField] private ParticleSystem mineHitParticles;
    [SerializeField] private AudioClip mineSfx;
    [SerializeField] private float mineSfxVolume = 0.7f;

    private int currentHealth;
    private bool playerInRange;
    private Collider2D triggerCollider;
    private PlayerUpgradeEffects activeMinerUpgrades;

    public string ResourceId => resourceId;

    // Allows the level spawner/editor tooling to configure which resource this node mines.
    public void SetResourceId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        resourceId = id;
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        triggerCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (!playerInRange)
        {
            return;
        }

        if (Input.GetKeyDown(mineKey))
        {
            int minePower = 1 + (activeMinerUpgrades != null ? activeMinerUpgrades.BonusMiningPower : 0);
            Mine(minePower);
        }
    }

    public void Mine(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        currentHealth -= damage;
        SpawnMineParticles();

        if (mineSfx != null)
        {
            AudioSource.PlayClipAtPoint(mineSfx, transform.position, mineSfxVolume);
        }

        if (currentHealth <= 0)
        {
            DropResource();
            gameObject.SetActive(false);
        }
    }

    private void DropResource()
    {
        if (resourceDropPrefab == null || dropAmount <= 0)
        {
            return;
        }

        ResourceItem dropped = Instantiate(resourceDropPrefab, transform.position, Quaternion.identity);
        dropped.Initialize(resourceId, dropAmount);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidPlayerCollider(other))
        {
            return;
        }

        playerInRange = true;
        activeMinerUpgrades = other.GetComponentInParent<PlayerUpgradeEffects>();
        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowPrompt(promptMessage);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValidPlayerCollider(other))
        {
            return;
        }

        playerInRange = false;
        activeMinerUpgrades = null;
        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowPrompt(string.Empty);
        }
    }

    private void OnDestroy()
    {
        if (playerInRange && HUDController.Instance != null)
        {
            HUDController.Instance.ShowPrompt(string.Empty);
        }
    }

    public void ResetForNewRun()
    {
        currentHealth = maxHealth;
        playerInRange = false;
        activeMinerUpgrades = null;
        gameObject.SetActive(true);

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
    }

    // Called by the level spawner to scale mining difficulty and loot for rarer materials.
    public void ApplyDifficulty(float maxHealthMultiplier, float dropMultiplier)
    {
        if (maxHealthMultiplier <= 0f || dropMultiplier <= 0f)
        {
            return;
        }

        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * maxHealthMultiplier));
        dropAmount = Mathf.Max(1, Mathf.RoundToInt(dropAmount * dropMultiplier));

        currentHealth = maxHealth;
        playerInRange = false;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }

        gameObject.SetActive(true);
    }

    private bool IsValidPlayerCollider(Collider2D other)
    {
        if (requirePlayerTag)
        {
            return other.CompareTag("Player");
        }

        return other.GetComponent<PlayerMovement2D>() != null || other.GetComponentInParent<PlayerMovement2D>() != null;
    }

    private void SpawnMineParticles()
    {
        if (mineHitParticles == null)
        {
            return;
        }

        ParticleSystem spawned = Instantiate(mineHitParticles, transform.position, Quaternion.identity);
        Destroy(spawned.gameObject, 2f);
    }
}
