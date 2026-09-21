using TMPro;
using UnityEngine;

// The survival clock at the top of the screen, and the result when the run ends.
// (Kept its old name so the scene's reference to it still works; it used to
// show wave numbers.)
public class WaveHud : MonoBehaviour
{
    [SerializeField] EnemySpawner spawner;
    [SerializeField] RunBank bank;

    [SerializeField] TMP_Text infoText;

    // What the label said last frame. Rebuilding the text every frame makes a
    // new string 60 times a second for something that changes about once a
    // second, and every one of those becomes garbage. Assigning to a TMP_Text
    // also makes it re-lay-out the characters, so skipping it saves both.
    string shown;

    void Update()
    {
        string next = Compose();
        if (next == shown)
            return;

        shown = next;
        infoText.text = next;
    }

    string Compose()
    {
        float seconds = spawner != null ? spawner.Elapsed : Time.timeSinceLevelLoad;

        if (GameStats.IsGameOver)
            return $"<size=200%><color=#ff5050>YOU DIED</color></size>\n" +
                   $"You survived {FormatTime(seconds)} and killed {GameStats.Kills} enemies\n" +
                   BankedLines() +
                   "Press R to try again with the same hero, or M for the main menu";

        return $"<size=150%>{FormatTime(seconds)}</size>";
    }

    // What carried over to the next run.
    string BankedLines()
    {
        if (bank == null || !bank.Banked)
            return "";

        string level = bank.ClassLevelAfter > bank.ClassLevelBefore
            ? $"<color=#80ff80>{bank.Hero.displayName} LV {bank.ClassLevelBefore} -> {bank.ClassLevelAfter}!</color>"
            : $"{bank.Hero.displayName} LV {bank.ClassLevelAfter}";

        return $"<color=#ffd24a>+{bank.CoinsBanked} coins banked  (total {PlayerProfile.Coins})</color>\n" +
               $"+{bank.ClassXpGained} class XP    {level}\n" +
               "Your inventory carries over\n";
    }

    // 125.3 seconds -> "2:05". Counting up, so round down.
    static string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(Mathf.Max(0f, seconds));
        return $"{s / 60}:{s % 60:00}";
    }
}
