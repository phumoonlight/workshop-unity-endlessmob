using UnityEngine;

public enum WeaponKind
{
    Sword,
    Bow,
}

// Which character model is shown for a class. The weapon says how the class
// fights; the skin only says what you see. Two classes can share a weapon
// and still look different.
public enum HeroSkin
{
    Sword, // the Haons greatsword hero (ModelSword)
    Bow,   // the Haons twin-dagger hero (ModelBow)
    Aris,  // the study model from TestBAChar, on Mixamo clips (ModelAris)
}

// One playable hero class. Create more with:
// right-click in the Project window > Create > Endless Mob > Player Class
[CreateAssetMenu(fileName = "PlayerClass", menuName = "Endless Mob/Player Class")]
public class PlayerClassData : ScriptableObject
{
    [Tooltip("Permanent number the save file uses for this class. Rename the class freely, but never change this once players have saves, and never reuse a number. 0 = not set.")]
    public int id;

    public string displayName = "Hero";

    [TextArea(3, 6)]
    public string description = "What makes this class special.";

    public WeaponKind weapon;

    [Tooltip("Which character model to show. Independent of the weapon.")]
    public HeroSkin skin;

    public float maxHealth = 100f;

    [Tooltip("Extra move speed in percent. 10 = 10% faster than normal.")]
    public float moveSpeedBonusPercent = 0f;

    public Color bodyColor = new Color(0.2f, 0.55f, 1f);

    [Header("Class level (carries over between runs)")]
    [Tooltip("Class XP needed to go from class level 1 to 2. Much more than a stage level: stage XP from a whole run is poured in at once.")]
    public int classFirstLevelXp = 300;

    [Tooltip("Each class level needs this much more class XP than the one before.")]
    public int classExtraXpPerLevel = 200;

    public int ClassXpToNextLevel(int level) => classFirstLevelXp + (level - 1) * classExtraXpPerLevel;
}
