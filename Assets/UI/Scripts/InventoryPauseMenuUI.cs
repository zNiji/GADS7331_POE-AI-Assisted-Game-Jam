using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPauseMenuUI : MonoBehaviour
{
    [Header("Optional (auto-find if null)")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text runInventoryText;
    [SerializeField] private Text bankedInventoryText;
    [Header("Layout")]
    [SerializeField] private Vector2 pausePanelSize = new Vector2(720f, 420f);

    private void Awake()
    {
        // Ensure the pause panel is large enough for the inventory text block.
        // (This avoids needing to regenerate the HUD canvas during iteration.)
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null && pausePanelSize != default)
        {
            rt.sizeDelta = pausePanelSize;
        }
        EnsureTexts();
    }
    
    private void OnEnable()
    {
        EnsureTexts();

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += Refresh;
        }

        // The PausePanel already has a centered "PAUSED" label; hiding it avoids overlapping with the inventory texts.
        Transform pauseLabel = transform.Find("PauseLabel");
        if (pauseLabel != null)
        {
            pauseLabel.gameObject.SetActive(false);
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
        }

        // Restore so if other pause UI expects it, it will be visible next time.
        Transform pauseLabel = transform.Find("PauseLabel");
        if (pauseLabel != null)
        {
            pauseLabel.gameObject.SetActive(true);
        }
    }

    private void EnsureTexts()
    {
        if (titleText == null)
        {
            titleText = FindOrCreateText("InventoryTitle", new Vector2(0f, 115f), 34);
            titleText.text = "INVENTORY";
        }
        else
        {
            // Re-apply layout every time (texts may have been created earlier with old positions).
            ApplyTextLayout(titleText, "InventoryTitle", new Vector2(0f, 115f), 34, TextAnchor.MiddleCenter, new Vector2(520f, 60f));
        }

        if (runInventoryText == null)
        {
            runInventoryText = FindOrCreateText(
                "RunInventoryText",
                new Vector2(0f, 25f),
                26,
                TextAnchor.UpperCenter,
                new Vector2(520f, 120f)
            );
        }
        else
        {
            ApplyTextLayout(runInventoryText, "RunInventoryText", new Vector2(0f, 25f), 26, TextAnchor.UpperCenter, new Vector2(520f, 120f));
        }

        if (bankedInventoryText == null)
        {
            bankedInventoryText = FindOrCreateText(
                "BankedInventoryText",
                new Vector2(0f, -85f),
                22,
                TextAnchor.UpperCenter,
                new Vector2(520f, 90f)
            );
        }
        else
        {
            ApplyTextLayout(bankedInventoryText, "BankedInventoryText", new Vector2(0f, -85f), 22, TextAnchor.UpperCenter, new Vector2(520f, 90f));
        }
    }

    private void ApplyTextLayout(Text text, string expectedName, Vector2 localAnchoredPos, int fontSize, TextAnchor anchor, Vector2 sizeDelta)
    {
        if (text == null) return;

        // Safety: if the existing name doesn't match, still proceed with layout.
        RectTransform rt = text.GetComponent<RectTransform>();
        if (rt == null) return;

        // Use center anchoring to match the rest of the PausePanel (e.g. "PAUSED" is centered).
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localAnchoredPos;

        if (sizeDelta != default)
        {
            rt.sizeDelta = sizeDelta;
        }

        Font font = GetSafeFont();
        if (font != null)
        {
            text.font = font;
        }

        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;
        text.resizeTextForBestFit = false; // prevent best-fit scaling from causing line overlap
        text.lineSpacing = 1.15f;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        // Truncate instead of overflowing vertically so long inventories never cover buttons.
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private Text FindOrCreateText(string name, Vector2 localAnchoredPos, int fontSize,
        TextAnchor anchor = TextAnchor.MiddleCenter, Vector2 sizeDelta = default)
    {
        Transform existing = transform.Find(name);
        Text text = existing != null ? existing.GetComponent<Text>() : null;
        if (text != null)
        {
            // Re-apply layout in case it was created earlier.
            ApplyTextLayout(text, name, localAnchoredPos, fontSize, anchor, sizeDelta == default ? new Vector2(520f, 60f) : sizeDelta);
            return text;
        }

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localAnchoredPos;
        if (sizeDelta != default)
        {
            rt.sizeDelta = sizeDelta;
        }
        else
        {
            rt.sizeDelta = new Vector2(520f, 60f);
        }

        text = go.GetComponent<Text>();
        Font font = GetSafeFont();
        if (font != null)
        {
            text.font = font;
        }

        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        // Truncate instead of overflowing vertically so long inventories never cover buttons.
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.text = string.Empty;
        return text;
    }

    private Font GetSafeFont()
    {
        // Use a built-in font that exists in older Unity versions.
        try
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            return null;
        }
    }

    private void Refresh()
    {
        if (runInventoryText == null || bankedInventoryText == null)
        {
            EnsureTexts();
        }

        // Run inventory = what player mined this run but hasn't extracted.
        Dictionary<string, int> runSnap = InventorySystem.Instance != null ? InventorySystem.Instance.GetSnapshot() : null;
        Dictionary<string, int> bankSnap = null;
        if (ExtractedResourceBank.Instance != null)
        {
            foreach (var kvp in ExtractedResourceBank.Instance.GetAllBankedResources())
            {
                if (bankSnap == null) bankSnap = new Dictionary<string, int>();
                bankSnap[kvp.Key] = kvp.Value;
            }
        }

        if (runSnap == null || runSnap.Count == 0)
        {
            if (runInventoryText != null) runInventoryText.text = "Mined this run:\n(none)";
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Mined this run:\n");
            foreach (KeyValuePair<string, int> kvp in runSnap)
            {
                sb.Append(kvp.Key);
                sb.Append(": ");
                sb.Append(kvp.Value);
                sb.Append("\n");
            }

            if (runInventoryText != null) runInventoryText.text = sb.ToString().TrimEnd();
        }

        if (bankSnap == null || bankSnap.Count == 0)
        {
            if (bankedInventoryText != null) bankedInventoryText.text = "Extracted (bank):\n(none)";
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Extracted (bank):\n");
            foreach (KeyValuePair<string, int> kvp in bankSnap)
            {
                sb.Append(kvp.Key);
                sb.Append(": ");
                sb.Append(kvp.Value);
                sb.Append("\n");
            }

            if (bankedInventoryText != null) bankedInventoryText.text = sb.ToString().TrimEnd();
        }
    }
}

