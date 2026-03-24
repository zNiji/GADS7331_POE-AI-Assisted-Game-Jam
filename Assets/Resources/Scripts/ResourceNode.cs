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

    private int currentHealth;
    private bool playerInRange;
    private Collider2D triggerCollider;

    public string ResourceId => resourceId;

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
            Mine(1);
        }
    }

    public void Mine(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        currentHealth -= damage;
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
        gameObject.SetActive(true);

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
    }

    private bool IsValidPlayerCollider(Collider2D other)
    {
        if (requirePlayerTag)
        {
            return other.CompareTag("Player");
        }

        return other.GetComponent<PlayerMovement2D>() != null || other.GetComponentInParent<PlayerMovement2D>() != null;
    }
}
