using UnityEngine;

public class LevelSetupSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject resourceNodePrefab;

    [Header("Behavior")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool clearSpawnedOnRespawn = true;

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

        SpawnPoint2D[] points = FindObjectsByType<SpawnPoint2D>(FindObjectsSortMode.None);
        for (int i = 0; i < points.Length; i++)
        {
            SpawnPoint2D point = points[i];
            if (point == null || point.Type != type)
            {
                continue;
            }

            Instantiate(prefab, point.transform.position, Quaternion.identity, parent);
        }
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
