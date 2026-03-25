using UnityEngine;

public class LevelSetupSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject resourceNodePrefab;

    [Header("Resource Mapping (via SpawnPoint2D.spawnId)")]
    [SerializeField] private string defaultResourceId = "Iron";
    [SerializeField] private string crystalResourceId = "Crystal";

    [Header("Behavior")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool clearSpawnedOnRespawn = true;

    [Header("Difficulty Scaling (distance from PlayerSpawn)")]
    [SerializeField] private float minDifficultyDistance = 0f;
    [SerializeField] private float maxDifficultyDistance = 90f;

    [SerializeField] private float minEnemyHealthMultiplier = 1f;
    [SerializeField] private float maxEnemyHealthMultiplier = 2.6f;
    [SerializeField] private float minEnemySpeedMultiplier = 1f;
    [SerializeField] private float maxEnemySpeedMultiplier = 1.6f;
    [SerializeField] private float minEnemyDamageMultiplier = 1f;
    [SerializeField] private float maxEnemyDamageMultiplier = 2f;

    [SerializeField] private float minShooterChance = 0.05f;
    [SerializeField] private float maxShooterChance = 0.35f;

    [Header("Rare Material Scaling")]
    [SerializeField] private float minIronHealthMultiplier = 1f;
    [SerializeField] private float maxIronHealthMultiplier = 1.35f;
    [SerializeField] private float minIronDropMultiplier = 1f;
    [SerializeField] private float maxIronDropMultiplier = 1.8f;

    [SerializeField] private float minCrystalHealthMultiplier = 1.2f;
    [SerializeField] private float maxCrystalHealthMultiplier = 2.6f;
    [SerializeField] private float minCrystalDropMultiplier = 1.2f;
    [SerializeField] private float maxCrystalDropMultiplier = 2.8f;

    [Header("Enemy Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shooterRange = 10f;
    [SerializeField] private float shooterCooldown = 1.2f;
    [SerializeField] private float shooterBulletSpeed = 12f;
    [SerializeField] private int shooterBulletDamage = 10;

    [Header("Organization (optional)")]
    [SerializeField] private Transform spawnedEnemiesRoot;
    [SerializeField] private Transform spawnedResourcesRoot;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnAll();
        }
    }

    public void SpawnAll()
    {
        if (clearSpawnedOnRespawn)
        {
            ClearChildren(spawnedEnemiesRoot);
            ClearChildren(spawnedResourcesRoot);
        }

        SpawnByType(SpawnPoint2D.SpawnType.Enemy, enemyPrefab, spawnedEnemiesRoot);
        SpawnByType(SpawnPoint2D.SpawnType.Resource, resourceNodePrefab, spawnedResourcesRoot);
    }

    private void SpawnByType(SpawnPoint2D.SpawnType type, GameObject prefab, Transform parent)
    {
        if (prefab == null)
        {
            return;
        }

        // Include inactive spawn points; otherwise some tiers can end up empty.
        SpawnPoint2D[] points = FindObjectsByType<SpawnPoint2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Vector3 playerSpawnPos = GetPlayerSpawnPosition();

        for (int i = 0; i < points.Length; i++)
        {
            SpawnPoint2D point = points[i];
            if (point == null || point.Type != type)
            {
                continue;
            }
            
            GameObject spawned = Instantiate(prefab, point.transform.position, Quaternion.identity, parent);

            if (type == SpawnPoint2D.SpawnType.Resource)
            {
                ResourceNode node = spawned != null ? spawned.GetComponent<ResourceNode>() : null;
                if (node != null)
                {
                    string resourceId = DetermineResourceId(point);
                    node.SetResourceId(resourceId);

                    float difficultyT = GetDifficultyT(point.transform.position, playerSpawnPos);
                    if (string.Equals(resourceId, crystalResourceId))
                    {
                        float healthMult = Mathf.Lerp(minCrystalHealthMultiplier, maxCrystalHealthMultiplier, difficultyT);
                        float dropMult = Mathf.Lerp(minCrystalDropMultiplier, maxCrystalDropMultiplier, difficultyT);
                        node.ApplyDifficulty(healthMult, dropMult);
                    }
                    else
                    {
                        float healthMult = Mathf.Lerp(minIronHealthMultiplier, maxIronHealthMultiplier, difficultyT);
                        float dropMult = Mathf.Lerp(minIronDropMultiplier, maxIronDropMultiplier, difficultyT);
                        node.ApplyDifficulty(healthMult, dropMult);
                    }
                }
            }
            else if (type == SpawnPoint2D.SpawnType.Enemy)
            {
                float difficultyT = GetDifficultyT(point.transform.position, playerSpawnPos);

                // Strength increases with distance from the initial player spawn.
                float healthMult = Mathf.Lerp(minEnemyHealthMultiplier, maxEnemyHealthMultiplier, difficultyT);
                float speedMult = Mathf.Lerp(minEnemySpeedMultiplier, maxEnemySpeedMultiplier, difficultyT);
                float damageMult = Mathf.Lerp(minEnemyDamageMultiplier, maxEnemyDamageMultiplier, difficultyT);

                EnemyHealth enemyHealth = spawned != null ? spawned.GetComponent<EnemyHealth>() : null;
                if (enemyHealth != null)
                {
                    enemyHealth.ApplyDifficulty(healthMult);
                }

                EnemyAI2D enemyAI = spawned != null ? spawned.GetComponent<EnemyAI2D>() : null;
                if (enemyAI != null)
                {
                    enemyAI.ApplyDifficulty(speedMult, damageMult);
                }

                // Some enemies shoot back; chance scales with distance too.
                float shooterChance = Mathf.Lerp(minShooterChance, maxShooterChance, difficultyT);
                if (bulletPrefab != null && spawned != null && Random.value <= shooterChance)
                {
                    EnemyShooterAI shooter = spawned.GetComponent<EnemyShooterAI>();
                    if (shooter == null)
                    {
                        shooter = spawned.AddComponent<EnemyShooterAI>();
                    }

                    float shotSpeed = shooterBulletSpeed * Mathf.Lerp(0.9f, 1.3f, difficultyT);
                    // Make bullets feel impactful as difficulty increases.
                    // Extra multiplier so changes apply even if an older prefab/scene serialized value was used.
                    int shotDamage = Mathf.Max(1, Mathf.RoundToInt(shooterBulletDamage * Mathf.Lerp(1.0f, 2.6f, difficultyT) * 2.0f));
                    shooter.Configure(bulletPrefab, shooterRange, shooterCooldown, shotSpeed, shotDamage);
                }
            }
        }
    }

    private float GetDifficultyT(Vector3 spawnPointPos, Vector3 playerSpawnPos)
    {
        float distance = Vector3.Distance(spawnPointPos, playerSpawnPos);
        return Mathf.InverseLerp(minDifficultyDistance, maxDifficultyDistance, distance);
    }

    private Vector3 GetPlayerSpawnPosition()
    {
        // Generated level uses a GameObject named "PlayerSpawn".
        GameObject spawnGo = GameObject.Find("PlayerSpawn");
        if (spawnGo != null)
        {
            return spawnGo.transform.position;
        }

        // Fallback: use current player position.
        PlayerStats stats = FindAnyObjectByType<PlayerStats>();
        if (stats != null)
        {
            return stats.transform.position;
        }

        return Vector3.zero;
    }

    private string DetermineResourceId(SpawnPoint2D point)
    {
        if (point == null)
        {
            return defaultResourceId;
        }

        string idHint = point.SpawnId;
        if (!string.IsNullOrWhiteSpace(idHint))
        {
            string lower = idHint.ToLowerInvariant();
            if (lower.Contains("crystal"))
            {
                return crystalResourceId;
            }

            if (lower.Contains("iron"))
            {
                return defaultResourceId;
            }
        }

        return defaultResourceId;
    }

    private static void ClearChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }
}
