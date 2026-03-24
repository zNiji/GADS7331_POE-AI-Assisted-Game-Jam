using System.Collections.Generic;
using UnityEngine;

public class ExtractionSystem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode callExtractionKey = KeyCode.X;

    [Header("Timing")]
    [SerializeField] private float extractionDelaySeconds = 5f;

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

    private bool extractionRequested;
    private float extractionCompleteTime;

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
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
        if (!extractionRequested && Input.GetKeyDown(callExtractionKey))
        {
            RequestExtraction();
            return;
        }

        if (!extractionRequested)
        {
            return;
        }

        float remaining = extractionCompleteTime - Time.time;
        if (remaining > 0f)
        {
            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetExtractionStatus("Extraction arriving in " + Mathf.CeilToInt(remaining) + "s");
            }

            return;
        }

        CompleteExtraction();
    }

    private void RequestExtraction()
    {
        extractionRequested = true;
        extractionCompleteTime = Time.time + Mathf.Max(0.1f, extractionDelaySeconds);

        if (HUDController.Instance != null)
        {
            HUDController.Instance.SetExtractionStatus("Extraction called...");
        }
    }

    private void CompleteExtraction()
    {
        extractionRequested = false;

        if (InventorySystem.Instance != null && ExtractedResourceBank.Instance != null)
        {
            Dictionary<string, int> snapshot = InventorySystem.Instance.GetSnapshot();
            ExtractedResourceBank.Instance.AddResources(snapshot);
        }

        if (HUDController.Instance != null)
        {
            HUDController.Instance.SetExtractionStatus("Extraction successful. Resources secured.");
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRun();
        }
    }

    private void HandlePlayerDied()
    {
        extractionRequested = false;
        if (HUDController.Instance != null)
        {
            HUDController.Instance.SetExtractionStatus("Extraction failed. Resources lost.");
        }
    }
}
