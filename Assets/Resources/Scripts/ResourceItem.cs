using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ResourceItem : MonoBehaviour
{
    [Header("Resource Payload")]
    [SerializeField] private string resourceId = "Iron";
    [SerializeField] private int amount = 1;
    [SerializeField] private bool requirePlayerTag = true;
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private float pickupSfxVolume = 0.75f;

    public void Initialize(string id, int count)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            resourceId = id;
        }

        amount = Mathf.Max(1, count);
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
