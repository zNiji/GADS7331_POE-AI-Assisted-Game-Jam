using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ResourceNode : MonoBehaviour, IRunResettable
{
    [Header("Resource Settings")]
    [SerializeField] private string resourceId = "Iron";
    [Header("Alien Tile Sprites (Optional)")]
    [SerializeField] private Sprite ironNodeSprite;
    [SerializeField] private Sprite crystalNodeSprite;
    [SerializeField] private Sprite uraniumNodeSprite;
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
    private SpriteRenderer spriteRenderer;

    public string ResourceId => resourceId;

    // Allows the level spawner/editor tooling to configure which resource this node mines.
    public void SetResourceId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        resourceId = id;
        UpdateSpriteForResourceId();
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        triggerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSpriteForResourceId();
    }

    private void UpdateSpriteForResourceId()
    {
        if (spriteRenderer == null) return;
        if (string.IsNullOrWhiteSpace(resourceId)) return;

        string lower = resourceId.ToLowerInvariant();
        if (lower.Contains("uranium"))
        {
            if (uraniumNodeSprite != null) spriteRenderer.sprite = uraniumNodeSprite;
            return;
        }
        if (lower.Contains("crystal"))
        {
            if (crystalNodeSprite != null) spriteRenderer.sprite = crystalNodeSprite;
            return;
        }

        // Default: Iron
        if (ironNodeSprite != null) spriteRenderer.sprite = ironNodeSprite;
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
        else if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayMine(transform.position, 1f);
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
