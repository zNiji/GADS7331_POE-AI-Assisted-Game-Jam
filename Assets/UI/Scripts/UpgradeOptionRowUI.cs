using System;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeOptionRowUI : MonoBehaviour
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text costText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Text purchaseButtonText;

    public void Bind(
        UpgradeDefinition definition,
        int level,
        bool canAfford,
        bool isMaxLevel,
        string formattedCosts,
        Action onPurchase)
    {
        if (titleText != null)
        {
            titleText.text = definition != null ? definition.displayName : "Unknown Upgrade";
        }

        if (descriptionText != null)
        {
            descriptionText.text = definition != null ? definition.description : string.Empty;
        }

        if (levelText != null && definition != null)
        {
            levelText.text = "Lv " + level + "/" + definition.maxLevel;
        }

        if (costText != null)
        {
            costText.text = formattedCosts;
        }

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            if (onPurchase != null)
            {
                purchaseButton.onClick.AddListener(() => onPurchase.Invoke());
            }

            purchaseButton.interactable = !isMaxLevel && canAfford;
        }

        if (purchaseButtonText != null)
        {
            purchaseButtonText.text = isMaxLevel ? "Maxed" : "Buy";
        }
    }
}
