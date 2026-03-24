using System.Collections;
using UnityEngine;

public class CameraShake2D : MonoBehaviour
{
    public static CameraShake2D Instance { get; private set; }

    [SerializeField] private Transform shakeTarget;
    [SerializeField] private float defaultDuration = 0.08f;
    [SerializeField] private float defaultMagnitude = 0.08f;

    private Vector3 originalLocalPosition;
    private Coroutine activeShake;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (shakeTarget == null)
        {
            shakeTarget = transform;
        }

        originalLocalPosition = shakeTarget.localPosition;
    }

    public void Shake(float duration = -1f, float magnitude = -1f)
    {
        if (shakeTarget == null)
        {
            shakeTarget = transform;
        }

        float finalDuration = duration > 0f ? duration : defaultDuration;
        float finalMagnitude = magnitude > 0f ? magnitude : defaultMagnitude;
        originalLocalPosition = shakeTarget.localPosition;

        if (activeShake != null)
        {
            StopCoroutine(activeShake);
        }

        activeShake = StartCoroutine(ShakeRoutine(finalDuration, finalMagnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector2 offset = Random.insideUnitCircle * magnitude;
            shakeTarget.localPosition = originalLocalPosition + new Vector3(offset.x, offset.y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeTarget.localPosition = originalLocalPosition;
        activeShake = null;
    }
}
