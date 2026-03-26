using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyShooterAI : MonoBehaviour
{
    [Header("Bullet Spawning")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Vector2 fireOffset = new Vector2(0.2f, 0f);
    [SerializeField] private float spawnDistanceFromShooter = 0.6f;

    [Header("Shooting")]
    [SerializeField] private float shootRange = 10f;
    [SerializeField] private float shootCooldown = 1.2f;
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private int bulletDamage = 1;

    private Transform player;
    private float nextShotTime;

    private void Awake()
    {
        if (player == null)
        {
            PlayerStats stats = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
            if (stats != null)
            {
                player = stats.transform;
            }
        }
    }

    // Editor/runtime-safe: configure when added by the level spawner.
    public void Configure(GameObject prefab, float range, float cooldown, float speed, int damage)
    {
        bulletPrefab = prefab;
        shootRange = Mathf.Max(0.1f, range);
        shootCooldown = Mathf.Max(0.05f, cooldown);
        bulletSpeed = Mathf.Max(0.1f, speed);
        bulletDamage = Mathf.Max(1, damage);
        nextShotTime = 0f;
    }

    private void Update()
    {
        if (bulletPrefab == null)
        {
            return;
        }

        if (player == null)
        {
            PlayerStats stats = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
            if (stats != null) player = stats.transform;
        }

        if (player == null)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > shootRange)
        {
            return;
        }

        if (Time.time < nextShotTime)
        {
            return;
        }

        FireAtPlayer();
        nextShotTime = Time.time + shootCooldown;
    }

    private void FireAtPlayer()
    {
        Vector2 toPlayer = (player.position - transform.position);
        Vector2 direction = toPlayer.sqrMagnitude < 0.0001f ? Vector2.right : toPlayer.normalized;
        
        // Spawn slightly forward so the bullet doesn't overlap the shooter's own collider.
        // This prevents instant self-hits where the bullet gets destroyed on spawn.
        Vector3 spawnPos = transform.position
                           + (Vector3)(direction * spawnDistanceFromShooter)
                           + (Vector3)fireOffset;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Bullet b = bullet != null ? bullet.GetComponent<Bullet>() : null;
        if (b != null)
        {
            // Use root object for the "ignore owner" check in Bullet.
            b.Initialize(direction, bulletSpeed, bulletDamage, transform.root.gameObject);
        }

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayEnemyShoot(spawnPos, 1f);
        }

        // Extra safety: ignore collision between the bullet and this shooter's colliders.
        Collider2D shooterCol = GetComponentInChildren<Collider2D>();
        if (shooterCol != null && bullet != null)
        {
            Collider2D bulletCol = bullet.GetComponentInChildren<Collider2D>();
            if (bulletCol != null)
            {
                Physics2D.IgnoreCollision(bulletCol, shooterCol, true);
            }
        }
    }

    // Intentionally no custom FindAnyObjectByType helper:
    // avoids editor warnings about hiding inherited methods.
}

