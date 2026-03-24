using UnityEngine;

public class PlayerUpgradeEffects : MonoBehaviour
{
    [SerializeField] private int bonusDamage;
    [SerializeField] private int bonusMiningPower;

    public int BonusDamage => bonusDamage;
    public int BonusMiningPower => bonusMiningPower;

    public void AddDamageBonus(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        bonusDamage += amount;
    }

    public void AddMiningPowerBonus(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        bonusMiningPower += amount;
    }
}
