using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private Slider healthSlider;

    [Header("Inventory")]
    [SerializeField] private Text inventoryText;

    private void OnEnable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += RefreshInventory;
        }

        RefreshInventory();
    }

    private void OnDisable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= RefreshInventory;
        }
    }

    public void SetHealth(float current, float max)
    {
        if (healthSlider == null || max <= 0f)
        {
            return;
        }

        healthSlider.value = Mathf.Clamp01(current / max);
    }

    public void RefreshInventory()
    {
        if (inventoryText == null)
        {
            return;
        }

        if (InventorySystem.Instance == null)
        {
            inventoryText.text = "Inventory: (none)";
            return;
        }

        var items = InventorySystem.Instance.GetItems();
        if (items.Count == 0)
        {
            inventoryText.text = "Inventory: (empty)";
            return;
        }

        string output = "Inventory:";
        foreach (var kvp in items)
        {
            output += "\n- " + kvp.Key + " x" + kvp.Value;
        }

        inventoryText.text = output;
    }
}
