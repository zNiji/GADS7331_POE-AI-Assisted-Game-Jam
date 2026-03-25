using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.12f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            PlayerStats player = FindAnyObjectByType<PlayerStats>();
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, Mathf.Max(0.01f, smoothTime));
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
