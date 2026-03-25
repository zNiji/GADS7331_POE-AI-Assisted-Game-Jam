using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Run Reset")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private DeathUpgradeMenuUI deathUpgradeMenu;

    public bool IsPaused { get; private set; }
    private Vector3 cachedPlayerSpawnPosition;
    private IRunResettable[] runResettables;
    private bool waitingForDeathUpgradeSelection;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResolveReferences();
        CacheRunResettables();
        EnsureCameraRendering();

        // Reinitialize when gameplay scene is loaded (GameManager persists across scenes).
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;

        if (playerTransform != null)
        {
            cachedPlayerSpawnPosition = playerSpawnPoint != null
                ? playerSpawnPoint.position
                : playerTransform.position;
        }
    }

    private void Start()
    {
        // Some objects (and/or camera components) may enable/disable after Awake across scene loads.
        // Re-apply camera display settings after the first frame.
        Invoke(nameof(EnsureCameraRendering), 0f);
    }

    private void OnDestroy()
    {
        try
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
        catch { }
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Be robust: scene name filtering can break if the gameplay scene is renamed.
        // Instead, only proceed when we can see core gameplay objects.
        PlayerStats psProbe = FindAnyObjectByType<PlayerStats>();
        LevelSetupSpawner spawnerProbe = FindAnyObjectByType<LevelSetupSpawner>();
        if (psProbe == null && spawnerProbe == null)
        {
            return;
        }

        // Refresh references to the newly loaded scene.
        ResolveReferences();
        RefreshPlayerSpawnPosition();
        EnsurePlayerDeathSubscription();
        EnsureCameraRendering();

        // Start a fresh run state first (clears run inventory / resets run-state objects).
        ResetRun();

        // Apply pending save + reseed the level on the next frame.
        // This ensures UI elements have subscribed to events (InventoryDebugDisplay / pause inventory text)
        // and the level spawner is fully ready to instantiate enemies/resources.
        StartCoroutine(ApplySaveAndReseedLevelNextFrame());
    }

    private System.Collections.IEnumerator ApplySaveAndReseedLevelNextFrame()
    {
        yield return null; // wait one frame for UI/event subscriptions + scene objects to fully wake

        // If the player chose a save slot from the main menu, restore the run state now.
        GameSaveSystem.TryLoadPendingSlotIntoWorld();

        // Find the spawner after the scene has fully woken up.
        LevelSetupSpawner spawner = FindAnyObjectByType<LevelSetupSpawner>();
        if (spawner != null)
        {
            spawner.SpawnAll();
        }
        else
        {
            Debug.LogWarning("Save load reseed failed: LevelSetupSpawner not found after scene wake.");
        }

        // Re-cache and reset run-resettable objects created/activated by the spawner.
        CacheRunResettables();
        for (int i = 0; i < runResettables.Length; i++)
        {
            if (runResettables[i] == null) continue;

            UnityEngine.Object uo = runResettables[i] as UnityEngine.Object;
            if (uo == null) continue;

            runResettables[i].ResetForNewRun();
        }
    }

    private void RefreshPlayerSpawnPosition()
    {
        if (playerSpawnPoint != null)
        {
            cachedPlayerSpawnPosition = playerSpawnPoint.position;
            return;
        }

        GameObject spawnGo = GameObject.Find("PlayerSpawn");
        if (spawnGo != null)
        {
            cachedPlayerSpawnPosition = spawnGo.transform.position;
        }
    }

    private void EnsurePlayerDeathSubscription()
    {
        if (playerStats == null) return;

        playerStats.OnDied -= HandlePlayerDied;
        playerStats.OnDied += HandlePlayerDied;
    }

    private void EnsureCameraRendering()
    {
        int desiredDisplay = GetDesiredDisplayIndex();

        // Unity shows "No cameras rendering" when none of the cameras are enabled for the current display.
        // Re-enable every camera we can find and force it to render to the desired display.
        Camera[] all = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Camera c = all[i];
            if (c == null) continue;

            c.gameObject.SetActive(true);
            c.enabled = true;

            c.targetDisplay = desiredDisplay;

            c.orthographic = true;
            // Taller orthographic view so newly-expanded higher tiers are visible immediately.
            c.orthographicSize = 10f;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.18f, 0.2f, 0.27f, 1f);
            c.cullingMask = ~0;
            c.nearClipPlane = 0.01f;
            c.farClipPlane = 100f;

            Vector3 p = c.transform.position;
            p.z = -10f;
            c.transform.position = p;
            c.transform.rotation = Quaternion.identity;
        }

        // Safety: make sure at least one camera is enabled for the desired display.
        bool anyForDesiredDisplay = false;
        for (int i = 0; i < all.Length; i++)
        {
            Camera c = all[i];
            if (c == null) continue;
            if (!c.enabled) continue;
            if (!c.gameObject.activeInHierarchy) continue;
            if (c.targetDisplay == desiredDisplay)
            {
                anyForDesiredDisplay = true;
                break;
            }
        }

        Camera camForFollow = Camera.main != null ? Camera.main : null;
        if (camForFollow == null)
        {
            // Pick the first camera from the already-collected list (includes inactive)
            // to avoid missing disabled cameras via FindAnyObjectByType().
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                {
                    camForFollow = all[i];
                    break;
                }
            }
        }

        if (camForFollow == null)
        {
            GameObject camGO = new GameObject("PlayerCam");
            try { camGO.tag = "MainCamera"; } catch { /* tag may not exist */ }
            camForFollow = camGO.AddComponent<Camera>();
            camForFollow.targetDisplay = desiredDisplay;

            camForFollow.enabled = true;
            camForFollow.orthographic = true;
            camForFollow.orthographicSize = 10f;
            camForFollow.clearFlags = CameraClearFlags.SolidColor;
            camForFollow.backgroundColor = new Color(0.18f, 0.2f, 0.27f, 1f);
            camForFollow.cullingMask = ~0;
            camForFollow.nearClipPlane = 0.01f;
            camForFollow.farClipPlane = 100f;

            Vector3 p = camForFollow.transform.position;
            p.z = -10f;
            camForFollow.transform.position = p;
            camForFollow.transform.rotation = Quaternion.identity;
        }
        else if (!anyForDesiredDisplay)
        {
            // We found a follow camera but none were targeting the desired display.
            // Force the follow camera to the correct display.
            try { camForFollow.targetDisplay = desiredDisplay; } catch { /* ignore */ }
        }

        CameraFollow2D follow = camForFollow.GetComponent<CameraFollow2D>();
        if (follow == null)
        {
            follow = camForFollow.gameObject.AddComponent<CameraFollow2D>();
        }

        if (playerTransform != null)
        {
            follow.SetTarget(playerTransform);
        }
        else if (playerStats != null)
        {
            follow.SetTarget(playerStats.transform);
        }
    }

    private static int GetDesiredDisplayIndex()
    {
        // The "No cameras rendering" overlay in your screenshot is specifically for Display 1,
        // so prefer Display 1 whenever a second display exists.
        if (Display.displays != null && Display.displays.Length > 1 && Display.displays[1] != null)
        {
            return 1;
        }

        return 0;
    }

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnDied += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnDied -= HandlePlayerDied;
        }
    }

    private void Update()
    {
        if (waitingForDeathUpgradeSelection)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        SetPause(!IsPaused);
    }

    public void SetPause(bool pauseState)
    {
        IsPaused = pauseState;
        Time.timeScale = IsPaused ? 0f : 1f;

        if (HUDController.Instance != null)
        {
            HUDController.Instance.SetPauseVisible(pauseState);
        }
    }

    private void HandlePlayerDied()
    {
        if (deathUpgradeMenu == null)
        {
            ResolveReferences();
        }

        waitingForDeathUpgradeSelection = true;
        SetPause(true);

        if (deathUpgradeMenu != null)
        {
            deathUpgradeMenu.Show(this);
            return;
        }

        CompleteDeathUpgradeAndRespawn();
    }

    public void CompleteDeathUpgradeAndRespawn()
    {
        waitingForDeathUpgradeSelection = false;
        ResetRun();
    }

    public void ShowUpgradeMenuAfterExtraction()
    {
        waitingForDeathUpgradeSelection = true;
        SetPause(true);

        if (deathUpgradeMenu == null)
        {
            ResolveReferences();
        }

        if (deathUpgradeMenu != null)
        {
            deathUpgradeMenu.Show(this, "Extraction Successful", "Choose one permanent upgrade before redeploying.");
            return;
        }

        // Fallback: if menu is missing, just redeploy.
        CompleteDeathUpgradeAndRespawn();
    }

    public void ResetRun()
    {
        SetPause(false);
        if (deathUpgradeMenu != null)
        {
            deathUpgradeMenu.HideImmediate();
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ClearRunResources();
        }

        // Always refresh run resettable list.
        // Otherwise we can keep stale references to destroyed objects
        // (e.g. after pausing -> returning to main menu -> spawning a new level),
        // which causes MissingReferenceException during ResetForNewRun().
        CacheRunResettables();

        for (int i = 0; i < runResettables.Length; i++)
        {
            if (runResettables[i] == null)
            {
                continue;
            }

            // Interface refs can still be "missing" even when they won't compare to null.
            // If the underlying UnityEngine.Object was destroyed, casting to UnityEngine.Object will compare == null.
            UnityEngine.Object uo = runResettables[i] as UnityEngine.Object;
            if (uo == null)
            {
                continue;
            }

            runResettables[i].ResetForNewRun();
        }

        RespawnPlayer();
    }

    private void RespawnPlayer()
    {
        if (playerTransform != null)
        {
            Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : cachedPlayerSpawnPosition;
            playerTransform.position = spawnPos;

            Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
            }
        }

        if (playerStats != null)
        {
            playerStats.ResetForNewRun();
        }

        if (BaseUpgradeSystem.Instance != null)
        {
            BaseUpgradeSystem.Instance.ReapplyAllUpgradeEffects(true);
        }

        if (HUDController.Instance != null)
        {
            HUDController.Instance.SetExtractionStatus(string.Empty);
        }
    }

    private void ResolveReferences()
    {
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
        }

        if (playerTransform == null && playerStats != null)
        {
            playerTransform = playerStats.transform;
        }

        if (deathUpgradeMenu == null)
        {
            deathUpgradeMenu = FindAnyObjectByType<DeathUpgradeMenuUI>();
        }
    }

    private void CacheRunResettables()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        System.Collections.Generic.List<IRunResettable> result = new System.Collections.Generic.List<IRunResettable>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IRunResettable resettable)
            {
                result.Add(resettable);
            }
        }

        runResettables = result.ToArray();
    }
}
