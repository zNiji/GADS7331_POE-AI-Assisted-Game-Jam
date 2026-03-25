using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class OxygenPickup : MonoBehaviour
{
    [Header("Pickup Behavior")]
    [SerializeField] private float oxygenAmount = 30f;
    [SerializeField] private bool destroyOnPickup = true;

    private void Reset()
    {
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

        stats.RestoreOxygen(oxygenAmount);

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }
}

