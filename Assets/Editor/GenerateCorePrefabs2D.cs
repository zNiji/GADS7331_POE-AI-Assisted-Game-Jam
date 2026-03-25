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

        Sprite bulletSprite = CreatePixelSprite("spr_bullet", new Color(1f, 0.95f, 0.35f), new Color(0.9f, 0.65f, 0.15f));
        Sprite enemySprite = CreatePixelSprite("spr_enemy", new Color(0.8f, 0.25f, 0.25f), new Color(0.35f, 0.08f, 0.08f));
        Sprite nodeSprite = CreatePixelSprite("spr_resource_node", new Color(0.35f, 0.75f, 0.85f), new Color(0.15f, 0.35f, 0.45f));
        Sprite itemSprite = CreatePixelSprite("spr_resource_item", new Color(0.6f, 1f, 0.75f), new Color(0.2f, 0.45f, 0.25f));
        Sprite healthPickupSprite = CreatePixelSprite("spr_health_pickup", new Color(0.35f, 1f, 0.55f), new Color(0.12f, 0.35f, 0.22f));

        GameObject resourceItemPrefab = CreateResourceItemPrefab(itemSprite);
        GameObject bulletPrefab = CreateBulletPrefab(bulletSprite);
        GameObject enemyPrefab = CreateEnemyPrefab(enemySprite);
        GameObject resourceNodePrefab = CreateResourceNodePrefab(nodeSprite, resourceItemPrefab);
        GameObject healthPickupPrefab = CreateHealthPickupPrefab(healthPickupSprite);
        GameObject rowPrefab = CreateUpgradeRowPrefab();
        GameObject playerPrefab = CreatePlayerPrefab(CreatePixelSprite("spr_player", new Color(0.4f, 0.8f, 1f), new Color(0.12f, 0.22f, 0.35f)), bulletPrefab);
        GameObject hudPrefab = CreateHudCanvasPrefab(rowPrefab);
        GameObject gameSystemsPrefab = CreateGameSystemsPrefab(enemyPrefab, resourceNodePrefab, bulletPrefab);

        Selection.activeObject = gameSystemsPrefab != null ? gameSystemsPrefab : rowPrefab;
        Debug.Log("Core prefabs generated: Bullet, Enemy, ResourceNode, ResourceItem, HealthPickup, UpgradeOptionRowUI, Player, HUDCanvas, GameSystems.");
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

        Sprite groundSprite = CreatePixelSprite("spr_ground_block", new Color(0.56f, 0.56f, 0.64f), new Color(0.26f, 0.26f, 0.34f));
        BuildGroundIfMissing(geometryRoot, groundSprite);
        BuildPlatformsIfMissing(geometryRoot, groundSprite);
        BuildSpawnPointsIfMissing(spawnPointsRoot);
        SnapSpawnPointsToGround(spawnPointsRoot);
        SpawnHealthPickups(levelRoot.transform, spawnPointsRoot);
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

        // Less common: aim for only a couple of placements, but keep extra candidates
        // in case one happens to overlap an ore.
        int maxPickups = 2;
        int spawnedCount = 0;
        Vector3[] pickupSpawnPositions =
        {
            new Vector3(-45f, tier1Y + 2.2f, 0f),
            new Vector3(-40f, tier2Y + 2.2f, 0f),
            new Vector3(20f, tier3Y + 2.2f, 0f),
            new Vector3(30f, tier4Y + 2.2f, 0f),
        };

        for (int i = 0; i < pickupSpawnPositions.Length; i++)
        {
            if (spawnedCount >= maxPickups)
            {
                break;
            }

            Vector3 guess = pickupSpawnPositions[i];

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
            1,
            5,
            new[] { new UpgradeCostTemplate("Iron", 4), new UpgradeCostTemplate("FuelCell", 1) }
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

        GameObject systems = FindInScene("GameSystems");
        if (systems != null)
        {
            BaseUpgradeSystem baseSystem = systems.GetComponent<BaseUpgradeSystem>();
            if (baseSystem != null)
            {
                SerializedObject so = new SerializedObject(baseSystem);
                SerializedProperty upgradesProp = so.FindProperty("availableUpgrades");
                upgradesProp.arraySize = 0;
                List<UpgradeDefinition> defs = new List<UpgradeDefinition> { health, damage, mining };
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

    private static GameObject CreateResourceItemPrefab(Sprite sprite)
    {
        GameObject go = new GameObject("ResourceItem");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 4;
        ApplyVisibleSpriteMaterial(renderer);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        go.AddComponent<ResourceItem>();

        string path = PrefabDir + "/ResourceItem.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateResourceNodePrefab(Sprite sprite, GameObject resourceItemPrefab)
    {
        GameObject go = new GameObject("ResourceNode");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
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
        Text suitText = CreateText("SuitLabel", canvasGO.transform, new Vector2(20f, -32f), 16, TextAnchor.MiddleLeft);
        Text oxygenText = CreateText("OxygenLabel", canvasGO.transform, new Vector2(20f, -70f), 16, TextAnchor.MiddleLeft);
        Text resourcesText = CreateText("ResourcesText", canvasGO.transform, new Vector2(20f, -120f), 16, TextAnchor.UpperLeft);
        Text biomeText = CreateText("BiomeLabel", canvasGO.transform, new Vector2(20f, -250f), 16, TextAnchor.MiddleLeft);
        Text promptText = CreateCenteredText("PromptText", canvasGO.transform, new Vector2(0f, 120f), 20);
        Text extractionText = CreateCenteredText("ExtractionStatusText", canvasGO.transform, new Vector2(0f, 80f), 18);

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

        Text debugText = CreateText("InventoryDebugText", canvasGO.transform, new Vector2(-320f, -120f), 14, TextAnchor.UpperLeft, new Vector2(350f, 300f), true);

        GameObject pausePanel = CreatePanel("PausePanel", canvasGO.transform, new Vector2(0f, 0f), new Vector2(500f, 260f), new Color(0f, 0f, 0f, 0.7f));
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

    private static GameObject CreateGameSystemsPrefab(GameObject enemyPrefab, GameObject resourceNodePrefab, GameObject bulletPrefab)
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

        // Vertical exploration platforms with larger gaps between tiers.
        // Ground top is around y = -2.5 (we build ground blocks at -3 and -4).
        // Using these tiers gives ~2 block gaps between ground/platform tiers.
        float tier1Y = 0f;
        float tier2Y = 3f;
        float tier3Y = 6f;
        float tier4Y = 9f;

        // Tier 1 (0)
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(-95f, tier1Y, 0f), 45);   // -95..-51
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(-45f, tier1Y, 0f), 40);  // -45..-6
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(-5f, tier1Y, 0f), 35);   // -5..29
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(35f, tier1Y, 0f), 35);   // 35..69
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(75f, tier1Y, 0f), 25);   // 75..99

        // Tier 2 (3)
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(-110f, tier2Y, 0f), 35);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(-60f, tier2Y, 0f), 35);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(-10f, tier2Y, 0f), 30);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(35f, tier2Y, 0f), 25);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(70f, tier2Y, 0f), 40);

        // Tier 3 (6)
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(-120f, tier3Y, 0f), 40);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(-65f, tier3Y, 0f), 35);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(-20f, tier3Y, 0f), 30);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(25f, tier3Y, 0f), 30);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(70f, tier3Y, 0f), 35);

        // Tier 4 (9)
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(-105f, tier4Y, 0f), 45);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(-50f, tier4Y, 0f), 35);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(0f, tier4Y, 0f), 35);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(45f, tier4Y, 0f), 25);
        CreatePlatformStrip(root.transform, groundSprite, new Vector3(80f, tier4Y, 0f), 25);
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

        float enemySpawnYOffset = 1.1f;
        float resourceSpawnYOffset = 1.0f;

        // Platforms are made of "strips". To ensure it never feels sparse, we spawn at least:
        // - 1 enemy per platform strip
        // - 1 resource node per platform strip (Iron everywhere, with deterministic Crystal rarity on some strips)

        // Tier 1 strip centers (y = 0): [-95..-51], [-45..-6], [-5..29], [35..69], [75..99]
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T1_1", new Vector3(-73f, tier1Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T1_2", new Vector3(-26f, tier1Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T1_3", new Vector3(12f, tier1Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T1_4", new Vector3(52f, tier1Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T1_5", new Vector3(87f, tier1Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);

        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T1_1", new Vector3(-73f, tier1Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T1_2", new Vector3(-26f, tier1Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T1_3", new Vector3(12f, tier1Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T1_4", new Vector3(52f, tier1Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T1_5", new Vector3(87f, tier1Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);

        // Tier 2 strip centers (y = 3): [-110..-76], [-60..-26], [-10..19], [35..59], [70..109]
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T2_1", new Vector3(-93f, tier2Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T2_2", new Vector3(-43f, tier2Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T2_3", new Vector3(5f, tier2Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T2_4", new Vector3(47f, tier2Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T2_5", new Vector3(90f, tier2Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);

        // Crystal rarity: tier2 strips 2 and 4 are Crystal, others Iron.
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T2_1", new Vector3(-93f, tier2Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Crystal_T2_2", new Vector3(-43f, tier2Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T2_3", new Vector3(5f, tier2Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Crystal_T2_4", new Vector3(47f, tier2Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T2_5", new Vector3(90f, tier2Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);

        // Tier 3 strip centers (y = 6): [-120..-81], [-65..-31], [-20..9], [25..54], [70..104]
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T3_1", new Vector3(-101f, tier3Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T3_2", new Vector3(-48f, tier3Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T3_3", new Vector3(-5f, tier3Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T3_4", new Vector3(40f, tier3Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T3_5", new Vector3(87f, tier3Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);

        // Crystal rarity: tier3 strips 2 and 5 are Crystal, others Iron.
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T3_1", new Vector3(-101f, tier3Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Crystal_T3_2", new Vector3(-48f, tier3Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T3_3", new Vector3(-5f, tier3Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T3_4", new Vector3(40f, tier3Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Crystal_T3_5", new Vector3(87f, tier3Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);

        // Tier 4 strip centers (y = 9): [-105..-61], [-50..-16], [0..34], [45..69], [80..104]
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T4_1", new Vector3(-83f, tier4Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T4_2", new Vector3(-33f, tier4Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T4_3", new Vector3(17f, tier4Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T4_4", new Vector3(57f, tier4Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);
        CreateSpawnPoint(spawnPointsRoot, "EnemySpawn_T4_5", new Vector3(92f, tier4Y + enemySpawnYOffset, 0f), SpawnPoint2D.SpawnType.Enemy);

        // Crystal rarity: tier4 strips 1 and 3 are Crystal, others Iron.
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Crystal_T4_1", new Vector3(-83f, tier4Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T4_2", new Vector3(-33f, tier4Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Crystal_T4_3", new Vector3(17f, tier4Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T4_4", new Vector3(57f, tier4Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
        CreateSpawnPoint(spawnPointsRoot, "ResourceSpawn_Iron_T4_5", new Vector3(92f, tier4Y + resourceSpawnYOffset, 0f), SpawnPoint2D.SpawnType.Resource);
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
