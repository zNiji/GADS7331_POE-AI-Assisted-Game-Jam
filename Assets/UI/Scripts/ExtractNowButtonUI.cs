using UnityEngine;
using UnityEngine.UI;

public class ExtractNowButtonUI : MonoBehaviour
{
    [Header("Optional (auto-find if missing)")]
    [SerializeField] private ExtractionSystem extractionSystem;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (extractionSystem == null)
        {
            extractionSystem = FindAnyObjectByType<ExtractionSystem>();
        }
    }

    private void OnEnable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnExtractClicked);
        }
    }

    private void OnExtractClicked()
    {
        if (extractionSystem == null)
        {
            extractionSystem = FindAnyObjectByType<ExtractionSystem>();
        }

        if (extractionSystem == null)
        {
            return;
        }

        extractionSystem.TriggerExtraction();
    }
}

