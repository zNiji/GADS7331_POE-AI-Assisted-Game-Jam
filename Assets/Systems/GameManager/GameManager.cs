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

        if (playerTransform != null)
        {
            cachedPlayerSpawnPosition = playerSpawnPoint != null
                ? playerSpawnPoint.position
                : playerTransform.position;
        }
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

            try { c.targetDisplay = desiredDisplay; } catch { /* ignore */ }

            c.orthographic = true;
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

        Camera camForFollow = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (camForFollow == null)
        {
            GameObject camGO = new GameObject("PlayerCam");
            try { camGO.tag = "MainCamera"; } catch { /* tag may not exist */ }
            camForFollow = camGO.AddComponent<Camera>();
            try { camForFollow.targetDisplay = desiredDisplay; } catch { /* ignore */ }

            camForFollow.enabled = true;
            camForFollow.orthographic = true;
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
        // If GameView is on Display 1, cameras targeting Display 0 won't render.
        // Prefer Display 1 when it is active; otherwise fall back to Display 0.
        if (Display.displays != null && Display.displays.Length > 1 && Display.displays[1] != null && Display.displays[1].active)
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

        if (runResettables == null || runResettables.Length == 0)
        {
            CacheRunResettables();
        }

        for (int i = 0; i < runResettables.Length; i++)
        {
            if (runResettables[i] == null)
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
