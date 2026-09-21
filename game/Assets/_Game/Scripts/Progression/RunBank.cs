using UnityEngine;

// The bridge between one run and the next.
//
// When the hero is chosen: start the run at that class's level, and apply the
// items carried in from the stash (armor's extra max HP).
// When the run ends -- however it ends -- bank the run's coins, put the
// inventory back in the stash, pour all the stage XP into the class XP, and
// write it to the save file. "However it ends" means: defeated, quitting to the
// menu from the pause screen, closing the game window, or pressing Stop in the
// editor. Leaving early keeps everything, exactly like dying does.
public class RunBank : MonoBehaviour
{
    [SerializeField] PlayerHealth health;
    [SerializeField] PlayerWallet wallet;
    [SerializeField] Inventory inventory;
    [SerializeField] PlayerExperience experience;
    [SerializeField] LevelUpRewards levelRewards;
    [SerializeField] PlayerClassSelector classSelector;
    [SerializeField] LevelUpPopup messages;

    // What the end-of-run screen reports.
    public bool Banked { get; private set; }
    public int CoinsBanked { get; private set; }
    public int ClassXpGained { get; private set; }
    public int ClassLevelBefore { get; private set; }
    public int ClassLevelAfter { get; private set; }
    public PlayerClassData Hero { get; private set; }

    void OnEnable()
    {
        classSelector.ClassChosen += OnClassChosen;
        health.Defeated += OnDefeated;
        health.Revived += OnRevived;
    }

    void OnDisable()
    {
        classSelector.ClassChosen -= OnClassChosen;
        health.Defeated -= OnDefeated;
        health.Revived -= OnRevived;
    }

    void OnClassChosen(PlayerClassData hero)
    {
        Hero = hero;

        // Start at the class level, with the stats of every level skipped.
        int level = PlayerProfile.ClassLevel(hero);
        experience.StartAtLevel(level);
        levelRewards.GrantLevels(level - 1);

        // Armor (and anything else with a max HP bonus) works just by being carried.
        float bonus = 0f;
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            ItemBag.Slot slot = inventory.GetSlot(i);
            if (!slot.IsEmpty)
                bonus += slot.Item.maxHealthBonus * slot.Count;
        }
        if (bonus > 0f)
            health.AddMaxHealth(bonus);

        health.HealToFull();

        if (level > 1 || bonus > 0f)
            messages.ShowMessage($"<color=#ffd24a>{hero.displayName.ToUpper()}  LV {level}</color>",
                                 bonus > 0f ? $"Armor: +{bonus:0} max HP" : "Starting at your class level");
    }

    void OnRevived(ItemData relic)
    {
        int left = inventory.CountOf(relic);
        messages.ShowMessage($"<color=#ffd24a>{relic.displayName.ToUpper()} SHATTERED</color>",
                             left > 0 ? $"You rose again!  {left} left" : "You rose again!  That was the last one");
    }

    void OnDefeated() => Bank();

    // Closing the game (or pressing Stop in the editor) calls this first...
    void OnApplicationQuit() => Bank();

    // ...and leaving the scene any other way -- quitting to the menu, restarting
    // -- destroys this object, which calls this. The other scripts may already
    // be torn down by now, but everything read below is plain C# data, which
    // stays readable after its object is destroyed.
    void OnDestroy() => Bank();

    public void Bank()
    {
        if (Banked)
            return; // once per run, however many of the above fire

        Banked = true;
        CoinsBanked = wallet.Coins;
        PlayerProfile.Coins += wallet.Coins;

        // Whatever is still in the bag goes home: berries not eaten, armor,
        // relics not used up.
        PlayerProfile.Stash.CopyFrom(inventory.Bag);

        // No hero chosen yet means no XP was collected; there is nothing to add.
        if (Hero != null)
        {
            ClassLevelBefore = PlayerProfile.ClassLevel(Hero);
            ClassXpGained = experience.TotalXpThisRun;
            PlayerProfile.AddClassXp(Hero, ClassXpGained);
            ClassLevelAfter = PlayerProfile.ClassLevel(Hero);
        }

        PlayerProfile.Save();
    }
}
