using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyShooterAI : MonoBehaviour
{
    [Header("Bullet Spawning")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Vector2 fireOffset = new Vector2(0.2f, 0f);

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
            PlayerStats stats = FindAnyObjectByType<PlayerStats>();
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
            PlayerStats stats = FindAnyObjectByType<PlayerStats>();
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

        Vector2 offset = fireOffset;
        // Make the offset point in the direction the enemy is firing.
        if (direction.x < 0f) offset.x = -Mathf.Abs(offset.x);
        else offset.x = Mathf.Abs(offset.x);

        Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Bullet b = bullet != null ? bullet.GetComponent<Bullet>() : null;
        if (b != null)
        {
            b.Initialize(direction, bulletSpeed, bulletDamage, gameObject);
        }
    }

    private static T FindAnyObjectByType<T>() where T : Component
    {
        return Object.FindAnyObjectByType<T>();
    }
}

