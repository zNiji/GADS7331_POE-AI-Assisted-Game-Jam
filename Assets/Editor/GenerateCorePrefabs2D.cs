using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public static class GenerateCorePrefabs2D
{
    private const string ArtDir = "Assets/Art/Generated";
    private const string PrefabDir = "Assets/Prefabs";
    private const string MaterialDir = "Assets/Art/Generated/Materials";
    private const string SpriteMaterialPath = "Assets/Art/Generated/Materials/generated_sprite_material.mat";

    [MenuItem("Tools/Frontier Extraction/Generate Core 2D Prefabs")]
    public static void Generate()
    {
        EnsureTag("Player");
        EnsureDir("Assets/Art");
        EnsureDir(ArtDir);
        EnsureDir(PrefabDir);
        EnsureDir(PrefabDir + "/UI");

        // Cylindrical/pill bullet to better read in motion.
        Sprite bulletSprite = CreateCylindricalBulletSprite("spr_bullet", seed: 55);

        // Player + enemies (pixel-art, procedural).
        Sprite astronautSprite = CreateAstronautSprite("spr_astronaut_player", seed: 11);
        Sprite floraEnemySprite = CreateFloraEnemySprite("spr_alien_flora_enemy", seed: 21);
        Sprite faunaEnemySprite = CreateFaunaEnemySprite("spr_alien_fauna_enemy", seed: 31);
        Sprite shooterAlienSprite = CreateAlienShooterEnemySprite("spr_alien_shooter_enemy", seed: 41);
        // Alien tile sprites (pixel-art, procedural, deterministic).
        // Different ore types so Iron/Crystal/Uranium look distinct.
        Sprite ironNodeSprite = CreateAlienTileSprite(
            "spr_alien_ore_iron_node",
            baseFill: new Color(0.18f, 0.75f, 0.95f),
            borderColor: new Color(0.08f, 0.28f, 0.45f),
            accentColor: new Color(0.75f, 1f, 0.45f),
            accentChance01: 0.12f,
            seed: 101
        );
        Sprite crystalNodeSprite = CreateAlienTileSprite(
            "spr_alien_ore_crystal_node",
            baseFill: new Color(0.35f, 0.85f, 1f),
            borderColor: new Color(0.10f, 0.35f, 0.55f),
            accentColor: new Color(1f, 0.55f, 0.9f),
            accentChance01: 0.10f,
            seed: 202
        );
        Sprite uraniumNodeSprite = CreateAlienTileSprite(
            "spr_alien_ore_uranium_node",
            baseFill: new Color(0.75f, 0.35f, 1f),
            borderColor: new Color(0.35f, 0.08f, 0.55f),
            accentColor: new Color(0.55f, 1f, 0.7f),
            accentChance01: 0.11f,
            seed: 303
        );

        Sprite ironItemSprite = CreateAlienTileSprite(
            "spr_alien_ore_iron_item",
            baseFill: new Color(0.45f, 1f, 0.75f),
            borderColor: new Color(0.18f, 0.35f, 0.18f),
            accentColor: new Color(0.95f, 0.55f, 0.25f),
            accentChance01: 0.08f,
            seed: 404
        );
        Sprite crystalItemSprite = CreateAlienTileSprite(
            "spr_alien_ore_crystal_item",
            baseFill: new Color(0.65f, 0.95f, 1f),
            borderColor: new Color(0.15f, 0.35f, 0.55f),
            accentColor: new Color(1f, 0.7f, 0.25f),
            accentChance01: 0.08f,
            seed: 505
        );
        Sprite uraniumItemSprite = CreateAlienTileSprite(
            "spr_alien_ore_uranium_item",
            baseFill: new Color(0.95f, 0.6f, 1f),
            borderColor: new Color(0.35f, 0.08f, 0.55f),
            accentColor: new Color(0.25f, 1f, 0.6f),
            accentChance01: 0.08f,
            seed: 606
        );
        Sprite healthPickupSprite = CreateAlienHealthPickupSprite(
            "spr_alien_health_pickup",
            // Match flora enemy body color (neon green).
            fill: new Color(0.18f, 1f, 0.55f),
            border: new Color(0.05f, 0.35f, 0.2f),
            cross: new Color(0.22f, 1f, 0.6f),
            seed: 303
        );
        // Oxygen icon is intentionally different (transparent outside, circular) so it doesn't look like resource nodes.
        Sprite oxygenPickupSprite = CreateAlienOxygenPickupSprite(
            "spr_alien_oxygen_pickup",
            fill: new Color(0.55f, 0.95f, 1f),
            border: new Color(0.12f, 0.35f, 0.55f),
            seed: 404
        );

        GameObject resourceItemPrefab = CreateResourceItemPrefab(ironItemSprite, crystalItemSprite, uraniumItemSprite);
        GameObject bulletPrefab = CreateBulletPrefab(bulletSprite);
        // Base enemy prefab: the runtime spawner will swap sprite to flora/fauna/shooter variants.
        GameObject enemyPrefab = CreateEnemyPrefab(faunaEnemySprite);
        GameObject resourceNodePrefab = CreateResourceNodePrefab(ironNodeSprite, resourceItemPrefab, crystalNodeSprite, uraniumNodeSprite);
        GameObject healthPickupPrefab = CreateHealthPickupPrefab(healthPickupSprite);
        GameObject oxygenPickupPrefab = CreateOxygenPickupPrefab(oxygenPickupSprite);
        GameObject rowPrefab = CreateUpgradeRowPrefab();
        GameObject playerPrefab = CreatePlayerPrefab(astronautSprite, bulletPrefab);
        GameObject hudPrefab = CreateHudCanvasPrefab(rowPrefab);
        GameObject gameSystemsPrefab = CreateGameSystemsPrefab(
            enemyPrefab,
            resourceNodePrefab,
            bulletPrefab,
            oxygenPickupPrefab,
            floraEnemySprite,
            faunaEnemySprite,
            shooterAlienSprite
        );

        Selection.activeObject = gameSystemsPrefab != null ? gameSystemsPrefab : rowPrefab;
        Debug.Log("Core prefabs generated: Bullet, Enemy, ResourceNode, ResourceItem, HealthPickup, OxygenPickup, UpgradeOptionRowUI, Player, HUDCanvas, GameSystems.");
    }

    private static GameObject CreateHealthPickupPrefab(Sprite healthSprite)
    {
        GameObject root = new GameObject("HealthPickup");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = healthSprite;
        renderer.sortingOrder = 1;

        CircleCollider2D col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        // Attach behavior
        root.AddComponent<HealthPickup>();

        string path = PrefabDir + "/HealthPickup.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateOxygenPickupPrefab(Sprite oxygenSprite)
    {
        GameObject root = new GameObject("OxygenPickup");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = oxygenSprite;
        renderer.sortingOrder = 1;

        CircleCollider2D col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        root.AddComponent<OxygenPickup>();

        string path = PrefabDir + "/OxygenPickup.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    [MenuItem("Tools/Frontier Extraction/Bootstrap Open Scene From Generated Prefabs")]
    public static void BootstrapOpenScene()
    {
        Generate();
        CleanupBootstrapDuplicates();

        if (!SceneManager.GetActiveScene().isLoaded)
        {
            Debug.LogError("No active scene loaded.");
            return;
        }

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Player.prefab");
        GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/UI/HUDCanvas.prefab");
        GameObject systemsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/GameSystems.prefab");

        GameObject player = FindInScene("Player");
        if (player == null && playerPrefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
        }

        GameObject systems = FindInScene("GameSystems");
        if (systems == null && systemsPrefab != null)
        {
            systems = (GameObject)PrefabUtility.InstantiatePrefab(systemsPrefab);
            systems.name = "GameSystems";
            Undo.RegisterCreatedObjectUndo(systems, "Create GameSystems");
        }

        GameObject hud = FindInScene("HUDCanvas");
        bool needsFreshHud = hud == null || hud.GetComponent<DeathUpgradeMenuUI>() == null;
        if (needsFreshHud && hudPrefab != null)
        {
            if (hud != null)
            {
                Undo.DestroyObjectImmediate(hud);
            }

            hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
            hud.name = "HUDCanvas";
            Undo.RegisterCreatedObjectUndo(hud, "Create HUDCanvas");
        }

        EnsureEventSystem();

        Camera mainCamera = EnsurePlayerCamera();

        if (mainCamera.GetComponent<CameraShake2D>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraShake2D>();
        }

        if (mainCamera.GetComponent<CameraFollow2D>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraFollow2D>();
        }

        ConfigureCameraFor2D(mainCamera);

        if (systems != null && player != null)
        {
            var gm = systems.GetComponent<GameManager>();
            var extraction = systems.GetComponent<ExtractionSystem>();
            var baseUpgrades = systems.GetComponent<BaseUpgradeSystem>();
            var playerStats = player.GetComponent<PlayerStats>();
            var playerUpgrades = player.GetComponent<PlayerUpgradeEffects>();
            var deathMenu = hud != null ? hud.GetComponent<DeathUpgradeMenuUI>() : null;

            if (gm != null)
            {
                Transform spawn = FindInScene("PlayerSpawn") != null
                    ? FindInScene("PlayerSpawn").transform
                    : player.transform.Find("PlayerSpawn");
                if (spawn == null)
                {
                    GameObject spawnGo = new GameObject("PlayerSpawn");
                    spawn = spawnGo.transform;
                    spawn.position = player.transform.position;
                    spawn.SetParent(systems != null ? systems.transform : null);
                    Undo.RegisterCreatedObjectUndo(spawnGo, "Create PlayerSpawn");
                }

                SerializedObject gmSO = new SerializedObject(gm);
                gmSO.FindProperty("playerStats").objectReferenceValue = playerStats;
                gmSO.FindProperty("playerTransform").objectReferenceValue = player.transform;
                gmSO.FindProperty("playerSpawnPoint").objectReferenceValue = spawn;
                gmSO.FindProperty("deathUpgradeMenu").objectReferenceValue = deathMenu;
                gmSO.ApplyModifiedPropertiesWithoutUndo();
            }

            if (extraction != null)
            {
                SerializedObject exSO = new SerializedObject(extraction);
                exSO.FindProperty("playerStats").objectReferenceValue = playerStats;
                exSO.ApplyModifiedPropertiesWithoutUndo();
            }

            if (baseUpgrades != null)
            {
                SerializedObject upSO = new SerializedObject(baseUpgrades);
                upSO.FindProperty("playerStats").objectReferenceValue = playerStats;
                upSO.FindProperty("playerUpgradeEffects").objectReferenceValue = playerUpgrades;
                upSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        if (player != null && mainCamera != null)
        {
            CameraFollow2D follow = mainCamera.GetComponent<CameraFollow2D>();
            if (follow != null)
            {
                SerializedObject followSO = new SerializedObject(follow);
                followSO.FindProperty("target").objectReferenceValue = player.transform;
                followSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        if (hud != null && player != null)
        {
            var hudController = hud.GetComponent<HUDController>();
            if (hudController != null)
            {
                SerializedObject hudSO = new SerializedObject(hudController);
                hudSO.FindProperty("playerStats").objectReferenceValue = player.GetComponent<PlayerStats>();
                hudSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeObject = systems != null ? systems : player;
        Debug.Log("Open scene bootstrapped: Player, HUDCanvas, GameSystems, CameraShake, and key references wired.");
    }

    [MenuItem("Tools/Frontier Extraction/Create Playable Test Level")]
    public static void CreatePlayableTestLevel()
    {
        BootstrapOpenScene();
        CreateDefaultUpgradeDefinitions();

        EnsureLayer("Ground");

        GameObject levelRoot = FindInScene("GeneratedLevel");
        if (levelRoot == null)
        {
            levelRoot = new GameObject("GeneratedLevel");
            Undo.RegisterCreatedObjectUndo(levelRoot, "Create GeneratedLevel Root");
        }

        Transform geometryRoot = EnsureChild(levelRoot.transform, "Geometry");
        Transform spawnPointsRoot = EnsureChild(levelRoot.transform, "SpawnPoints");
        // Always rebuild so old spawn points/resources/enemies don't persist.
        ClearChildren(geometryRoot);
        ClearChildren(spawnPointsRoot);

        Sprite groundSprite = CreateAlienSquareTileSprite(
            "spr_alien_ground_block",
            baseFill: new Color(0.10f, 0.12f, 0.18f),
            borderColor: new Color(0.03f, 0.05f, 0.08f),
            accentColor: new Color(0.25f, 1f, 0.65f),
            accentChance01: 0.10f,
            seed: 505
        );
        BuildGroundIfMissing(geometryRoot, groundSprite);
        BuildPlatformsIfMissing(geometryRoot, groundSprite);

        // World/scene background behind the generated level (alien planet).
        Transform existingBg = levelRoot.transform.Find("LevelBackground");
        if (existingBg != null)
        {
            Object.DestroyImmediate(existingBg.gameObject);
        }

        Sprite bgSprite = CreateAlienBackgroundSprite("spr_alien_background", seed: 707);
        GameObject bgGO = new GameObject("LevelBackground", typeof(SpriteRenderer));
        bgGO.transform.SetParent(levelRoot.transform, false);
        SpriteRenderer bgSR = bgGO.GetComponent<SpriteRenderer>();
        bgSR.sprite = bgSprite;
        bgSR.sortingOrder = -50; // keep definitely behind ground blocks
        bgSR.color = Color.white;
        // Put it in the same Z-plane as gameplay so the camera definitely draws it.
        bgGO.transform.position = new Vector3(0f, 0f, 0f);
        // Scale to cover the typical visible area + wider generated map.
        // spr size is ~4x4 units (64px / 16ppu), so this gives a big starfield.
        // Make it tall enough for the expanded generated level height.
        bgGO.transform.localScale = new Vector3(75f, 40f, 1f);
        string bgName = bgSprite != null ? bgSprite.name : "null";
        Debug.Log($"[CreatePlayableTestLevel] Background sprite: {bgName}, sortingOrder: {bgSR.sortingOrder}");

        BuildSpawnPointsIfMissing(spawnPointsRoot);
        SnapSpawnPointsToGround(spawnPointsRoot);

        // Helpful debug so you can verify the generator actually rebuilt platforms/spawnpoints.
        int platformSprites = geometryRoot.GetComponentsInChildren<SpriteRenderer>(true).Length;
        int spawnPointCount = spawnPointsRoot.GetComponentsInChildren<SpawnPoint2D>(true).Length;
        Debug.Log($"[CreatePlayableTestLevel] Platform sprites: {platformSprites}, SpawnPoints: {spawnPointCount}");
        SpawnHealthPickups(levelRoot.transform, spawnPointsRoot);
        SpawnOxygenPickups(levelRoot.transform, spawnPointsRoot);
        PositionPlayerAtSpawn();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeObject = levelRoot;
        Debug.Log("Playable test level created: ground/platforms, spawn points, and camera follow ready.");
    }

    private static void SnapSpawnPointsToGround(Transform spawnPointsRoot)
    {
        if (spawnPointsRoot == null)
        {
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            return;
        }

        LayerMask mask = 1 << groundLayer;

        SpawnPoint2D[] points = spawnPointsRoot.GetComponentsInChildren<SpawnPoint2D>(true);
        for (int i = 0; i < points.Length; i++)
        {
            SpawnPoint2D sp = points[i];
            if (sp == null)
            {
                continue;
            }

            Vector3 pos = sp.transform.position;
            // Important: only raycast from just above the intended spawn height.
            // Otherwise we can "snap" everything to the top-most platform collider in the same X column.
            Vector2 origin = new Vector2(pos.x, pos.y + 0.2f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 20f, mask);
            if (!hit.collider)
            {
                continue;
            }

            float surfaceY = hit.point.y;
            float yOffset = sp.Type == SpawnPoint2D.SpawnType.Resource ? 0.5f : 0.6f;

            sp.transform.position = new Vector3(pos.x, surfaceY + yOffset, pos.z);
        }
    }

    private static void SpawnHealthPickups(Transform levelRootTransform, Transform spawnPointsRoot)
    {
        if (levelRootTransform == null) return;

        GameObject healthPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/HealthPickup.prefab");
        if (healthPickupPrefab == null) return;

        Transform healthRoot = EnsureChild(levelRootTransform, "HealthPickups");
        ClearChildren(healthRoot);

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) return;
        LayerMask mask = 1 << groundLayer;

        // Avoid placing pickups on top of ore/resource spawn locations.
        float avoidResourceRadius = 2.5f;
        Vector3[] resourceSpawnPositions = new Vector3[0];
        if (spawnPointsRoot != null)
        {
            SpawnPoint2D[] all = spawnPointsRoot.GetComponentsInChildren<SpawnPoint2D>(true);
            System.Collections.Generic.List<Vector3> list = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].Type == SpawnPoint2D.SpawnType.Resource)
                {
                    list.Add(all[i].transform.position);
                }
            }
            resourceSpawnPositions = list.ToArray();
        }

        // Match the tier spacing used by BuildPlatformsIfMissing().
        float tier1Y = 0f;
        float tier2Y = 3f;
        float tier3Y = 6f;
        float tier4Y = 9f;
        float tier5Y = 12f;
        float tier6Y = 15f;

        // Less common: aim for only a couple of placements, but keep extra candidates
        // in case one happens to overlap an ore.
        int maxPickups = 2;
        int spawnedCount = 0;
        // Oxygen should not use the same candidate X positions as health.
        // Using a candidate list + random pick gives a more varied layout.
        List<Vector3> pickupCandidates = new List<Vector3>
        {
            new Vector3(-60f, tier1Y + 2.2f, 0f),
            new Vector3(-70f, tier2Y + 2.2f, 0f),
            new Vector3(-25f, tier2Y + 2.2f, 0f),
            new Vector3(0f,   tier3Y + 2.2f, 0f),
            new Vector3(15f,  tier3Y + 2.2f, 0f),
            new Vector3(45f,  tier4Y + 2.2f, 0f),
            new Vector3(60f,  tier4Y + 2.2f, 0f),
            new Vector3(-80f, tier5Y + 2.2f, 0f),
            new Vector3(-10f, tier5Y + 2.2f, 0f),
            new Vector3(25f,  tier6Y + 2.2f, 0f),
            new Vector3(70f,  tier6Y + 2.2f, 0f),
        };

        while (pickupCandidates.Count > 0 && spawnedCount < maxPickups)
        {
            int idx = UnityEngine.Random.Range(0, pickupCandidates.Count);
            Vector3 guess = pickupCandidates[idx];
            pickupCandidates.RemoveAt(idx);

            // Skip if too close to an ore spawn point.
            bool overlapsOre = false;
            for (int j = 0; j < resourceSpawnPositions.Length; j++)
            {
                if (Vector3.Distance(new Vector3(guess.x, guess.y, guess.z), resourceSpawnPositions[j]) <= avoidResourceRadius)
                {
                    overlapsOre = true;
                    break;
                }
            }
            if (overlapsOre)
            {
                continue;
            }

            Vector2 origin = new Vector2(guess.x, guess.y + 10f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 25f, mask);
            if (!hit.collider) continue;

            float surfaceY = hit.point.y;
            float yOffset = 0.7f; // slightly above the platform surface
            Vector3 finalPos = new Vector3(guess.x, surfaceY + yOffset, guess.z);

            GameObject instance = PrefabUtility.InstantiatePrefab(healthPickupPrefab) as GameObject;
            if (instance == null) continue;

            instance.transform.SetPositionAndRotation(finalPos, Quaternion.identity);
            instance.transform.SetParent(healthRoot, true);
            Undo.RegisterCreatedObjectUndo(instance, "Spawn HealthPickup");
            spawnedCount++;
        }
    }

    private static void SpawnOxygenPickups(Transform levelRootTransform, Transform spawnPointsRoot)
    {
        if (levelRootTransform == null) return;

        GameObject oxygenPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/OxygenPickup.prefab");
        if (oxygenPickupPrefab == null) return;

        Transform oxygenRoot = EnsureChild(levelRootTransform, "OxygenPickups");
        ClearChildren(oxygenRoot);

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) return;
        LayerMask mask = 1 << groundLayer;

        // Avoid placing pickups on top of ore/resource spawn locations.
        float avoidResourceRadius = 2.5f;
        Vector3[] resourceSpawnPositions = new Vector3[0];
        if (spawnPointsRoot != null)
        {
            SpawnPoint2D[] all = spawnPointsRoot.GetComponentsInChildren<SpawnPoint2D>(true);
            System.Collections.Generic.List<Vector3> list = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].Type == SpawnPoint2D.SpawnType.Resource)
                {
                    list.Add(all[i].transform.position);
                }
            }
            resourceSpawnPositions = list.ToArray();
        }

        // Also avoid overlapping with already placed health pickups.
        Transform healthRoot = levelRootTransform.Find("HealthPickups");
        Vector3[] placedPickupPositions = new Vector3[0];
        if (healthRoot != null)
        {
            List<Vector3> list = new List<Vector3>();
            for (int i = 0; i < healthRoot.childCount; i++)
            {
                Transform c = healthRoot.GetChild(i);
                if (c != null) list.Add(c.position);
            }
            placedPickupPositions = list.ToArray();
        }

        // Match the tier spacing used by BuildPlatformsIfMissing().
        float tier1Y = 0f;
        float tier2Y = 3f;
        float tier3Y = 6f;
        float tier4Y = 9f;
        float tier5Y = 12f;
        float tier6Y = 15f;

        // As rare as health pickups.
        int maxPickups = 2;
        int spawnedCount = 0;

        Vector3[] pickupSpawnPositions =
        {
            new Vector3(-45f, tier1Y + 2.2f, 0f),
            new Vector3(-40f, tier2Y + 2.2f, 0f),
            new Vector3(20f, tier3Y + 2.2f, 0f),
            new Vector3(30f, tier4Y + 2.2f, 0f),
            new Vector3(-60f, tier5Y + 2.2f, 0f),
            new Vector3(10f, tier5Y + 2.2f, 0f),
            new Vector3(40f, tier6Y + 2.2f, 0f),
            new Vector3(-10f, tier6Y + 2.2f, 0f),
        };

        for (int i = 0; i < pickupSpawnPositions.Length; i++)
        {
            if (spawnedCount >= maxPickups)
            {
                break;
            }

            Vector3 guess = pickupSpawnPositions[i];

            Vector2 origin = new Vector2(guess.x, guess.y + 10f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 25f, mask);
            if (!hit.collider) continue;

            float surfaceY = hit.point.y;
            float yOffset = 0.7f; // slightly above the platform surface
            Vector3 finalPos = new Vector3(guess.x, surfaceY + yOffset, guess.z);

            // Overlap checks must use the final snapped position (y changes after raycast).
            bool overlapsOre = false;
            for (int j = 0; j < resourceSpawnPositions.Length; j++)
            {
                if (Vector3.Distance(finalPos, resourceSpawnPositions[j]) <= avoidResourceRadius)
                {
                    overlapsOre = true;
                    break;
                }
            }
            if (overlapsOre)
            {
                continue;
            }

            bool overlapsHealth = false;
            for (int j = 0; j < placedPickupPositions.Length; j++)
            {
                if (Vector3.Distance(finalPos, placedPickupPositions[j]) <= 1.0f)
                {
                    overlapsHealth = true;
                    break;
                }
            }
            if (overlapsHealth)
            {
                continue;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(oxygenPickupPrefab) as GameObject;
            if (instance == null) continue;

            instance.transform.SetPositionAndRotation(finalPos, Quaternion.identity);
            instance.transform.SetParent(oxygenRoot, true);
            Undo.RegisterCreatedObjectUndo(instance, "Spawn OxygenPickup");
            spawnedCount++;
        }
    }

    [MenuItem("Tools/Frontier Extraction/Fix Pink Sprites (Scene + Prefabs)")]
    public static void FixPinkSprites()
    {
        int fixedCount = 0;
        Material spriteMat = GetCompatibleSpriteMaterial();

        // Fix current open scene objects.
        SpriteRenderer[] sceneRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneRenderers.Length; i++)
        {
            if (sceneRenderers[i] == null)
            {
                continue;
            }

            if (sceneRenderers[i].sharedMaterial != spriteMat)
            {
                sceneRenderers[i].sharedMaterial = spriteMat;
                sceneRenderers[i].color = Color.white;
                EditorUtility.SetDirty(sceneRenderers[i]);
                fixedCount++;
            }
        }

        // Fix generated prefabs under Assets/Prefabs.
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir });
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                continue;
            }

            bool changed = false;
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                SpriteRenderer sr = renderers[r];
                if (sr == null || sr.sharedMaterial == spriteMat)
                {
                    continue;
                }

                sr.sharedMaterial = spriteMat;
                sr.color = Color.white;
                changed = true;
                fixedCount++;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fixed pink sprite materials. Renderers updated: " + fixedCount);
    }

    [MenuItem("Tools/Frontier Extraction/Create Default Upgrade Definitions")]
    public static void CreateDefaultUpgradeDefinitions()
    {
        EnsureDir("Assets/Data");
        EnsureDir("Assets/Data/Upgrades");

        UpgradeDefinition health = CreateUpgradeDefinitionAsset(
            "Assets/Data/Upgrades/upgrade_max_health.asset",
            "upgrade.max_health",
            "Max Health",
            "Increase suit integrity for longer runs.",
            UpgradeEffectType.IncreaseMaxHealth,
            15,
            5,
            new[] { new UpgradeCostTemplate("Iron", 3), new UpgradeCostTemplate("Crystal", 1) }
        );

        UpgradeDefinition damage = CreateUpgradeDefinitionAsset(
            "Assets/Data/Upgrades/upgrade_damage.asset",
            "upgrade.damage",
            "Weapon Damage",
            "Increase bullet and melee damage.",
            UpgradeEffectType.IncreaseDamage,
            2,
            5,
            new[] { new UpgradeCostTemplate("Iron", 4), new UpgradeCostTemplate("Crystal", 1) }
        );

        UpgradeDefinition mining = CreateUpgradeDefinitionAsset(
            "Assets/Data/Upgrades/upgrade_mining.asset",
            "upgrade.mining",
            "Mining Power",
            "Mine resource nodes faster.",
            UpgradeEffectType.IncreaseMiningSpeed,
            1,
            5,
            new[] { new UpgradeCostTemplate("Iron", 2), new UpgradeCostTemplate("Crystal", 2) }
        );

        UpgradeDefinition ultraDamage = CreateUpgradeDefinitionAsset(
            "Assets/Data/Upgrades/upgrade_uranium_damage.asset",
            "upgrade.uranium_damage",
            "Omega Weaponry",
            "High-tier damage upgrades using ultra-rare ore.",
            UpgradeEffectType.IncreaseDamage,
            4,
            3,
            new[]
            {
                new UpgradeCostTemplate("Iron", 8),
                new UpgradeCostTemplate("Crystal", 2),
                new UpgradeCostTemplate("Uranium", 1)
            }
        );

        GameObject systems = FindInScene("GameSystems");
        if (systems != null)
        {
            BaseUpgradeSystem baseSystem = systems.GetComponent<BaseUpgradeSystem>();
            if (baseSystem != null)
            {
                SerializedObject so = new SerializedObject(baseSystem);
                SerializedProperty upgradesProp = so.FindProperty("availableUpgrades");
                upgradesProp.arraySize = 0;
                // Order matters for the death menu slots: Option C should be weapon damage.
                List<UpgradeDefinition> defs = new List<UpgradeDefinition> { health, mining, damage, ultraDamage };
                int idx = 0;
                for (int i = 0; i < defs.Count; i++)
                {
                    if (defs[i] == null)
                    {
                        continue;
                    }

                    upgradesProp.InsertArrayElementAtIndex(idx);
                    upgradesProp.GetArrayElementAtIndex(idx).objectReferenceValue = defs[i];
                    idx++;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(baseSystem);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Default upgrade definitions created and assigned to BaseUpgradeSystem (if present).");
    }

    private static GameObject CreateBulletPrefab(Sprite sprite)
    {
        GameObject go = new GameObject("Bullet");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.2f;

        go.AddComponent<Bullet>();

        string path = PrefabDir + "/Bullet.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateEnemyPrefab(Sprite sprite)
    {
        GameObject go = new GameObject("Enemy");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 3;
        ApplyVisibleSpriteMaterial(renderer);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2f;
        rb.freezeRotation = true;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);

        var health = go.AddComponent<EnemyHealth>();
        var ai = go.AddComponent<EnemyAI2D>();

        Transform patrolA = new GameObject("PatrolA").transform;
        patrolA.SetParent(go.transform);
        patrolA.localPosition = new Vector3(-2f, 0f, 0f);

        Transform patrolB = new GameObject("PatrolB").transform;
        patrolB.SetParent(go.transform);
        patrolB.localPosition = new Vector3(2f, 0f, 0f);

        SerializedObject aiSO = new SerializedObject(ai);
        aiSO.FindProperty("patrolPointA").objectReferenceValue = patrolA;
        aiSO.FindProperty("patrolPointB").objectReferenceValue = patrolB;
        aiSO.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        aiSO.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject healthSO = new SerializedObject(health);
        healthSO.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabDir + "/Enemy.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateResourceItemPrefab(Sprite ironSprite, Sprite crystalSprite, Sprite uraniumSprite)
    {
        GameObject go = new GameObject("ResourceItem");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = ironSprite;
        renderer.sortingOrder = 4;
        ApplyVisibleSpriteMaterial(renderer);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        go.AddComponent<ResourceItem>();
        ResourceItem item = go.GetComponent<ResourceItem>();
        if (item != null)
        {
            SerializedObject itemSO = new SerializedObject(item);
            itemSO.FindProperty("ironItemSprite").objectReferenceValue = ironSprite;
            itemSO.FindProperty("crystalItemSprite").objectReferenceValue = crystalSprite;
            itemSO.FindProperty("uraniumItemSprite").objectReferenceValue = uraniumSprite;
            itemSO.ApplyModifiedPropertiesWithoutUndo();
        }

        string path = PrefabDir + "/ResourceItem.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateResourceNodePrefab(Sprite ironNodeSprite, GameObject resourceItemPrefab, Sprite crystalNodeSprite, Sprite uraniumNodeSprite)
    {
        GameObject go = new GameObject("ResourceNode");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = ironNodeSprite;
        renderer.sortingOrder = 2;
        ApplyVisibleSpriteMaterial(renderer);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        var node = go.AddComponent<ResourceNode>();

        SerializedObject nodeSO = new SerializedObject(node);
        nodeSO.FindProperty("resourceDropPrefab").objectReferenceValue = resourceItemPrefab != null
            ? resourceItemPrefab.GetComponent<ResourceItem>()
            : null;
        nodeSO.FindProperty("ironNodeSprite").objectReferenceValue = ironNodeSprite;
        nodeSO.FindProperty("crystalNodeSprite").objectReferenceValue = crystalNodeSprite;
        nodeSO.FindProperty("uraniumNodeSprite").objectReferenceValue = uraniumNodeSprite;
        nodeSO.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabDir + "/ResourceNode.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateUpgradeRowPrefab()
    {
        GameObject row = new GameObject("UpgradeOptionRowUI", typeof(RectTransform), typeof(UpgradeOptionRowUI));
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(640f, 120f);

        Text title = CreateText("TitleText", row.transform, new Vector2(16f, -14f), 20, TextAnchor.UpperLeft);
        Text desc = CreateText("DescriptionText", row.transform, new Vector2(16f, -46f), 14, TextAnchor.UpperLeft);
        Text level = CreateText("LevelText", row.transform, new Vector2(16f, -92f), 14, TextAnchor.UpperLeft);
        Text cost = CreateText("CostText", row.transform, new Vector2(250f, -92f), 14, TextAnchor.UpperLeft);

        GameObject buttonGO = new GameObject("PurchaseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(row.transform, false);
        RectTransform btnRt = buttonGO.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 0.5f);
        btnRt.anchorMax = new Vector2(1f, 0.5f);
        btnRt.pivot = new Vector2(1f, 0.5f);
        btnRt.sizeDelta = new Vector2(130f, 42f);
        btnRt.anchoredPosition = new Vector2(-16f, 0f);

        Image btnImage = buttonGO.GetComponent<Image>();
        btnImage.color = new Color(0.2f, 0.25f, 0.3f, 1f);
        Button button = buttonGO.GetComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = btnImage.color;
        cb.highlightedColor = new Color(0.3f, 0.35f, 0.45f, 1f);
        cb.pressedColor = new Color(0.15f, 0.2f, 0.25f, 1f);
        button.colors = cb;

        Text buttonText = CreateText("PurchaseButtonText", buttonGO.transform, new Vector2(0f, 0f), 16, TextAnchor.MiddleCenter);
        RectTransform btr = buttonText.GetComponent<RectTransform>();
        btr.anchorMin = Vector2.zero;
        btr.anchorMax = Vector2.one;
        btr.offsetMin = Vector2.zero;
        btr.offsetMax = Vector2.zero;
        buttonText.text = "Buy";

        UpgradeOptionRowUI rowUI = row.GetComponent<UpgradeOptionRowUI>();
        SerializedObject so = new SerializedObject(rowUI);
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("descriptionText").objectReferenceValue = desc;
        so.FindProperty("levelText").objectReferenceValue = level;
        so.FindProperty("costText").objectReferenceValue = cost;
        so.FindProperty("purchaseButton").objectReferenceValue = button;
        so.FindProperty("purchaseButtonText").objectReferenceValue = buttonText;
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabDir + "/UI/UpgradeOptionRowUI.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(row, path);
        Object.DestroyImmediate(row);
        return prefab;
    }

    private static GameObject CreatePlayerPrefab(Sprite sprite, GameObject bulletPrefab)
    {
        GameObject go = new GameObject("Player");
        go.tag = "Player";
        go.layer = LayerMask.NameToLayer("Default");

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 6;
        ApplyVisibleSpriteMaterial(renderer);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.8f, 0.95f);
        col.offset = new Vector2(0f, 0f);

        var movement = go.AddComponent<PlayerMovement2D>();
        var stats = go.AddComponent<PlayerStats>();
        go.AddComponent<PlayerUpgradeEffects>();
        var shooting = go.AddComponent<PlayerShooting>();

        Transform groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(go.transform);
        groundCheck.localPosition = new Vector3(0f, -0.52f, 0f);

        Transform firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(go.transform);
        firePoint.localPosition = new Vector3(0.5f, 0f, 0f);

        SerializedObject moveSO = new SerializedObject(movement);
        moveSO.FindProperty("moveSpeed").floatValue = 10.5f;
        moveSO.FindProperty("acceleration").floatValue = 45f;
        moveSO.FindProperty("deceleration").floatValue = 55f;
        moveSO.FindProperty("jumpForce").floatValue = 18f;
        moveSO.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        moveSO.FindProperty("groundCheck").objectReferenceValue = groundCheck;
        moveSO.FindProperty("groundLayer").intValue = LayerMask.GetMask("Ground");
        moveSO.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject shootSO = new SerializedObject(shooting);
        shootSO.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab != null ? bulletPrefab.GetComponent<Bullet>() : null;
        shootSO.FindProperty("firePoint").objectReferenceValue = firePoint;
        shootSO.FindProperty("playerSprite").objectReferenceValue = renderer;
        shootSO.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject statsSO = new SerializedObject(stats);
        statsSO.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabDir + "/Player.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateHudCanvasPrefab(GameObject rowPrefab)
    {
        GameObject canvasGO = new GameObject("HUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(HUDController), typeof(BaseUpgradeMenuUI), typeof(InventoryDebugDisplay), typeof(DeathUpgradeMenuUI));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRt = canvasGO.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(1920f, 1080f);

        Slider suitSlider = CreateSlider("SuitIntegritySlider", canvasGO.transform, new Vector2(190f, -40f));
        Slider oxygenSlider = CreateSlider("OxygenSlider", canvasGO.transform, new Vector2(190f, -78f));
        Text suitText = CreateText("SuitLabel", canvasGO.transform, new Vector2(20f, -32f), 18, TextAnchor.MiddleLeft);
        Text oxygenText = CreateText("OxygenLabel", canvasGO.transform, new Vector2(20f, -70f), 18, TextAnchor.MiddleLeft);
        Text resourcesText = CreateText("ResourcesText", canvasGO.transform, new Vector2(20f, -120f), 18, TextAnchor.UpperLeft);
        Text biomeText = CreateText("BiomeLabel", canvasGO.transform, new Vector2(20f, -250f), 18, TextAnchor.MiddleLeft);
        Text promptText = CreateCenteredText("PromptText", canvasGO.transform, new Vector2(0f, 120f), 22);
        Text extractionText = CreateCenteredText("ExtractionStatusText", canvasGO.transform, new Vector2(0f, 80f), 20);

        // Extract button so players can bank materials at any time.
        GameObject extractButtonGO = new GameObject("ExtractButton",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(ExtractNowButtonUI));
        extractButtonGO.transform.SetParent(canvasGO.transform, false);
        RectTransform extractRt = extractButtonGO.GetComponent<RectTransform>();
        extractRt.anchorMin = new Vector2(0.5f, 0f);
        extractRt.anchorMax = new Vector2(0.5f, 0f);
        extractRt.pivot = new Vector2(0.5f, 0f);
        extractRt.sizeDelta = new Vector2(360f, 70f);
        extractRt.anchoredPosition = new Vector2(0f, 180f);

        Image extractImg = extractButtonGO.GetComponent<Image>();
        extractImg.color = new Color(0.18f, 0.24f, 0.32f, 1f);

        Button extractBtn = extractButtonGO.GetComponent<Button>();
        if (extractBtn != null)
        {
            ColorBlock cb = extractBtn.colors;
            cb.normalColor = extractImg.color;
            cb.highlightedColor = new Color(0.26f, 0.34f, 0.46f, 1f);
            cb.pressedColor = new Color(0.14f, 0.2f, 0.28f, 1f);
            extractBtn.colors = cb;
        }

        Text extractBtnText = CreateCenteredText("ExtractButtonText", extractButtonGO.transform, Vector2.zero, 30);
        RectTransform ebRt = extractBtnText.GetComponent<RectTransform>();
        ebRt.sizeDelta = new Vector2(320f, 50f);
        extractBtnText.text = "Extract (X)";

        // Debug inventory: move under Resources on the left and make it larger/readable.
        Text debugText = CreateText("InventoryDebugText", canvasGO.transform, new Vector2(20f, -155f), 18, TextAnchor.UpperLeft, new Vector2(420f, 240f), false);

        // Bigger pause panel = more space for inventory texts and fewer overlap/clipping issues.
        GameObject pausePanel = CreatePanel("PausePanel", canvasGO.transform, new Vector2(0f, 0f), new Vector2(720f, 420f), new Color(0f, 0f, 0f, 0.7f));
        pausePanel.SetActive(false);
        CreateCenteredText("PauseLabel", pausePanel.transform, new Vector2(0f, 20f), 36).text = "PAUSED";

        GameObject menuRoot = CreatePanel("UpgradeMenuRoot", canvasGO.transform, new Vector2(0f, 0f), new Vector2(860f, 560f), new Color(0.05f, 0.08f, 0.12f, 0.9f));
        menuRoot.SetActive(false);
        CreateCenteredText("UpgradeTitle", menuRoot.transform, new Vector2(0f, 240f), 30).text = "Base Upgrades";

        GameObject container = new GameObject("RowContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        container.transform.SetParent(menuRoot.transform, false);
        RectTransform containerRt = container.GetComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.pivot = new Vector2(0.5f, 0.5f);
        containerRt.sizeDelta = new Vector2(780f, 440f);
        containerRt.anchoredPosition = new Vector2(0f, -20f);

        VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        HUDController hud = canvasGO.GetComponent<HUDController>();
        SerializedObject hudSO = new SerializedObject(hud);
        hudSO.FindProperty("suitIntegritySlider").objectReferenceValue = suitSlider;
        hudSO.FindProperty("oxygenSlider").objectReferenceValue = oxygenSlider;
        hudSO.FindProperty("suitIntegrityLabel").objectReferenceValue = suitText;
        hudSO.FindProperty("oxygenLabel").objectReferenceValue = oxygenText;
        hudSO.FindProperty("resourcesText").objectReferenceValue = resourcesText;
        hudSO.FindProperty("biomeLabel").objectReferenceValue = biomeText;
        hudSO.FindProperty("promptText").objectReferenceValue = promptText;
        hudSO.FindProperty("extractionStatusText").objectReferenceValue = extractionText;
        hudSO.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        InventoryDebugDisplay debugDisplay = canvasGO.GetComponent<InventoryDebugDisplay>();
        SerializedObject debugSO = new SerializedObject(debugDisplay);
        debugSO.FindProperty("debugText").objectReferenceValue = debugText;
        debugSO.ApplyModifiedPropertiesWithoutUndo();

        BaseUpgradeMenuUI menuUI = canvasGO.GetComponent<BaseUpgradeMenuUI>();
        SerializedObject menuSO = new SerializedObject(menuUI);
        menuSO.FindProperty("menuRoot").objectReferenceValue = menuRoot;
        menuSO.FindProperty("rowContainer").objectReferenceValue = container.transform;
        menuSO.FindProperty("rowPrefab").objectReferenceValue = rowPrefab != null ? rowPrefab.GetComponent<UpgradeOptionRowUI>() : null;
        menuSO.ApplyModifiedPropertiesWithoutUndo();

        GameObject deathRoot = CreatePanel("DeathUpgradeMenuRoot", canvasGO.transform, Vector2.zero, new Vector2(760f, 460f), new Color(0f, 0f, 0f, 0.85f));
        deathRoot.SetActive(false);
        Text deathTitle = CreateCenteredText("DeathTitle", deathRoot.transform, new Vector2(0f, 170f), 46);
        deathTitle.text = "Run Failed";
        Text deathDesc = CreateCenteredText("DeathDescription", deathRoot.transform, new Vector2(0f, 118f), 28);
        deathDesc.text = "Choose one permanent upgrade before redeploying.";

        Button btnA = CreateMenuButton("OptionAButton", deathRoot.transform, new Vector2(0f, 62f), out Text btnAText);
        Button btnB = CreateMenuButton("OptionBButton", deathRoot.transform, new Vector2(0f, 2f), out Text btnBText);
        Button btnC = CreateMenuButton("OptionCButton", deathRoot.transform, new Vector2(0f, -58f), out Text btnCText);
        Button continueBtn = CreateMenuButton("ContinueButton", deathRoot.transform, new Vector2(0f, -146f), out Text continueText);
        continueText.text = "Continue";

        DeathUpgradeMenuUI deathMenu = canvasGO.GetComponent<DeathUpgradeMenuUI>();
        SerializedObject deathSO = new SerializedObject(deathMenu);
        deathSO.FindProperty("menuRoot").objectReferenceValue = deathRoot;
        deathSO.FindProperty("titleText").objectReferenceValue = deathTitle;
        deathSO.FindProperty("descriptionText").objectReferenceValue = deathDesc;
        deathSO.FindProperty("optionAButton").objectReferenceValue = btnA;
        deathSO.FindProperty("optionBButton").objectReferenceValue = btnB;
        deathSO.FindProperty("optionCButton").objectReferenceValue = btnC;
        deathSO.FindProperty("optionALabel").objectReferenceValue = btnAText;
        deathSO.FindProperty("optionBLabel").objectReferenceValue = btnBText;
        deathSO.FindProperty("optionCLabel").objectReferenceValue = btnCText;
        deathSO.FindProperty("continueButton").objectReferenceValue = continueBtn;
        deathSO.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabDir + "/UI/HUDCanvas.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasGO, path);
        Object.DestroyImmediate(canvasGO);
        return prefab;
    }

    private static GameObject CreateGameSystemsPrefab(
        GameObject enemyPrefab,
        GameObject resourceNodePrefab,
        GameObject bulletPrefab,
        GameObject oxygenPickupPrefab,
        Sprite floraEnemySprite,
        Sprite faunaEnemySprite,
        Sprite shooterAlienSprite)
    {
        GameObject root = new GameObject("GameSystems");
        GameManager gameManager = root.AddComponent<GameManager>();
        root.AddComponent<InventorySystem>();
        root.AddComponent<ExtractedResourceBank>();
        root.AddComponent<PermanentUpgradeSystem>();
        root.AddComponent<BaseUpgradeSystem>();
        root.AddComponent<ExtractionSystem>();
        LevelSetupSpawner spawner = root.AddComponent<LevelSetupSpawner>();

        Transform enemiesRoot = new GameObject("SpawnedEnemies").transform;
        enemiesRoot.SetParent(root.transform);
        enemiesRoot.localPosition = Vector3.zero;

        Transform resourcesRoot = new GameObject("SpawnedResources").transform;
        resourcesRoot.SetParent(root.transform);
        resourcesRoot.localPosition = Vector3.zero;

        SerializedObject gmSO = new SerializedObject(gameManager);
        gmSO.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject spawnerSO = new SerializedObject(spawner);
        spawnerSO.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
        spawnerSO.FindProperty("resourceNodePrefab").objectReferenceValue = resourceNodePrefab;
        spawnerSO.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;
        spawnerSO.FindProperty("oxygenPickupPrefab").objectReferenceValue = oxygenPickupPrefab;
        spawnerSO.FindProperty("floraEnemySprite").objectReferenceValue = floraEnemySprite;
        spawnerSO.FindProperty("faunaEnemySprite").objectReferenceValue = faunaEnemySprite;
        spawnerSO.FindProperty("shooterEnemySprite").objectReferenceValue = shooterAlienSprite;
        spawnerSO.FindProperty("spawnedEnemiesRoot").objectReferenceValue = enemiesRoot;
        spawnerSO.FindProperty("spawnedResourcesRoot").objectReferenceValue = resourcesRoot;
        spawnerSO.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabDir + "/GameSystems.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Text CreateText(string name, Transform parent, Vector2 anchoredPos, int fontSize, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(420f, 24f);

        Text text = go.GetComponent<Text>();
        text.font = GetSafeBuiltinFont();
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = new Color(0.92f, 0.95f, 1f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.text = name;
        return text;
    }

    private static Text CreateText(string name, Transform parent, Vector2 anchoredPos, int fontSize, TextAnchor anchor, Vector2 size, bool topRight)
    {
        Text text = CreateText(name, parent, anchoredPos, fontSize, anchor);
        RectTransform rt = text.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        if (topRight)
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
        }
        return text;
    }

    private static Text CreateCenteredText(string name, Transform parent, Vector2 anchoredPos, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900f, 44f);
        rt.anchoredPosition = anchoredPos;

        Text text = go.GetComponent<Text>();
        text.font = GetSafeBuiltinFont();
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = name;
        return text;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 topLeftAnchoredPos)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = topLeftAnchoredPos;
        rt.sizeDelta = new Vector2(280f, 20f);

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0f);
        faRt.anchorMax = new Vector2(1f, 1f);
        faRt.offsetMin = new Vector2(2f, 2f);
        faRt.offsetMax = new Vector2(-2f, -2f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.35f, 0.95f, 0.65f, 1f);

        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.targetGraphic = fill.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image image = panel.GetComponent<Image>();
        image.color = color;
        return panel;
    }

    private static Button CreateMenuButton(string name, Transform parent, Vector2 anchoredPos, out Text label)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);
        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(620f, 68f);

        Image img = buttonGO.GetComponent<Image>();
        img.color = new Color(0.18f, 0.24f, 0.32f, 1f);

        Button btn = buttonGO.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = img.color;
        cb.highlightedColor = new Color(0.26f, 0.34f, 0.46f, 1f);
        cb.pressedColor = new Color(0.14f, 0.2f, 0.28f, 1f);
        btn.colors = cb;

        label = CreateCenteredText(name + "_Label", buttonGO.transform, Vector2.zero, 24);
        label.text = "Upgrade";
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        return btn;
    }

    private static Sprite CreatePixelSprite(string name, Color fill, Color border)
    {
        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        if (!File.Exists(fullPath))
        {
            WritePixelPng(fullPath, fill, border);
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (loaded != null)
        {
            return loaded;
        }

        Debug.LogWarning("Failed to load generated sprite at: " + assetPath + ". Using runtime fallback sprite.");
        return CreateFallbackSprite(name, fill, border);
    }

    private static Sprite CreateOxygenPixelSprite(string name, Color fill, Color border)
    {
        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        // Always write the oxygen sprite so changes to the pixel pattern take effect.
        WriteOxygenPixelPng(fullPath, fill, border);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (loaded != null)
        {
            return loaded;
        }

        // Fallback: use the default square sprite if something went wrong.
        return CreateFallbackSprite(name, fill, border);
    }

    private static Sprite CreateAstronautSprite(string name, int seed)
    {
        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        // Always write the sprite so you can iterate on the look.
        WriteAstronautPlayerPng(fullPath, seed);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return loaded != null ? loaded : CreateFallbackSprite(name, new Color(0.4f, 0.8f, 1f), new Color(0.12f, 0.22f, 0.35f));
    }

    private static Sprite CreateCylindricalBulletSprite(string name, int seed)
    {
        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        // Always overwrite to iterate quickly.
        WriteCylindricalBulletPng(fullPath, 16, seed);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return loaded != null ? loaded : CreateFallbackSprite(name, new Color(1f, 0.95f, 0.35f), new Color(0.9f, 0.65f, 0.15f));
    }

    private static Sprite CreateFloraEnemySprite(string name, int seed)
    {
        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        WriteFloraEnemyPng(fullPath, seed);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return loaded != null ? loaded : CreateFallbackSprite(name, new Color(0.25f, 1f, 0.55f), new Color(0.06f, 0.25f, 0.1f));
    }

    private static Sprite CreateFaunaEnemySprite(string name, int seed)
    {
        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        WriteFaunaEnemyPng(fullPath, seed);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return loaded != null ? loaded : CreateFallbackSprite(name, new Color(0.95f, 0.7f, 0.25f), new Color(0.3f, 0.12f, 0.06f));
    }

    private static Sprite CreateAlienShooterEnemySprite(string name, int seed)
    {
        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        WriteAlienShooterEnemyPng(fullPath, seed);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return loaded != null ? loaded : CreateFallbackSprite(name, new Color(0.6f, 0.9f, 1f), new Color(0.15f, 0.2f, 0.35f));
    }

    private static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string child = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }

    private static float Hash01(int x, int y, int seed)
    {
        // Deterministic integer hash -> [0..1)
        int h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
        h = (h ^ (h >> 13)) * 1274126177;
        uint uh = (uint)(h & 0x7fffffff);
        return uh / (float)0x7fffffff;
    }

    private static Sprite CreateAlienTileSprite(
        string name,
        Color baseFill,
        Color borderColor,
        Color accentColor,
        float accentChance01,
        int seed)
    {
        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        // Always overwrite so you can iterate on art style.
        WriteAlienTilePng(fullPath, 16, baseFill, borderColor, accentColor, accentChance01, seed, drawCross: false);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return loaded != null ? loaded : CreateFallbackSprite(name, baseFill, borderColor);
    }

    private static Sprite CreateAlienHealthPickupSprite(
        string name,
        Color fill,
        Color border,
        Color cross,
        int seed)
    {
        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        // Cross-only (transparent outside) so it's not a "square with a cross".
        WriteAlienHealthCrossPng(fullPath, 16, fill, border, cross, seed);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return loaded != null ? loaded : CreateFallbackSprite(name, fill, border);
    }

    private static Sprite CreateAlienOxygenPickupSprite(string name, Color fill, Color border, int seed)
    {
        // Oxygen stays circular and transparent outside.
        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        WriteAlienOxygenPixelPng(fullPath, 16, fill, border, seed);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return loaded != null ? loaded : CreateFallbackSprite(name, fill, border);
    }

    private static Sprite CreateAlienBackgroundSprite(string name, int seed)
    {
        // Larger resolution -> fewer artifacts when scaled up.
        int w = 128;
        int h = 72;

        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        WriteAlienBackgroundPng(fullPath, w, h, seed);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return loaded != null ? loaded : CreateFallbackSprite(name, new Color(0.1f, 0.1f, 0.2f), new Color(0.05f, 0.05f, 0.1f));
    }

    private static Sprite CreateAlienSquareTileSprite(
        string name,
        Color baseFill,
        Color borderColor,
        Color accentColor,
        float accentChance01,
        int seed)
    {
        string assetPath = ArtDir + "/" + name + ".png";
        string fullPath = ToFullProjectPath(assetPath);
        string fullDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }

        // Always overwrite so you can iterate on the square-block look.
        WriteAlienSquareTilePng(fullPath, 16, baseFill, borderColor, accentColor, accentChance01, seed, drawCross: false);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return loaded != null ? loaded : CreateFallbackSprite(name, baseFill, borderColor);
    }

    private static void WriteAlienTilePng(
        string fullPath,
        int size,
        Color baseFill,
        Color borderColor,
        Color accentColor,
        float accentChance01,
        int seed,
        bool drawCross)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(0f, 0f, 0f, 0f);

        // Crystal-like ore: union of a few deterministic shard triangles + facet lines.
        int cx = size / 2;
        int yBase = size - 1;
        int mid = size / 2;

        // Seed-derived shard vertices (bigger/more blocky facets).
        int tA = 1 + Mathf.FloorToInt(Hash01(0, 0, seed) * 7f); // 1..7
        int tB = 0 + Mathf.FloorToInt(Hash01(9, 2, seed) * 8f); // 0..7
        int tC = 2 + Mathf.FloorToInt(Hash01(4, 7, seed) * 7f); // 2..8

        int xA = cx - 7 + Mathf.FloorToInt(Hash01(2, 3, seed) * 4f); // cx-7..cx-4
        int xB = cx - 2 + Mathf.FloorToInt(Hash01(7, 5, seed) * 6f); // cx-2..cx+3
        int xC = cx + 1 + Mathf.FloorToInt(Hash01(1, 8, seed) * 7f); // cx+1..cx+8

        // Quantize to a 2px grid to make the silhouette feel "blocky".
        tA = Mathf.Clamp(Mathf.RoundToInt(tA / 2f) * 2, 0, size - 1);
        tB = Mathf.Clamp(Mathf.RoundToInt(tB / 2f) * 2, 0, size - 1);
        tC = Mathf.Clamp(Mathf.RoundToInt(tC / 2f) * 2, 0, size - 1);
        xA = Mathf.Clamp(Mathf.RoundToInt(xA / 2f) * 2, 0, size - 1);
        xB = Mathf.Clamp(Mathf.RoundToInt(xB / 2f) * 2, 0, size - 1);
        xC = Mathf.Clamp(Mathf.RoundToInt(xC / 2f) * 2, 0, size - 1);

        // Base edges (bigger caps = less thin tops).
        Vector2Int a0 = new Vector2Int(cx - 8, yBase);
        Vector2Int a1 = new Vector2Int(xA, tA);
        Vector2Int a2 = new Vector2Int(cx - 1, yBase);

        Vector2Int b0 = new Vector2Int(cx - 4, yBase);
        Vector2Int b1 = new Vector2Int(xB, tB);
        Vector2Int b2 = new Vector2Int(cx + 3, yBase);

        Vector2Int c0 = new Vector2Int(cx - 1, yBase);
        Vector2Int c1 = new Vector2Int(xC, tC);
        Vector2Int c2 = new Vector2Int(cx + 8, yBase);

        // Helper: point-in-triangle (barycentric sign method).
        bool InTri(int px, int py, Vector2Int p0, Vector2Int p1, Vector2Int p2)
        {
            float x = px;
            float y = py;
            float x0 = p0.x; float y0 = p0.y;
            float x1 = p1.x; float y1 = p1.y;
            float x2 = p2.x; float y2 = p2.y;

            float denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2);
            if (Mathf.Abs(denom) < 0.0001f) return false;

            float a = ((y1 - y2) * (x - x2) + (x2 - x1) * (y - y2)) / denom;
            float b = ((y2 - y0) * (x - x2) + (x0 - x2) * (y - y2)) / denom;
            float d = 1f - a - b;

            return a >= 0f && b >= 0f && d >= 0f;
        }

        // Helper: near a line segment (facet ridge).
        bool NearEdge(int px, int py, Vector2Int p0, Vector2Int p1)
        {
            // Distance from point to infinite line using cross product magnitude.
            float x0 = p0.x; float y0 = p0.y;
            float x1 = p1.x; float y1 = p1.y;
            float dx = x1 - x0;
            float dy = y1 - y0;
            if (Mathf.Abs(dx) < 0.0001f && Mathf.Abs(dy) < 0.0001f) return false;

            float num = Mathf.Abs(dy * px - dx * py + x1 * y0 - y1 * x0);
            float den = Mathf.Sqrt(dx * dx + dy * dy);
            float dist = num / Mathf.Max(0.0001f, den);

            // Only consider pixels that project onto the segment.
            float t = ((px - x0) * dx + (py - y0) * dy) / Mathf.Max(0.0001f, dx * dx + dy * dy);
            if (t < -0.1f || t > 1.1f) return false;

            return dist <= 0.6f;
        }

        bool InOreRaw(int px, int py)
        {
            return
                InTri(px, py, a0, a1, a2) ||
                InTri(px, py, b0, b1, b2) ||
                InTri(px, py, c0, c1, c2);
        }

        // Top rounding: if we're in the top band, dilate slightly so the cap feels thicker/rounded.
        bool InOre(int px, int py)
        {
            if (px < 0 || px >= size || py < 0 || py >= size) return false;

            if (InOreRaw(px, py)) return true;

            int topRows = 4;
            if (py >= (size - topRows))
            {
                if (InOreRaw(px - 1, py)) return true;
                if (InOreRaw(px + 1, py)) return true;
                if (InOreRaw(px, py - 1)) return true;
                if (InOreRaw(px, py + 1)) return true;
            }

            return false;
        }

        // Raster (evaluate in flipped Y-space so shards point the right way).
        for (int y = 0; y < size; y++)
        {
            int yEval = size - 1 - y;
            for (int x = 0; x < size; x++)
            {
                if (!InOre(x, yEval))
                {
                    tex.SetPixel(x, y, transparent);
                    continue;
                }

                // Silhouette border (use flipped-space for inside/outside checks).
                bool isBorder = false;

                // 4-neighborhood gives crisp borders.
                if (!InOre(x - 1, yEval) || !InOre(x + 1, yEval) || !InOre(x, yEval - 1) || !InOre(x, yEval + 1))
                {
                    if (x > 0 && x < size - 1 && y > 0 && y < size - 1)
                    {
                        isBorder = true;
                    }
                }

                // Always border on the sprite edges.
                if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                {
                    isBorder = true;
                }

                Color c = isBorder ? borderColor : baseFill;

                if (!isBorder)
                {
                    // Facet ridges: edges of shards (also evaluated in flipped space).
                    bool ridge =
                        NearEdge(x, yEval, a1, a2) ||
                        NearEdge(x, yEval, a0, a1) ||
                        NearEdge(x, yEval, b1, b2) ||
                        NearEdge(x, yEval, b0, b1) ||
                        NearEdge(x, yEval, c1, c2) ||
                        NearEdge(x, yEval, c0, c1);

                    if (ridge)
                    {
                        c = Color.Lerp(c, borderColor, 0.75f);
                    }
                    else
                    {
                        // Highlights on some facets (deterministic).
                        float h = Hash01(x, yEval, seed + 222);
                        float sheen = (x + yEval) % 5 == 0 ? 1f : 0f;
                        if (h < accentChance01 || sheen > 0f)
                        {
                            c = Color.Lerp(c, accentColor, 0.55f + 0.25f * sheen);
                        }
                    }

                    // Optional plus sign for health pickup (kept in sprite-space).
                    if (drawCross)
                    {
                        bool crossPixel = x == mid || y == mid || x == mid - 1 || y == mid - 1;
                        if (crossPixel) c = Color.Lerp(c, accentColor, 0.85f);
                    }
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    // Square-block variant used for ground/platform so it stays crisp and non-organic.
    private static void WriteAlienSquareTilePng(
        string fullPath,
        int size,
        Color baseFill,
        Color borderColor,
        Color accentColor,
        float accentChance01,
        int seed,
        bool drawCross)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        int mid = size / 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x <= 1 || x >= size - 2 || y <= 1 || y >= size - 2;
                Color c = border ? borderColor : baseFill;

                if (!border)
                {
                    // Accent speckles
                    float h = Hash01(x, y, seed);
                    if (h < accentChance01)
                    {
                        float strength = 0.5f + Hash01(x + 17, y + 9, seed + 11) * 0.5f;
                        c = Color.Lerp(c, accentColor, strength);
                    }

                    // Subtle “crack/vein” diagonal lines
                    int diag = x - y;
                    if (Mathf.Abs(diag - (mid - 2)) <= 0 || Mathf.Abs(diag - (mid + 1)) <= 0)
                    {
                        float veinH = Hash01(x + 3, y + 7, seed + 99);
                        if (veinH < 0.55f)
                        {
                            c = Color.Lerp(c, accentColor, 0.6f);
                        }
                    }

                    // Optional plus sign for health pickup
                    if (drawCross)
                    {
                        bool crossPixel = x == mid || y == mid || x == mid - 1 || y == mid - 1;
                        if (crossPixel)
                        {
                            c = Color.Lerp(c, accentColor, 0.85f);
                        }
                    }
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void WriteAlienOxygenPixelPng(string fullPath, int size, Color fill, Color border, int seed)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float innerRadius = 5.0f;
        float outerRadius = 7.0f;

        // Seed chooses highlight pattern.
        int mode = Mathf.Abs(seed) % 3;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > outerRadius)
                {
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                    continue;
                }

                if (dist > innerRadius)
                {
                    tex.SetPixel(x, y, border);
                    continue;
                }

                // Inner fill + highlight accents.
                Color c = fill;
                bool highlight = false;
                if (mode == 0)
                {
                    highlight = Mathf.Abs(dx) <= 1.0f || Mathf.Abs(dy) <= 1.0f; // plus
                }
                else if (mode == 1)
                {
                    highlight = (x == y) || (x + y == size - 1); // X
                }
                else
                {
                    highlight = (x == midValue(size)) || (y == midValue(size)); // thick cross
                    if (!highlight)
                    {
                        float h = Hash01(x, y, seed);
                        highlight = h < 0.08f;
                    }
                }

                if (highlight)
                {
                    c = Color.Lerp(fill, Color.white, 0.35f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static int midValue(int size)
    {
        return size / 2;
    }

    private static void WriteAlienBackgroundPng(string fullPath, int width, int height, int seed)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color baseCol = new Color(0.02f, 0.02f, 0.07f, 1f);
        Color hazeA = new Color(0.15f, 0.60f, 0.35f, 1f);
        Color hazeB = new Color(0.55f, 0.25f, 0.75f, 1f);

        // Pre-picked blob centers (deterministic).
        Vector2[] centers = new Vector2[]
        {
            new Vector2(width * 0.22f, height * 0.35f),
            new Vector2(width * 0.68f, height * 0.25f),
            new Vector2(width * 0.58f, height * 0.78f),
        };
        float[] sigmas = new float[] { width * 0.22f, width * 0.18f, width * 0.25f };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float px = x;
                float py = y;

                // Haze blobs
                float blob0 = Mathf.Exp(-((px - centers[0].x) * (px - centers[0].x) + (py - centers[0].y) * (py - centers[0].y)) / (2f * sigmas[0] * sigmas[0]));
                float blob1 = Mathf.Exp(-((px - centers[1].x) * (px - centers[1].x) + (py - centers[1].y) * (py - centers[1].y)) / (2f * sigmas[1] * sigmas[1]));
                float blob2 = Mathf.Exp(-((px - centers[2].x) * (px - centers[2].x) + (py - centers[2].y) * (py - centers[2].y)) / (2f * sigmas[2] * sigmas[2]));

                float haze = Mathf.Clamp01(blob0 * 0.85f + blob1 * 0.75f + blob2 * 0.65f);
                Color hazeCol = Color.Lerp(hazeA, hazeB, blob1 / Mathf.Max(0.0001f, blob0 + blob1));

                // Starfield: sparse bright dots.
                float h = Hash01(x, y, seed);
                float star = 0f;
                if (h < 0.010f)
                {
                    float sizeRoll = Hash01(x + 11, y + 31, seed + 99);
                    star = sizeRoll < 0.08f ? 1f : 0.7f;
                    // Slight chroma
                    float chroma = Hash01(x + 7, y + 13, seed + 7);
                    hazeCol = Color.Lerp(hazeCol, Color.white, chroma * 0.8f);
                }

                Color c = baseCol;
                c = Color.Lerp(c, hazeCol, haze * 0.65f);
                c += star * 0.9f * Color.white;

                // Clamp
                c.r = Mathf.Clamp01(c.r);
                c.g = Mathf.Clamp01(c.g);
                c.b = Mathf.Clamp01(c.b);
                c.a = 1f;

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void EnsureTag(string tagName)
    {
        string[] tags = InternalEditorUtility.tags;
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i] == tagName)
            {
                return;
            }
        }

        InternalEditorUtility.AddTag(tagName);
    }

    private static void WritePixelPng(string fullPath, Color fill, Color border)
    {
        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool isBorder = x <= 1 || x >= 14 || y <= 1 || y >= 14;
                tex.SetPixel(x, y, isBorder ? border : fill);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void WriteOxygenPixelPng(string fullPath, Color fill, Color border)
    {
        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        tex.name = "oxygen_tex";

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

                // Make everything outside the circle transparent.
                if (dist > outerRadius)
                {
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                    continue;
                }

                // Border ring.
                if (dist > innerRadius)
                {
                    tex.SetPixel(x, y, border);
                    continue;
                }

                // Inner core + a tiny "cross" highlight to differentiate from resource nodes.
                Color c = fill;
                bool highlight = Mathf.Abs(dx) <= 1.0f || Mathf.Abs(dy) <= 1.0f;
                if (highlight)
                {
                    c = Color.Lerp(fill, Color.white, 0.35f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void WriteAlienHealthCrossPng(string fullPath, int size, Color fill, Color border, Color cross, int seed)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(0f, 0f, 0f, 0f);

        // Pixel-art cross centered in the 16x16 tile.
        int mid = size / 2; // 8
        int coreMin = mid - 1; // 7
        int coreMax = mid;     // 8
        int barStart = 2;
        int barEnd = size - 3; // 13

        // Border thickness is one pixel larger than core.
        int borderMin = mid - 2; // 6
        int borderMax = mid + 1; // 9

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = transparent;

                // Core cross (thicker center).
                bool inCore =
                    ((x >= coreMin && x <= coreMax) && (y >= barStart && y <= barEnd)) ||
                    ((y >= coreMin && y <= coreMax) && (x >= barStart && x <= barEnd));

                // Outer cross border (still cross-only, not a square tile).
                bool inBorder =
                    ((x >= borderMin && x <= borderMax) && (y >= barStart && y <= barEnd)) ||
                    ((y >= borderMin && y <= borderMax) && (x >= barStart && x <= barEnd));

                if (inCore)
                {
                    c = cross;
                    // A tiny alien glow sparkle.
                    float h = Hash01(x, y, seed + 505);
                    if (h < 0.03f) c = Color.Lerp(c, Color.white, 0.25f);
                }
                else if (inBorder)
                {
                    c = border;
                    // Subtle inner tint using `fill` so the cross doesn't look too flat.
                    float h = Hash01(x, y, seed + 777);
                    if (h < 0.05f) c = Color.Lerp(c, fill, 0.25f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void WriteCylindricalBulletPng(string fullPath, int size, int seed)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(0f, 0f, 0f, 0f);

        Color body = new Color(1f, 0.95f, 0.35f, 1f);
        Color bodyShadow = new Color(0.8f, 0.65f, 0.18f, 1f);
        Color border = new Color(0.9f, 0.65f, 0.15f, 1f);
        Color highlight = new Color(1f, 1f, 0.85f, 1f);

        // Horizontally stretched capsule (bullet points to the right by default).
        float cx = (size - 1) * 0.5f; // 7.5
        float cy = (size - 1) * 0.5f; // 7.5
        float a = 6.6f; // horizontal radius
        float b = 2.5f; // vertical radius

        // Border thickness in "eq" space.
        float innerEq = 0.86f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float eq = (dx * dx) / (a * a) + (dy * dy) / (b * b);

                if (eq > 1f)
                {
                    tex.SetPixel(x, y, transparent);
                    continue;
                }

                // Border ring.
                if (eq > innerEq)
                {
                    tex.SetPixel(x, y, border);
                    continue;
                }

                // Shading to look cylindrical: darker on left, brighter on right.
                float t = Mathf.InverseLerp(-a, a, dx);
                Color c = Color.Lerp(bodyShadow, body, t);

                // Specular highlight stripe.
                bool inStripe = (x >= Mathf.FloorToInt(cx + 1f) && x <= Mathf.FloorToInt(cx + 2f)) && Mathf.Abs(dy) <= 1.1f;
                if (inStripe)
                {
                    c = Color.Lerp(c, highlight, 0.9f);
                }

                // Tiny deterministic speckle (keeps it from looking flat).
                float h = Hash01(x, y, seed + 123);
                if (h < 0.04f)
                {
                    c = Color.Lerp(c, Color.white, 0.25f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void WriteAstronautPlayerPng(string fullPath, int seed)
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(0f, 0f, 0f, 0f);
        Color helmetFill = new Color(0.72f, 0.88f, 1f, 1f);
        Color helmetBorder = new Color(0.12f, 0.42f, 0.6f, 1f);
        Color suitFill = new Color(0.32f, 0.66f, 1f, 1f);
        Color suitBorder = new Color(0.06f, 0.22f, 0.35f, 1f);
        Color visorFill = new Color(0.04f, 0.25f, 0.45f, 1f);
        Color visorHighlight = new Color(0.88f, 0.98f, 1f, 1f);
        Color accent = new Color(1f, 0.92f, 0.55f, 1f);

        Vector2 helmetCenter = new Vector2(8f, 10.2f);
        float helmetOuter = 6.4f;
        float helmetInner = 5.6f;

        Vector2 bodyCenter = new Vector2(8f, 5.8f);
        float bodyRx = 4.2f;
        float bodyRy = 3.7f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = transparent;

                // Helmet (circle + border).
                float dx = x - helmetCenter.x;
                float dy = y - helmetCenter.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= helmetOuter)
                {
                    c = dist >= helmetInner ? helmetBorder : helmetFill;
                }

                // Visor (rectangle inside helmet).
                bool inVisor = (x >= 5 && x <= 10 && y >= 8 && y <= 11);
                if (inVisor && c.a > 0f)
                {
                    bool visorEdge = (x == 5 || x == 10 || y == 8 || y == 11);
                    c = visorEdge ? helmetBorder : visorFill;
                    if ((x == 7 || x == 8) && (y == 9 || y == 10))
                    {
                        c = visorHighlight;
                    }
                }

                // Suit body (oval).
                float bx = (x - bodyCenter.x) / bodyRx;
                float by = (y - bodyCenter.y) / bodyRy;
                float bodyEq = bx * bx + by * by;
                bool inBody = bodyEq <= 1f && y >= 2 && y <= 9;
                if (inBody && c.a <= 0f)
                {
                    c = bodyEq > 0.86f ? suitBorder : suitFill;
                }

                // Backpack thruster (left side).
                if (x >= 2 && x <= 4 && y >= 3 && y <= 7)
                {
                    bool edge = x == 2 || x == 4 || y == 3 || y == 7;
                    c = edge ? suitBorder : accent;
                }

                // Antenna.
                if (x == 8 && (y == 14 || y == 15))
                {
                    c = accent;
                }
                if (x == 8 && y == 13 && c.a <= 0f)
                {
                    c = suitBorder;
                }

                // Boots.
                if (y <= 1)
                {
                    if ((x >= 5 && x <= 7) || (x >= 9 && x <= 11))
                    {
                        c = suitBorder;
                        if (x == 6 || x == 10) c = accent;
                    }
                }

                // Subtle suit speckle pattern (only the inner part of the body).
                if (c.a > 0f && inBody && bodyEq <= 0.86f)
                {
                    float h = Hash01(x, y, seed + 99);
                    if (h < 0.07f) c = Color.Lerp(suitFill, Color.white, 0.25f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void WriteFloraEnemyPng(string fullPath, int seed)
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(0f, 0f, 0f, 0f);

        // Match the neon-green look (so your health pickup color stays consistent).
        Color leafFill = new Color(0.18f, 1f, 0.55f, 1f);
        Color leafBorder = new Color(0.05f, 0.35f, 0.2f, 1f);
        Color vein = new Color(0.05f, 0.7f, 0.35f, 1f);
        Color danger = new Color(1f, 0.32f, 0.25f, 1f);
        Color mouthInner = new Color(0.95f, 0.18f, 0.20f, 1f);
        Color mouthHi = new Color(1f, 0.7f, 0.55f, 1f);

        int cx = 8;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = transparent;

                // Legs/feet at the bottom: more than one leg.
                if (y <= 1)
                {
                    if (x == 5 || x == 6 || x == 9 || x == 10) c = leafBorder;
                    if (x == 6 || x == 9) c = leafFill;
                }

                // Main stem.
                if (y >= 2 && y <= 13 && x >= 7 && x <= 9)
                {
                    bool edge = x == 7 || x == 9;
                    c = edge ? leafBorder : leafFill;
                }

                // Crown body (bigger open-mouth head).
                if (y >= 7 && y <= 12)
                {
                    int half = Mathf.Clamp(2 + (y - 7), 2, 6); // wider as you go down
                    int left = cx - half;
                    int right = cx + half;
                    if (x >= left && x <= right)
                    {
                        bool edge = x == left || x == right || y == 7 || y == 12;
                        c = edge ? leafBorder : leafFill;

                        // Veins inside crown.
                        if (!edge)
                        {
                            int diag = x - y;
                            if (Mathf.Abs(diag - (cx - 7)) <= 1) c = Color.Lerp(c, vein, 0.6f);
                            float h = Hash01(x, y, seed + 55);
                            if (h < 0.05f) c = Color.Lerp(c, vein, 0.75f);
                        }
                    }
                }

                // Spikes at the top (more aggressive silhouette).
                if (y >= 13)
                {
                    // Base green rim.
                    if (x >= 4 && x <= 12)
                    {
                        if (x == 4 || x == 12 || y == 13) c = leafBorder;
                        else c = leafFill;
                    }

                    // Red danger spikes.
                    // y==14/15 produce the tall points.
                    if (y == 13 && x % 2 == 0 && x >= 4 && x <= 12) c = danger;
                    if (y == 14 && x >= 5 && x <= 11 && (x % 2 == 1)) c = danger;
                    if (y == 15 && (x == 6 || x == 8 || x == 10)) c = danger;
                }

                // Big open mouth (centered under the spike crown).
                if (y >= 9 && y <= 11 && x >= 6 && x <= 10)
                {
                    bool mouthEdge = (y == 9 || y == 11 || x == 6 || x == 10);
                    c = mouthEdge ? leafBorder : mouthInner;

                    // Teeth/lip highlights.
                    if (!mouthEdge)
                    {
                        if ((x == 7 || x == 9) && y == 10) c = mouthHi;
                        float h = Hash01(x, y, seed + 888);
                        if (h < 0.04f) c = Color.Lerp(c, mouthHi, 0.35f);
                    }
                }

                // Extra sap glow speckles.
                if (c.a > 0f && y >= 7 && y <= 12)
                {
                    float h = Hash01(x, y, seed + 777);
                    if (h < 0.03f) c = Color.Lerp(c, Color.white, 0.42f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void WriteFaunaEnemyPng(string fullPath, int seed)
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(0f, 0f, 0f, 0f);
        // Darker palette + sharper silhouette.
        Color bodyFill = new Color(0.72f, 0.22f, 0.08f, 1f);   // rusty crimson
        Color bodyBorder = new Color(0.20f, 0.04f, 0.02f, 1f); // near-black
        Color eyeGlow = new Color(0.10f, 1f, 0.55f, 1f);       // neon green
        Color eyeWhite = new Color(0.75f, 1f, 0.95f, 1f);
        Color pupil = new Color(0.02f, 0.20f, 0.18f, 1f);
        Color mouth = new Color(1f, 0.25f, 0.2f, 1f);
        Color accent = new Color(0.55f, 0.20f, 1f, 1f);       // alien purple

        Vector2 center = new Vector2(8f, 8f);
        float rx = 4.6f;
        float ry = 4.1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = transparent;

                // Torso (rounded).
                float bx = (x - center.x) / rx;
                float by = (y - center.y) / ry;
                float eq = bx * bx + by * by;
                bool inTorso = eq <= 1f && y >= 3 && y <= 14;
                if (inTorso)
                {
                    c = eq > 0.86f ? bodyBorder : bodyFill;
                }

                // Head spikes.
                if (y == 12 || y == 13)
                {
                    if (x == 6 || x == 7 || x == 9 || x == 10) c = accent;
                }
                if (y == 14 && (x == 6 || x == 8 || x == 10)) c = accent;

                // Back spine.
                if (x == 8 && y >= 8 && y <= 13)
                {
                    if (y % 2 == 0) c = accent;
                }

                // Antennae.
                if (y == 15 && (x == 6 || x == 9)) c = eyeGlow;
                if (y == 14 && (x == 7 || x == 8)) c = bodyBorder;

                // Eyes.
                if (y == 9 && (x == 6 || x == 10)) c = eyeGlow;
                if (y == 10 && (x == 6 || x == 10)) c = eyeWhite;
                if (y == 10 && (x == 6 || x == 10)) c = pupil;

                // Jaw + mouth.
                if (y == 7 && x >= 7 && x <= 9) c = mouth;
                if (y == 6 && (x == 7 || x == 9)) c = Color.Lerp(mouth, Color.white, 0.25f);
                if (y == 8 && (x == 7 || x == 9)) c = Color.Lerp(mouth, Color.white, 0.15f);

                // Legs.
                if ((y == 3 || y == 4) && (x == 6 || x == 8 || x == 10)) c = bodyBorder;
                if (y == 4 && (x == 5 || x == 11)) c = bodyBorder;

                // Purple freckles over inner torso.
                if (c.a > 0f && inTorso && eq <= 0.86f && c == bodyFill)
                {
                    float h = Hash01(x, y, seed + 913);
                    if (h < 0.06f) c = Color.Lerp(c, accent, 0.65f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void WriteAlienShooterEnemyPng(string fullPath, int seed)
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(0f, 0f, 0f, 0f);
        Color bodyFill = new Color(0.15f, 0.65f, 1f, 1f);
        Color bodyBorder = new Color(0.06f, 0.18f, 0.45f, 1f);
        Color mask = new Color(0.02f, 0.15f, 0.3f, 1f);
        Color glow = new Color(0.85f, 0.35f, 1f, 1f);
        Color glow2 = new Color(0.25f, 0.95f, 1f, 1f);
        Color tentacle = new Color(0.18f, 0.42f, 0.85f, 1f);

        Vector2 center = new Vector2(8f, 8.2f);
        float bodyRx = 5.0f;
        float bodyRy = 4.6f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = transparent;

                // Base body oval (alien).
                float bx = (x - center.x) / bodyRx;
                float by = (y - center.y) / bodyRy;
                float eq = bx * bx + by * by;
                bool inBody = eq <= 1f && y >= 3 && y <= 13;
                if (inBody)
                {
                    c = eq > 0.86f ? bodyBorder : bodyFill;
                }

                // Slanted top horns.
                if (y >= 13 && (x == 6 || x == 9))
                {
                    c = bodyBorder;
                    if (y == 15) c = glow2;
                }
                if (y == 14 && (x == 5 || x == 10)) c = glow2;

                // Tentacles left/right.
                if (y >= 7 && y <= 13)
                {
                    if (x == 3 || x == 13)
                    {
                        c = tentacle;
                        if (y % 2 == 0) c = Color.Lerp(tentacle, glow2, 0.45f);
                    }
                    if (x == 4 && y >= 9 && y <= 12) c = tentacle;
                    if (x == 12 && y >= 9 && y <= 12) c = tentacle;
                }

                // Face mask (visor rectangle).
                if (y >= 8 && y <= 10 && x >= 5 && x <= 11)
                {
                    bool edge = (x == 5 || x == 11 || y == 8 || y == 10);
                    c = edge ? bodyBorder : mask;
                    if (!edge && (x == 7 || x == 9) && y == 9)
                    {
                        c = glow2;
                    }
                }

                // Glowing mouth.
                float mdx = x - 8f;
                float mdy = y - 7.2f;
                float md = Mathf.Sqrt(mdx * mdx + mdy * mdy);
                if (md <= 2.2f && y <= 9)
                {
                    bool mouthCore = md <= 1.3f;
                    c = mouthCore ? glow2 : glow;
                }

                // Speckle (alien texture) on body/tentacles.
                if (c.a > 0f && (inBody || x == 3 || x == 4 || x == 12 || x == 13))
                {
                    float h = Hash01(x, y, seed + 909);
                    if (h < 0.04f) c = Color.Lerp(c, glow, 0.6f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static string ToFullProjectPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string normalized = assetPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(projectRoot, normalized);
    }

    private static Sprite CreateFallbackSprite(string name, Color fill, Color border)
    {
        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.name = "fallback_" + name;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool isBorder = x <= 1 || x >= 14 || y <= 1 || y >= 14;
                tex.SetPixel(x, y, isBorder ? border : fill);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
    }

    private static GameObject FindInScene(string objectName)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == objectName)
            {
                return roots[i];
            }
        }

        return null;
    }

    private static Camera EnsurePlayerCamera()
    {
        GameObject camGO = FindInScene("PlayerCam");
        Camera cam = camGO != null ? camGO.GetComponent<Camera>() : null;

        if (camGO == null)
        {
            camGO = new GameObject("PlayerCam");
            Undo.RegisterCreatedObjectUndo(camGO, "Create PlayerCam");
        }

        if (cam == null)
        {
            cam = camGO.GetComponent<Camera>();
            if (cam == null)
            {
                cam = camGO.AddComponent<Camera>();
            }
        }

        if (camGO.tag != "MainCamera")
        {
            camGO.tag = "MainCamera";
        }

        camGO.transform.position = new Vector3(0f, 0f, -10f);
        camGO.transform.rotation = Quaternion.identity;
        return cam;
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject go = new GameObject(childName);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    private static void ClearChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child != null)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }

    private static void EnsureLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) != -1)
        {
            return;
        }

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        for (int i = 8; i < 32; i++)
        {
            SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                layerSP.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
    }

    private static void BuildGroundIfMissing(Transform geometryRoot, Sprite groundSprite)
    {
        if (geometryRoot.Find("GroundRow") != null)
        {
            return;
        }

        GameObject root = new GameObject("GroundRow");
        root.transform.SetParent(geometryRoot);
        root.transform.localPosition = Vector3.zero;

        // Wider map for exploration (player starts near x = 0).
        for (int x = -140; x <= 140; x++)
        {
            CreateBlock(root.transform, groundSprite, new Vector3(x, -3f, 0f), true);
            CreateBlock(root.transform, groundSprite, new Vector3(x, -4f, 0f), true);
        }
    }

    private static void BuildPlatformsIfMissing(Transform geometryRoot, Sprite groundSprite)
    {
        if (geometryRoot.Find("Platforms") != null)
        {
            return;
        }

        GameObject root = new GameObject("Platforms");
        root.transform.SetParent(geometryRoot);
        root.transform.localPosition = Vector3.zero;

        // Vertical exploration platforms.
        // Ground top is around y = -2.5 (we build ground blocks at -3 and -4).
        // Using these tiers gives ~2 block gaps between ground/platform tiers.
        // We also expand upwards by adding two more tiers.
        float tier1Y = 0f;
        float tier2Y = 3f;
        float tier3Y = 6f;
        float tier4Y = 9f;
        float tier5Y = 12f;
        float tier6Y = 15f;

        // Helper: split a long strip into smaller adjacent pieces.
        // Randomize segment count (3-5) so strips don't look uniform.
        void CreateSplitStrip(float y, float startX, int length)
        {
            if (length <= 0)
            {
                return;
            }

            // Randomly decide how many segments this strip becomes.
            int segCount = UnityEngine.Random.Range(3, 6); // 3..5 (inclusive)
            segCount = Mathf.Clamp(segCount, 3, length);

            // Occasionally shorten the whole strip a bit so segments are smaller overall.
            int adjustedLength = length;
            if (length >= 12 && UnityEngine.Random.value < 0.55f)
            {
                float scale = UnityEngine.Random.Range(0.75f, 0.92f);
                adjustedLength = Mathf.Clamp(Mathf.RoundToInt(length * scale), segCount, length);
            }

            // Split into N parts (each part at least 1 block) by choosing random cut points.
            // Cuts are in [1..adjustedLength-1], sorted, and define segment sizes.
            int cutCount = segCount - 1;
            System.Collections.Generic.HashSet<int> cutSet = new System.Collections.Generic.HashSet<int>();
            while (cutSet.Count < cutCount)
            {
                int cut = UnityEngine.Random.Range(1, adjustedLength); // max exclusive
                cutSet.Add(cut);
            }

            int[] cuts = new int[cutCount];
            cutSet.CopyTo(cuts);
            System.Array.Sort(cuts);

            int currentX = Mathf.RoundToInt(startX);
            int prevCut = 0;
            for (int i = 0; i < segCount; i++)
            {
                int nextCut = (i < cutCount) ? cuts[i] : adjustedLength;
                int segLen = nextCut - prevCut;
                if (segLen > 0)
                {
                    CreatePlatformStrip(root.transform, groundSprite, new Vector3(currentX, y, 0f), segLen);
                    currentX += segLen;
                }
                prevCut = nextCut;
            }
        }

        // Tier 1 (0) - split each original long strip into smaller segments.
        CreateSplitStrip(tier1Y, -95f, 45);  // -95..-51
        CreateSplitStrip(tier1Y, -45f, 40);  // -45..-6
        CreateSplitStrip(tier1Y, -5f, 35);   // -5..29
        CreateSplitStrip(tier1Y, 35f, 35);   // 35..69
        CreateSplitStrip(tier1Y, 75f, 25);   // 75..99

        // Tier 2 (3)
        CreateSplitStrip(tier2Y, -110f, 35);
        CreateSplitStrip(tier2Y, -60f, 35);
        CreateSplitStrip(tier2Y, -10f, 30);
        CreateSplitStrip(tier2Y, 35f, 25);
        CreateSplitStrip(tier2Y, 70f, 40);

        // Tier 3 (6)
        CreateSplitStrip(tier3Y, -120f, 40);
        CreateSplitStrip(tier3Y, -65f, 35);
        CreateSplitStrip(tier3Y, -20f, 30);
        CreateSplitStrip(tier3Y, 25f, 30);
        CreateSplitStrip(tier3Y, 70f, 35);

        // Tier 4 (9)
        CreateSplitStrip(tier4Y, -105f, 45);
        CreateSplitStrip(tier4Y, -50f, 35);
        CreateSplitStrip(tier4Y, 0f, 35);
        CreateSplitStrip(tier4Y, 45f, 25);
        CreateSplitStrip(tier4Y, 80f, 25);

        // Tier 5 (12) - fewer, smaller exploratory platforms.
        CreateSplitStrip(tier5Y, -120f, 40);
        CreateSplitStrip(tier5Y, -70f, 35);
        CreateSplitStrip(tier5Y, -20f, 35);
        CreateSplitStrip(tier5Y, 30f, 35);

        // Tier 6 (15)
        CreateSplitStrip(tier6Y, -110f, 35);
        CreateSplitStrip(tier6Y, -55f, 30);
        CreateSplitStrip(tier6Y, 0f, 30);
        CreateSplitStrip(tier6Y, 50f, 30);
    }

    private static void CreatePlatformStrip(Transform root, Sprite sprite, Vector3 start, int length)
    {
        for (int i = 0; i < length; i++)
        {
            CreateBlock(root, sprite, start + new Vector3(i, 0f, 0f), true);
        }
    }

    private static void BuildSpawnPointsIfMissing(Transform spawnPointsRoot)
    {
        if (spawnPointsRoot.childCount > 0)
        {
            return;
        }
        // Enemies and resources should spawn ABOVE platform colliders,
        // so the ore trigger isn't embedded "in the floor".
        float tier1Y = 0f;
        float tier2Y = 3f;
        float tier3Y = 6f;
        float tier4Y = 9f;
        float tier5Y = 12f;
        float tier6Y = 15f;

        float enemySpawnYOffset = 1.1f;
        float resourceSpawnYOffset = 1.0f;

        // Platforms are made of strips; we want it to be dense but NOT uniform.
        // For each strip we always spawn at least 1 enemy + 1 ore, and sometimes spawn extra ones.

        float edgePadding = 2f; // keep spawns away from strip edges

        // Per-strip spawn count ranges (keeps density, adds variation)
        int enemyMinPerStrip = 1;
        int enemyMaxPerStrip = 2;
        int oreMinPerStrip = 1;
        int oreMaxPerStrip = 1;

        int enemyIndex = 0;
        int oreIndex = 0;

        void SpawnStripSpawns(
            string tierTag,
            float yEnemy,
            float yOre,
            float stripXMin,
            float stripXMax,
            bool crystalForThisStrip)
        {
            // Extremely rare ore: should appear far less often than Crystal.
            float uraniumChance = 0f;
            if (tierTag.StartsWith("T2")) uraniumChance = 0.005f;
            else if (tierTag.StartsWith("T3")) uraniumChance = 0.015f;
            else if (tierTag.StartsWith("T4")) uraniumChance = 0.03f;
            else if (tierTag.StartsWith("T5")) uraniumChance = 0.06f;
            else if (tierTag.StartsWith("T6")) uraniumChance = 0.10f;

            bool uraniumPlacedThisStrip = false;

            float clampedMin = stripXMin + edgePadding;
            float clampedMax = stripXMax - edgePadding;
            if (clampedMax < clampedMin)
            {
                clampedMin = stripXMin;
                clampedMax = stripXMax;
            }

            int enemyCount = Mathf.Clamp(Mathf.RoundToInt(UnityEngine.Random.Range(enemyMinPerStrip, enemyMaxPerStrip + 1)), enemyMinPerStrip, enemyMaxPerStrip);
            int oreCount = Mathf.Clamp(Mathf.RoundToInt(UnityEngine.Random.Range(oreMinPerStrip, oreMaxPerStrip + 1)), oreMinPerStrip, oreMaxPerStrip);

            for (int e = 0; e < enemyCount; e++)
            {
                float x = UnityEngine.Random.Range(clampedMin, clampedMax);
                CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_" + tierTag + "_" + (enemyIndex++).ToString(), new Vector3(x, yEnemy, 0f), SpawnPoint2D.SpawnType.Enemy);
            }

            for (int o = 0; o < oreCount; o++)
            {
                float x = UnityEngine.Random.Range(clampedMin, clampedMax);
                string resourceName;
                if (!uraniumPlacedThisStrip && UnityEngine.Random.value <= uraniumChance)
                {
                    resourceName = "ResourceSpawn_Uranium_";
                    uraniumPlacedThisStrip = true;
                }
                else
                {
                    resourceName = crystalForThisStrip ? "ResourceSpawn_Crystal_" : "ResourceSpawn_Iron_";
                }
                CreateSpawnPoint(
                    spawnPointsRoot,
                    resourceName + tierTag + "_" + (oreIndex++).ToString(),
                    new Vector3(x, yOre, 0f),
                    SpawnPoint2D.SpawnType.Resource
                );
            }
        }

        void SpawnSplitStripSpawns(string tierPrefix, int baseIndex, float yEnemy, float yOre, float stripStartX, int stripLength, bool crystalForStrip)
        {
            int lenA = Mathf.Max(1, stripLength / 2);
            int lenB = Mathf.Max(1, stripLength - lenA);

            float seg1Min = stripStartX;
            float seg1Max = stripStartX + lenA - 1;
            float seg2Min = stripStartX + lenA;
            float seg2Max = stripStartX + stripLength - 1;

            SpawnStripSpawns(tierPrefix + "_" + baseIndex + "a", yEnemy, yOre, seg1Min, seg1Max, crystalForStrip);
            SpawnStripSpawns(tierPrefix + "_" + baseIndex + "b", yEnemy, yOre, seg2Min, seg2Max, crystalForStrip);
        }

        // Tier 1 (0)
        float t1YEnemy = tier1Y + enemySpawnYOffset;
        float t1YOre = tier1Y + resourceSpawnYOffset;
        SpawnSplitStripSpawns("T1", 1, t1YEnemy, t1YOre, -95f, 45, false);
        SpawnSplitStripSpawns("T1", 2, t1YEnemy, t1YOre, -45f, 40, false);
        SpawnSplitStripSpawns("T1", 3, t1YEnemy, t1YOre, -5f, 35, false);
        SpawnSplitStripSpawns("T1", 4, t1YEnemy, t1YOre, 35f, 35, false);
        SpawnSplitStripSpawns("T1", 5, t1YEnemy, t1YOre, 75f, 25, false);

        // Tier 2 (3)
        float t2YEnemy = tier2Y + enemySpawnYOffset;
        float t2YOre = tier2Y + resourceSpawnYOffset;
        SpawnSplitStripSpawns("T2", 1, t2YEnemy, t2YOre, -110f, 35, false);
        SpawnSplitStripSpawns("T2", 2, t2YEnemy, t2YOre, -60f, 35, true);   // crystal
        SpawnSplitStripSpawns("T2", 3, t2YEnemy, t2YOre, -10f, 30, true);
        SpawnSplitStripSpawns("T2", 4, t2YEnemy, t2YOre, 35f, 25, true);    // crystal
        SpawnSplitStripSpawns("T2", 5, t2YEnemy, t2YOre, 70f, 40, false);

        // Tier 3 (6)
        float t3YEnemy = tier3Y + enemySpawnYOffset;
        float t3YOre = tier3Y + resourceSpawnYOffset;
        SpawnSplitStripSpawns("T3", 1, t3YEnemy, t3YOre, -120f, 40, false);
        SpawnSplitStripSpawns("T3", 2, t3YEnemy, t3YOre, -65f, 35, true);   // crystal
        SpawnSplitStripSpawns("T3", 3, t3YEnemy, t3YOre, -20f, 30, false);
        SpawnSplitStripSpawns("T3", 4, t3YEnemy, t3YOre, 25f, 30, true);
        SpawnSplitStripSpawns("T3", 5, t3YEnemy, t3YOre, 70f, 35, true);    // crystal

        // Tier 4 (9)
        float t4YEnemy = tier4Y + enemySpawnYOffset;
        float t4YOre = tier4Y + resourceSpawnYOffset;
        SpawnSplitStripSpawns("T4", 1, t4YEnemy, t4YOre, -105f, 45, true);  // crystal
        SpawnSplitStripSpawns("T4", 2, t4YEnemy, t4YOre, -50f, 35, true);
        SpawnSplitStripSpawns("T4", 3, t4YEnemy, t4YOre, 0f, 35, true);     // crystal
        SpawnSplitStripSpawns("T4", 4, t4YEnemy, t4YOre, 45f, 25, false);
        SpawnSplitStripSpawns("T4", 5, t4YEnemy, t4YOre, 80f, 25, false);

        // Tier 5 (12) - more exploration up
        float t5YEnemy = tier5Y + enemySpawnYOffset;
        float t5YOre = tier5Y + resourceSpawnYOffset;
        SpawnSplitStripSpawns("T5", 1, t5YEnemy, t5YOre, -120f, 40, false);
        SpawnSplitStripSpawns("T5", 2, t5YEnemy, t5YOre, -70f, 35, true);   // crystal
        SpawnSplitStripSpawns("T5", 3, t5YEnemy, t5YOre, -20f, 35, true);
        SpawnSplitStripSpawns("T5", 4, t5YEnemy, t5YOre, 30f, 35, true);    // crystal

        // Tier 6 (15)
        float t6YEnemy = tier6Y + enemySpawnYOffset;
        float t6YOre = tier6Y + resourceSpawnYOffset;
        SpawnSplitStripSpawns("T6", 1, t6YEnemy, t6YOre, -110f, 35, false);
        SpawnSplitStripSpawns("T6", 2, t6YEnemy, t6YOre, -55f, 30, true);   // crystal
        SpawnSplitStripSpawns("T6", 3, t6YEnemy, t6YOre, 0f, 30, true);
        SpawnSplitStripSpawns("T6", 4, t6YEnemy, t6YOre, 50f, 30, true);    // crystal
    }

    private static void PositionPlayerAtSpawn()
    {
        GameObject player = FindInScene("Player");
        if (player == null)
        {
            return;
        }

        // Spawn away from the tier-1 enemy spawns to avoid immediate contact damage.
        player.transform.position = new Vector3(0f, -2.2f, 0f);
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private static void CreateBlock(Transform parent, Sprite sprite, Vector3 worldPosition, bool isGround)
    {
        GameObject block = new GameObject("Block");
        block.transform.SetParent(parent);
        block.transform.position = worldPosition;

        SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;
        ApplyVisibleSpriteMaterial(sr);

        BoxCollider2D bc = block.AddComponent<BoxCollider2D>();
        bc.size = Vector2.one;
        if (isGround)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1)
            {
                block.layer = groundLayer;
            }
        }
    }

    private static void CreateSpawnPoint(Transform parent, string name, Vector3 worldPos, SpawnPoint2D.SpawnType type)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = worldPos;

        SpawnPoint2D sp = go.AddComponent<SpawnPoint2D>();
        SerializedObject spSO = new SerializedObject(sp);
        spSO.FindProperty("spawnType").enumValueIndex = (int)type;
        spSO.FindProperty("spawnId").stringValue = name.ToLowerInvariant();
        spSO.ApplyModifiedPropertiesWithoutUndo();
    }

    private static UpgradeDefinition CreateUpgradeDefinitionAsset(
        string assetPath,
        string upgradeId,
        string displayName,
        string description,
        UpgradeEffectType effectType,
        int effectPerLevel,
        int maxLevel,
        UpgradeCostTemplate[] costs)
    {
        UpgradeDefinition def = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(assetPath);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<UpgradeDefinition>();
            AssetDatabase.CreateAsset(def, assetPath);
        }

        def.upgradeId = upgradeId;
        def.displayName = displayName;
        def.description = description;
        def.effectType = effectType;
        def.effectAmountPerLevel = effectPerLevel;
        def.maxLevel = maxLevel;

        def.resourceCosts = new List<UpgradeDefinition.ResourceCost>();
        for (int i = 0; i < costs.Length; i++)
        {
            def.resourceCosts.Add(new UpgradeDefinition.ResourceCost
            {
                resourceId = costs[i].resourceId,
                amount = costs[i].amount
            });
        }

        EditorUtility.SetDirty(def);
        return def;
    }

    private static void ConfigureCameraFor2D(Camera cam)
    {
        if (cam == null)
        {
            return;
        }

        Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < allCameras.Length; i++)
        {
            Camera other = allCameras[i];
            if (other != null && other != cam)
            {
                other.enabled = false;
            }
        }

        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;
        cam.cullingMask = ~0;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.18f, 0.2f, 0.27f, 1f);
        cam.enabled = true;

        Vector3 p = cam.transform.position;
        p.z = -10f;
        cam.transform.position = p;
        cam.transform.rotation = Quaternion.identity;
    }

    private static void ApplyVisibleSpriteMaterial(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }
        renderer.sharedMaterial = GetCompatibleSpriteMaterial();
        renderer.color = Color.white;
    }

    private static Material GetCompatibleSpriteMaterial()
    {
        EnsureDir("Assets/Art");
        EnsureDir(ArtDir);
        EnsureDir(MaterialDir);

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
        if (existing != null && existing.shader != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            return null;
        }

        Material mat = existing != null ? existing : new Material(shader);
        mat.shader = shader;
        mat.name = "generated_sprite_material";
        EditorUtility.SetDirty(mat);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(mat, SpriteMaterialPath);
        }

        return mat;
    }

    private static void CleanupBootstrapDuplicates()
    {
        KeepSingleRootObject("Player");
        KeepSingleRootObject("HUDCanvas");
        KeepSingleRootObject("GameSystems");
        KeepSingleRootObject("PlayerCam");
        KeepSingleRootObject("PlayerSpawn");
    }

    private static void KeepSingleRootObject(string objectName)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        GameObject keeper = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name != objectName)
            {
                continue;
            }

            if (keeper == null)
            {
                keeper = roots[i];
            }
            else
            {
                Undo.DestroyObjectImmediate(roots[i]);
            }
        }
    }

    private static void EnsureEventSystem()
    {
        EventSystem existing = Object.FindAnyObjectByType<EventSystem>();
        if (existing != null)
        {
            return;
        }

        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
    }

    private static Font GetSafeBuiltinFont()
    {
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            // ignored; try fallback below
        }

        if (font == null)
        {
            try
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch
            {
                // ignored
            }
        }

        return font;
    }

    private struct UpgradeCostTemplate
    {
        public readonly string resourceId;
        public readonly int amount;

        public UpgradeCostTemplate(string resourceId, int amount)
        {
            this.resourceId = resourceId;
            this.amount = amount;
        }
    }
}
