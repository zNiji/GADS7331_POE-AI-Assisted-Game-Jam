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

    private void Awake()
    {
        EnsureTexts();
    }
    
    private void OnEnable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
        }
    }

    private void EnsureTexts()
    {
        if (titleText == null)
        {
            titleText = FindOrCreateText("InventoryTitle", new Vector2(0f, 140f), 34);
            titleText.text = "INVENTORY";
        }

        if (runInventoryText == null)
        {
            runInventoryText = FindOrCreateText("RunInventoryText", new Vector2(0f, 60f), 26, TextAnchor.UpperCenter, new Vector2(720f, 140f));
        }

        if (bankedInventoryText == null)
        {
            bankedInventoryText = FindOrCreateText("BankedInventoryText", new Vector2(0f, -10f), 22, TextAnchor.UpperCenter, new Vector2(720f, 130f));
        }
    }

    private Text FindOrCreateText(string name, Vector2 localAnchoredPos, int fontSize,
        TextAnchor anchor = TextAnchor.UpperCenter, Vector2 sizeDelta = default)
    {
        Transform existing = transform.Find(name);
        Text text = existing != null ? existing.GetComponent<Text>() : null;
        if (text != null)
        {
            return text;
        }

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = localAnchoredPos;
        if (sizeDelta != default)
        {
            rt.sizeDelta = sizeDelta;
        }
        else
        {
            rt.sizeDelta = new Vector2(720f, 60f);
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
        text.verticalOverflow = VerticalWrapMode.Overflow;
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

