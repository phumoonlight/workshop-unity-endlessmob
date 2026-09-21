using System;
using UnityEngine;

// Tracks the STAGE level and stage XP: the levels you gain during one run.
// Cheap to level (5 XP, then 10, 15...) and gone when the run ends -- but all
// the XP collected is poured into the class's XP, which is what carries over.
// Other scripts listen to LeveledUp to react.
public class PlayerExperience : MonoBehaviour
{
    [Tooltip("XP needed to go from level 1 to level 2.")]
    [SerializeField] int firstLevelXp = 5;

    [Tooltip("Each level needs this much more XP than the one before.")]
    [SerializeField] int extraXpPerLevel = 5;

    [Tooltip("Gems closer than this (in meters) fly to the player.")]
    [SerializeField] float pickupRadius = 3f;

    public int Level { get; private set; } = 1;
    public int CurrentXp { get; private set; }

    // Every point of XP picked up this run, whatever it was spent on. RunBank
    // adds it to the class XP when the run ends.
    public int TotalXpThisRun { get; private set; }
    public int XpToNextLevel => firstLevelXp + (Level - 1) * extraXpPerLevel;
    public float PickupRadius => pickupRadius;

    // An "event": other scripts can subscribe with  experience.LeveledUp += MyMethod;
    // and MyMethod gets called every time we level up.
    public event Action LeveledUp;

    // Begin the run at the class level. Quiet on purpose: no LeveledUp events,
    // so no popups or sounds for levels you already earned in earlier runs.
    // LevelUpRewards.GrantLevels hands out their stat bonuses instead.
    public void StartAtLevel(int level)
    {
        Level = Mathf.Max(1, level);
        CurrentXp = 0;
    }

    public void AddXp(int amount)
    {
        TotalXpThisRun += amount;
        CurrentXp += amount;

        // "while" instead of "if": one big gem could give several levels at once.
        while (CurrentXp >= XpToNextLevel)
        {
            CurrentXp -= XpToNextLevel;
            Level++;
            GameAudio.Play(GameAudio.Sfx.LevelUp);
            LeveledUp?.Invoke();
        }
    }

    public void AddPickupRadius(float meters)
    {
        pickupRadius += meters;
    }
}
