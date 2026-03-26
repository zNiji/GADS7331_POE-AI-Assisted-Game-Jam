using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ResourceItem : MonoBehaviour
{
    [Header("Resource Payload")]
    [SerializeField] private string resourceId = "Iron";
    [Header("Alien Tile Sprites (Optional)")]
    [SerializeField] private Sprite ironItemSprite;
    [SerializeField] private Sprite crystalItemSprite;
    [SerializeField] private Sprite uraniumItemSprite;
    [SerializeField] private int amount = 1;
    [SerializeField] private bool requirePlayerTag = true;
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private float pickupSfxVolume = 0.75f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSpriteForResourceId();
    }

    public void Initialize(string id, int count)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            resourceId = id;
        }

        amount = Mathf.Max(1, count);
        UpdateSpriteForResourceId();
    }

    private void UpdateSpriteForResourceId()
    {
        if (spriteRenderer == null) return;
        if (string.IsNullOrWhiteSpace(resourceId)) return;

        string lower = resourceId.ToLowerInvariant();
        if (lower.Contains("uranium"))
        {
            if (uraniumItemSprite != null) spriteRenderer.sprite = uraniumItemSprite;
            return;
        }
        if (lower.Contains("crystal"))
        {
            if (crystalItemSprite != null) spriteRenderer.sprite = crystalItemSprite;
            return;
        }

        if (ironItemSprite != null) spriteRenderer.sprite = ironItemSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidPlayerCollider(other))
        {
            return;
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.AddItem(resourceId, amount);
        }

        if (pickupSfx != null)
        {
            AudioSource.PlayClipAtPoint(pickupSfx, transform.position, pickupSfxVolume);
        }
        else if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayPickup(transform.position, 1f);
        }

        Destroy(gameObject);
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
