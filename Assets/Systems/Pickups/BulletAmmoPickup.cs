using UnityEngine;

public class BulletAmmoPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private int ammoAmount = 10;
    [SerializeField] private bool destroyOnPickup = true;

    public void SetAmmoAmount(int amount)
    {
        ammoAmount = Mathf.Max(0, amount);
    }

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

        PlayerAmmo ammo = other.GetComponentInParent<PlayerAmmo>();
        if (ammo == null) return;

        ammo.AddAmmo(ammoAmount);

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayAmmoPickup(transform.position, 1f);
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }
}

