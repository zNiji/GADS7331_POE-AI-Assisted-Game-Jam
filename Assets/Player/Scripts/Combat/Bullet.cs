using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;

    private Rigidbody2D rb;
    private int damage;
    private GameObject owner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction, float speed, int damageAmount, GameObject bulletOwner)
    {
        damage = damageAmount;
        owner = bulletOwner;

        Vector2 normalizedDirection = direction.normalized;
        rb.linearVelocity = normalizedDirection * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.transform.root.gameObject == owner)
        {
            return;
        }

        // Ore/resources should not stop bullets.
        // Resource nodes use trigger colliders and do not implement IDamageable.
        ResourceNode resourceNode = other.GetComponentInParent<ResourceNode>();
        if (resourceNode != null)
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            // Hit the environment (e.g. ground/platform). Destroy the bullet.
            Destroy(gameObject);
            return;
        }

        damageable.TakeDamage(damage);
        Destroy(gameObject);
    }
}
