using UnityEngine;

// Every level up: attack and max HP go up, and HP is fully restored.
public class LevelUpRewards : MonoBehaviour
{
    [SerializeField] PlayerExperience experience;
    [SerializeField] PlayerHealth health;
    [SerializeField] PlayerClassSelector classSelector; // knows which weapon is in use
    [SerializeField] LevelUpPopup popup;

    [Tooltip("Attack gained each level, in percent of your starting attack.")]
    [SerializeField] float attackPercentPerLevel = 1f;

    [Tooltip("Max HP gained each level, in percent of your class's starting HP.")]
    [SerializeField] float healthPercentPerLevel = 1f;

    // Listen for level ups only while this component is active.
    void OnEnable() => experience.LeveledUp += OnLeveledUp;
    void OnDisable() => experience.LeveledUp -= OnLeveledUp;

    // The bonuses of several levels at once, with no popup. Used at the start of
    // a run, which begins at your class level: starting at LV 5 should feel like
    // having reached LV 5, so you get the 4 level-ups' worth of stats.
    public void GrantLevels(int count)
    {
        Weapon weapon = classSelector.ActiveWeapon;
        for (int i = 0; i < count; i++)
        {
            if (weapon != null)
                weapon.AddDamagePercent(attackPercentPerLevel);
            health.AddMaxHealthPercentOfBase(healthPercentPerLevel);
        }
    }

    void OnLeveledUp()
    {
        Weapon weapon = classSelector.ActiveWeapon;
        if (weapon != null)
            weapon.AddDamagePercent(attackPercentPerLevel);

        float hpGained = health.AddMaxHealthPercentOfBase(healthPercentPerLevel);
        health.HealToFull();

        if (popup != null)
            popup.Show(experience.Level, attackPercentPerLevel, hpGained);
    }
}
