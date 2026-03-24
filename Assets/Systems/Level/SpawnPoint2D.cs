using UnityEngine;

public class SpawnPoint2D : MonoBehaviour
{
    public enum SpawnType
    {
        Enemy,
        Resource
    }

    [SerializeField] private SpawnType spawnType = SpawnType.Enemy;
    [SerializeField] private string spawnId = "default";

    public SpawnType Type => spawnType;
    public string SpawnId => spawnId;

    private void OnDrawGizmos()
    {
        Gizmos.color = spawnType == SpawnType.Enemy ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
