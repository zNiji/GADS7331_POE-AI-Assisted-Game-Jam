using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDebugDisplay : MonoBehaviour
{
    [SerializeField] private Text debugText;
    [SerializeField] private bool sortAlphabetically = true;
    [SerializeField] private bool showWhenEmpty = true;

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
            debugText.text = showWhenEmpty ? "Inventory Debug:\n(empty)" : string.Empty;
            return;
        }

        List<string> keys = new List<string>(snapshot.Keys);
        if (sortAlphabetically)
        {
            keys.Sort();
        }

        StringBuilder output = new StringBuilder();
        output.AppendLine("Inventory Debug:");
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            output.Append("- ");
            output.Append(key);
            output.Append(": ");
            output.Append(snapshot[key]);
            output.AppendLine();
        }

        debugText.text = output.ToString().TrimEnd();
    }
}
