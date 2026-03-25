using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HealthPickup : MonoBehaviour
{
    [Header("Pickup Behavior")]
    [SerializeField] private float healAmount = 30f;
    [SerializeField] private bool destroyOnPickup = true;

    private void Reset()
    {
        // Make it easy to configure in the editor.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null) return;

        stats.Heal(healAmount);

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }
}

