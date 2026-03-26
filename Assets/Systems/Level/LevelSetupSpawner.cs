using System.Collections.Generic;
using UnityEngine;

public class LevelSetupSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject resourceNodePrefab;

    [Header("Pickup Prefabs")]
    [SerializeField] private GameObject oxygenPickupPrefab;
    [SerializeField] private bool spawnOxygenPickupsIfNone = true;

    [Header("Resource Mapping (via SpawnPoint2D.spawnId)")]
    [SerializeField] private string defaultResourceId = "Iron";
    [SerializeField] private string crystalResourceId = "Crystal";
    [SerializeField] private string uraniumResourceId = "Uranium";

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

    [Header("Ultra Rare Material Scaling")]
    [SerializeField] private float minUraniumHealthMultiplier = 2.0f;
    [SerializeField] private float maxUraniumHealthMultiplier = 5.0f;
    [SerializeField] private float minUraniumDropMultiplier = 1.5f;
    [SerializeField] private float maxUraniumDropMultiplier = 4.0f;

    [Header("Enemy Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shooterRange = 10f;
    [SerializeField] private float shooterCooldown = 1.2f;
    [SerializeField] private float shooterBulletSpeed = 12f;
    [SerializeField] private int shooterBulletDamage = 10;

    [Header("Enemy Sprites")]
    [SerializeField] private Sprite floraEnemySprite;
    [SerializeField] private Sprite faunaEnemySprite;
    [SerializeField] private Sprite shooterEnemySprite;

    [Header("Oxygen Pickups (rare)")]
    [SerializeField] private int maxOxygenPickups = 2;
    [SerializeField] private float oxygenAvoidResourceRadius = 2.0f;
    [SerializeField] private float oxygenAvoidExistingPickupRadius = 0.6f;
    [SerializeField] private float oxygenAvoidHealthPickupRadius = 1.2f;
    [SerializeField] private float oxygenRaycastHeightAbove = 10f;
    [SerializeField] private float oxygenRaycastDistance = 25f;
    [SerializeField] private float oxygenSurfaceYOffset = 0.7f;
    [SerializeField] private int oxygenSpawnAttempts = 80;

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

        SpawnOxygenPickupsIfNeeded();
    }

    private void SpawnOxygenPickupsIfNeeded()
    {
        if (!spawnOxygenPickupsIfNone)
        {
            return;
        }

        // If the oxygen prefab wasn't wired in the existing prefab yet,
        // fall back to cloning an existing HealthPickup instance (if any),
        // then disable its health behaviour and enable OxygenPickup.
        GameObject prefabToUse = oxygenPickupPrefab;
        if (prefabToUse == null)
        {
            // Procedural fallback so we still spawn oxygen even if your prefabs weren't regenerated yet.
            prefabToUse = CreateRuntimeOxygenPickupPrefab();
        }

        if (prefabToUse == null)
        {
            return;
        }

        // If oxygen pickups already exist, don't duplicate them.
        Transform levelRoot = GameObject.Find("GeneratedLevel") != null ? GameObject.Find("GeneratedLevel").transform : null;
        Transform oxygenRoot = levelRoot != null ? levelRoot.Find("OxygenPickups") : null;
        if (oxygenRoot != null && oxygenRoot.childCount > 0)
        {
            return;
        }

        // Create an oxygen root if missing.
        if (oxygenRoot == null)
        {
            GameObject oxygenRootGO = new GameObject("OxygenPickups");
            oxygenRootGO.transform.SetParent(levelRoot != null ? levelRoot : transform, false);
            oxygenRoot = oxygenRootGO.transform;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) return;
        LayerMask mask = 1 << groundLayer;

        // Collect resource spawn positions so we can keep pickups from overlapping ore nodes.
        List<Vector3> resourceSpawnPositions = new List<Vector3>();
        SpawnPoint2D[] all = FindObjectsByType<SpawnPoint2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (all[i].Type == SpawnPoint2D.SpawnType.Resource)
            {
                resourceSpawnPositions.Add(all[i].transform.position);
            }
        }

        // Collect already-spawned oxygen pickups (if any).
        List<Vector3> existingPickupPositions = new List<Vector3>();
        for (int i = 0; i < oxygenRoot.childCount; i++)
        {
            Transform c = oxygenRoot.GetChild(i);
            if (c != null) existingPickupPositions.Add(c.position);
        }

        // Collect health pickup positions so oxygen doesn't spawn on top of them.
        List<Vector3> existingHealthPickupPositions = new List<Vector3>();
        Transform healthRoot = null;
        if (levelRoot != null)
        {
            healthRoot = levelRoot.Find("HealthPickups");
        }
        if (healthRoot == null && transform != null)
        {
            healthRoot = transform.Find("HealthPickups");
        }
        if (healthRoot != null)
        {
            for (int i = 0; i < healthRoot.childCount; i++)
            {
                Transform c = healthRoot.GetChild(i);
                if (c != null) existingHealthPickupPositions.Add(c.position);
            }
        }

        float tier1Y = 0f;
        float tier2Y = 3f;
        float tier3Y = 6f;
        float tier4Y = 9f;

        float[] tierSpawnYs = new float[]
        {
            tier1Y + 2.2f,
            tier2Y + 2.2f,
            tier3Y + 2.2f,
            tier4Y + 2.2f
        };

        int spawnedCount = 0;
        for (int attempt = 0; attempt < oxygenSpawnAttempts; attempt++)
        {
            if (spawnedCount >= maxOxygenPickups) break;

            float x = Random.Range(-110f, 110f);
            float y = tierSpawnYs[Random.Range(0, tierSpawnYs.Length)];
            Vector3 guess = new Vector3(x, y, 0f);

            // Snap onto ground using a raycast.
            Vector2 origin = new Vector2(guess.x, guess.y + oxygenRaycastHeightAbove);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, oxygenRaycastDistance, mask);
            if (!hit.collider) continue;

            Vector3 finalPos = new Vector3(guess.x, hit.point.y + oxygenSurfaceYOffset, guess.z);

            // Overlap checks must use the final snapped position (y changes after raycast).

            // Skip if too close to an ore spawn point.
            for (int j = 0; j < resourceSpawnPositions.Count; j++)
            {
                if (Vector3.Distance(finalPos, resourceSpawnPositions[j]) <= oxygenAvoidResourceRadius)
                {
                    goto NextOxygenAttempt;
                }
            }

            // Skip if too close to existing oxygen pickups.
            for (int j = 0; j < existingPickupPositions.Count; j++)
            {
                if (Vector3.Distance(finalPos, existingPickupPositions[j]) <= oxygenAvoidExistingPickupRadius)
                {
                    goto NextOxygenAttempt;
                }
            }

            // Skip if too close to health pickups.
            for (int j = 0; j < existingHealthPickupPositions.Count; j++)
            {
                if (Vector3.Distance(finalPos, existingHealthPickupPositions[j]) <= oxygenAvoidHealthPickupRadius)
                {
                    goto NextOxygenAttempt;
                }
            }

            GameObject instance = Instantiate(prefabToUse, finalPos, Quaternion.identity, oxygenRoot);
            existingPickupPositions.Add(finalPos);
            spawnedCount++;

            NextOxygenAttempt:
            continue;
        }

        // Safety net: if our strict overlap checks filtered everything out, try again with a looser radius.
        if (spawnedCount == 0)
        {
            float loosenedOreRadius = Mathf.Max(0.5f, oxygenAvoidResourceRadius * 0.5f);
            for (int attempt = 0; attempt < oxygenSpawnAttempts; attempt++)
            {
                if (spawnedCount >= maxOxygenPickups) break;

                float x = Random.Range(-110f, 110f);
                float y = tierSpawnYs[Random.Range(0, tierSpawnYs.Length)];
                Vector3 guess = new Vector3(x, y, 0f);

                Vector2 origin = new Vector2(guess.x, guess.y + oxygenRaycastHeightAbove);
                RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, oxygenRaycastDistance, mask);
                if (!hit.collider) continue;

                Vector3 finalPos = new Vector3(guess.x, hit.point.y + oxygenSurfaceYOffset, guess.z);

                // Use final snapped position for overlap checks.
                for (int j = 0; j < resourceSpawnPositions.Count; j++)
                {
                    if (Vector3.Distance(finalPos, resourceSpawnPositions[j]) <= loosenedOreRadius)
                    {
                        goto NextOxygenAttempt2;
                    }
                }
                for (int j = 0; j < existingHealthPickupPositions.Count; j++)
                {
                    if (Vector3.Distance(finalPos, existingHealthPickupPositions[j]) <= oxygenAvoidHealthPickupRadius)
                    {
                        goto NextOxygenAttempt2;
                    }
                }
                for (int j = 0; j < existingPickupPositions.Count; j++)
                {
                    if (Vector3.Distance(finalPos, existingPickupPositions[j]) <= oxygenAvoidExistingPickupRadius)
                    {
                        goto NextOxygenAttempt2;
                    }
                }

                Instantiate(prefabToUse, finalPos, Quaternion.identity, oxygenRoot);
                existingPickupPositions.Add(finalPos);
                spawnedCount++;

                NextOxygenAttempt2:
                continue;
            }
        }
    }

    private static Sprite runtimeOxygenSprite;
    private static GameObject runtimeOxygenPickupPrefab;

    private GameObject CreateRuntimeOxygenPickupPrefab()
    {
        if (runtimeOxygenPickupPrefab != null)
        {
            return runtimeOxygenPickupPrefab;
        }

        // Generate a distinct circular oxygen icon (transparent outside).
        Color core = new Color(0.55f, 0.95f, 1f, 1f);
        Color ring = new Color(0.12f, 0.35f, 0.55f, 1f);

        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(7.5f, 7.5f);
        float innerRadius = 5.0f;
        float outerRadius = 7.0f;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                Color c;
                if (dist > outerRadius)
                {
                    c = new Color(0f, 0f, 0f, 0f);
                }
                else if (dist > innerRadius)
                {
                    c = ring;
                }
                else
                {
                    // Add a small highlight cross.
                    bool highlight = Mathf.Abs(dx) <= 1.0f || Mathf.Abs(dy) <= 1.0f;
                    c = highlight ? Color.Lerp(core, Color.white, 0.35f) : core;
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        runtimeOxygenSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);

        GameObject go = new GameObject("RuntimeOxygenPickupPrefab");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = runtimeOxygenSprite;
        sr.sortingOrder = 1;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        go.AddComponent<OxygenPickup>();

        runtimeOxygenPickupPrefab = go;
        return runtimeOxygenPickupPrefab;
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
                    if (string.Equals(resourceId, uraniumResourceId))
                    {
                        float healthMult = Mathf.Lerp(minUraniumHealthMultiplier, maxUraniumHealthMultiplier, difficultyT);
                        float dropMult = Mathf.Lerp(minUraniumDropMultiplier, maxUraniumDropMultiplier, difficultyT);
                        node.ApplyDifficulty(healthMult, dropMult);
                    }
                    else if (string.Equals(resourceId, crystalResourceId))
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
                // Use a non-linear curve + high-tier boost so far-away enemies feel less squishy.
                // Stronger high-tier enemies: push the curve harder towards far-away spawns.
                float healthCurveT = Mathf.Pow(difficultyT, 1.85f);
                float healthMult = Mathf.Lerp(minEnemyHealthMultiplier, maxEnemyHealthMultiplier, healthCurveT);
                // Extra boost ramps harder near max difficulty.
                healthMult *= Mathf.Lerp(1f, 3.2f, difficultyT);
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
                    // Enemies further from the initial player spawn should aggro earlier/more reliably.
                    float detectionMult = Mathf.Lerp(1f, 2.2f, Mathf.Pow(difficultyT, 0.6f));
                    enemyAI.ApplyDifficulty(speedMult, damageMult, detectionMult);
                }

                // Some enemies shoot back; chance scales with distance too.
                // More frequent shooting enemies, especially at higher tiers.
                float shooterChance = Mathf.Lerp(minShooterChance, maxShooterChance, difficultyT);
                bool willBeShooter = bulletPrefab != null && spawned != null && Random.value <= shooterChance;

                if (willBeShooter)
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

                // Visuals: normal enemies are dangerous flora/fauna, shooters are alien race.
                SpriteRenderer enemyRenderer = spawned != null ? spawned.GetComponent<SpriteRenderer>() : null;
                if (enemyRenderer != null)
                {
                    if (willBeShooter && shooterEnemySprite != null)
                    {
                        enemyRenderer.sprite = shooterEnemySprite;
                    }
                    else
                    {
                        // Slightly bias towards fauna with distance so far-away spawns feel more varied.
                        bool isFauna = Random.value < Mathf.Lerp(0.45f, 0.65f, difficultyT);
                        Sprite chosen = isFauna ? faunaEnemySprite : floraEnemySprite;
                        if (chosen != null) enemyRenderer.sprite = chosen;
                    }
                }
            }
        }
    }

    private float GetDifficultyT(Vector3 spawnPointPos, Vector3 playerSpawnPos)
    {
        float distance = Vector3.Distance(spawnPointPos, playerSpawnPos);
        return Mathf.Clamp01(Mathf.InverseLerp(minDifficultyDistance, maxDifficultyDistance, distance));
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
            if (lower.Contains("uranium"))
            {
                return uraniumResourceId;
            }
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
