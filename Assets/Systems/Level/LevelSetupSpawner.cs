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
            
            GameObject spawned = Instantiate(prefab, point.transform.position, Quaternion.identity, parent);

            if (type == SpawnPoint2D.SpawnType.Resource)
            {
                ResourceNode node = spawned != null ? spawned.GetComponent<ResourceNode>() : null;
                if (node != null)
                {
                    node.SetResourceId(DetermineResourceId(point));
                }
            }
        }
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
