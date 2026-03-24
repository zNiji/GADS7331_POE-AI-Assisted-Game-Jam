using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private SpriteRenderer playerSprite;

    [Header("Shoot Settings")]
    [SerializeField] private float fireRate = 5f;
    [SerializeField] private float bulletSpeed = 14f;
    [SerializeField] private int bulletDamage = 1;

    private float nextShotTime;

    private void Awake()
    {
        if (playerSprite == null)
        {
            playerSprite = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (Input.GetButton("Fire1"))
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            return;
        }

        if (Time.time < nextShotTime)
        {
            return;
        }

        nextShotTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));

        float direction = GetFacingDirectionSign();
        Vector2 shootDirection = new Vector2(direction, 0f);

        Bullet spawnedBullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        spawnedBullet.Initialize(shootDirection, bulletSpeed, bulletDamage, gameObject);
    }

    private float GetFacingDirectionSign()
    {
        if (playerSprite != null)
        {
            return playerSprite.flipX ? -1f : 1f;
        }

        return transform.localScale.x < 0f ? -1f : 1f;
    }
}
