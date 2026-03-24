using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Run Reset")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform playerSpawnPoint;

    public bool IsPaused { get; private set; }
    private Vector3 cachedPlayerSpawnPosition;
    private IRunResettable[] runResettables;

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

        if (playerTransform != null)
        {
            cachedPlayerSpawnPosition = playerSpawnPoint != null
                ? playerSpawnPoint.position
                : playerTransform.position;
        }
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
    }

    private void HandlePlayerDied()
    {
        ResetRun();
    }

    public void ResetRun()
    {
        SetPause(false);

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
                playerRb.velocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
            }
        }

        if (playerStats != null)
        {
            playerStats.ResetForNewRun();
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
