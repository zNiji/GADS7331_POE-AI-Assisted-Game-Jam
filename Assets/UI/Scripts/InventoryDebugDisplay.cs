using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDebugDisplay : MonoBehaviour
{
    [SerializeField] private Text debugText;
    [SerializeField] private bool sortAlphabetically = true;
    [SerializeField] private bool showWhenEmpty = true;

    private bool isSubscribedToInventory;

    private void OnEnable()
    {
        isSubscribedToInventory = false;
        TrySubscribeAndRefresh();
        if (!isSubscribedToInventory)
        {
            StartCoroutine(SubscribeRetry());
        }

        ApplyReadableInventoryStyle();
    }

    private void OnDisable()
    {
        if (isSubscribedToInventory && InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
        }
        isSubscribedToInventory = false;
    }

    private void TrySubscribeAndRefresh()
    {
        if (InventorySystem.Instance == null)
        {
            Refresh(); // show "no InventorySystem" while waiting
            return;
        }

        if (!isSubscribedToInventory)
        {
            InventorySystem.Instance.OnInventoryChanged += Refresh;
            isSubscribedToInventory = true;
        }

        Refresh();
    }

    private System.Collections.IEnumerator SubscribeRetry()
    {
        // InventorySystem can be created a frame or two after the debug UI enables.
        // Keep retrying briefly so the debug display doesn't get stuck.
        for (int i = 0; i < 6; i++)
        {
            yield return null;
            if (InventorySystem.Instance != null)
            {
                TrySubscribeAndRefresh();
                if (isSubscribedToInventory)
                {
                    yield break;
                }
            }
        }
    }

    public void Refresh()
    {
        if (debugText == null)
        {
            return;
        }

        if (InventorySystem.Instance == null)
        {
            debugText.text = "Inventory Debug: no InventorySystem";
            return;
        }

        Dictionary<string, int> snapshot = InventorySystem.Instance.GetSnapshot();
        if (snapshot.Count == 0)
        {
            // Don't show a debug header during gameplay.
            debugText.text = string.Empty;
            return;
        }

        List<string> keys = new List<string>(snapshot.Keys);
        if (sortAlphabetically)
        {
            keys.Sort();
        }

        StringBuilder output = new StringBuilder();
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            output.Append(key);
            output.Append(": ");
            output.Append(snapshot[key]);
            output.AppendLine();
        }

        debugText.text = output.ToString().TrimEnd();
    }

    private void ApplyReadableInventoryStyle()
    {
        if (debugText == null) return;

        debugText.fontStyle = FontStyle.Bold;
        debugText.color = Color.white;
        debugText.resizeTextForBestFit = false;

        Outline outline = debugText.GetComponent<Outline>();
        if (outline == null) outline = debugText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;

        // Avoid additional shadow blur; outline is enough.
        Shadow shadow = debugText.GetComponent<Shadow>();
        if (shadow != null) shadow.enabled = false;
    }
}
