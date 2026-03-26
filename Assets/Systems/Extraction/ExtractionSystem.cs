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

    public bool IsExtractionInProgress => extractionRequested;

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
        }
    }

    private void LateUpdate()
    {
        // Be robust across respawns/scene reloads: PlayerStats reference can change.
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.OnDied -= HandlePlayerDied;
                playerStats.OnDied += HandlePlayerDied;
            }
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
            TriggerExtraction();
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

    public void TriggerExtraction()
    {
        if (extractionRequested)
        {
            return;
        }

        extractionRequested = true;
        extractionCompleteTime = Time.time + Mathf.Max(0.1f, extractionDelaySeconds);

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayExtractStart(transform.position);
        }

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

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayExtractSuccess(transform.position);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowUpgradeMenuAfterExtraction();
        }
    }

    private void HandlePlayerDied()
    {
        CancelExtraction(showFailMessage: true);
    }

    public void CancelExtraction(bool showFailMessage)
    {
        if (!extractionRequested)
        {
            return;
        }

        extractionRequested = false;

        if (HUDController.Instance != null)
        {
            HUDController.Instance.SetExtractionStatus(showFailMessage ? "Extraction failed. Resources lost." : string.Empty);
        }

        if (showFailMessage && GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayExtractFail(transform.position);
        }
    }
}
