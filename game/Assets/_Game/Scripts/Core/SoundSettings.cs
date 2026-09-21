using UnityEngine;

// The player's sound volumes, changed in the settings window and remembered
// between sessions.
//
// They are kept in PlayerPrefs, Unity's small built-in store for preferences
// (on Windows it lives in the registry). That is the right place for settings:
// they are about the computer, not about progress, so they stay out of the
// save file and survive a save reset. The editor and a built game keep
// separate PlayerPrefs, so testing never changes a real player's volumes.
//
// "static" = one copy for the whole game, reachable from anywhere, like
// PlayerProfile. GameAudio reads these every time it plays a sound, so a
// slider takes effect immediately.
public static class SoundSettings
{
    public const float DefaultMaster = 0.8f;
    public const float DefaultSlash = 0.25f;
    public const float DefaultShoot = 0.15f; // lowest: Rapid Fire plays it three times as often

    // All 0..1. A sound's loudness = its own volume x Master.
    public static float Master = DefaultMaster;
    public static float Slash = DefaultSlash;
    public static float Shoot = DefaultShoot;

    const string MasterKey = "sound.master";
    const string SlashKey = "sound.slash";
    const string ShootKey = "sound.shoot";

    // Runs before the first scene, on every Play press. Statics survive between
    // Play presses here (domain reload is off), so we always re-read.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load()
    {
        Master = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, DefaultMaster));
        Slash = Mathf.Clamp01(PlayerPrefs.GetFloat(SlashKey, DefaultSlash));
        Shoot = Mathf.Clamp01(PlayerPrefs.GetFloat(ShootKey, DefaultShoot));
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(MasterKey, Master);
        PlayerPrefs.SetFloat(SlashKey, Slash);
        PlayerPrefs.SetFloat(ShootKey, Shoot);
        PlayerPrefs.Save(); // write now, not only when the game quits cleanly
    }

    public static void ResetToDefaults()
    {
        Master = DefaultMaster;
        Slash = DefaultSlash;
        Shoot = DefaultShoot;
    }
}
