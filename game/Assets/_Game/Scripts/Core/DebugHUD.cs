using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Debug readouts and run controls, drawn with Unity's old OnGUI text:
// FPS and live enemy count (bottom-right) and the Esc pause screen (M quits to
// the menu). The player-facing numbers live on RunHud; the buttons after a run
// ends are on WaveHud's result panel.
public class DebugHUD : MonoBehaviour
{
    [SerializeField] PlayerHealth player;

    GUIStyle style;
    GUIStyle centerStyle;

    // Paused with Esc. Separate from the other timeScale-0 pauses (hero select),
    // so Esc can't unfreeze a menu it didn't open.
    bool paused;
    GUIStyle rightStyle;

    // FPS is averaged over half a second. A single frame's number jumps around
    // too fast to read, and one slow frame would flash a misleading low value.
    const float FpsWindow = 0.5f;
    int framesCounted;
    float timeCounted;
    int fps;

    void Awake()
    {
        GameStats.Reset();
        Time.timeScale = 1f; // un-freeze in case we restarted after a game over
    }

    void Update()
    {
        // Unscaled: FPS is about the computer, not the game's clock, so it has
        // to keep counting through hit-stop and pauses.
        framesCounted++;
        timeCounted += Time.unscaledDeltaTime;
        if (timeCounted >= FpsWindow)
        {
            fps = Mathf.RoundToInt(framesCounted / timeCounted);
            framesCounted = 0;
            timeCounted = 0f;
        }

        // Esc pauses and unpauses -- only when the game is running normally, so it
        // can't fight the hero-select screen, which also stops time. While the
        // settings window is open, Esc belongs to it (it closes the window).
        if (!GameStats.IsGameOver && !SettingsMenu.IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (paused)
            {
                paused = false;
                Time.timeScale = 1f;
            }
            else if (Time.timeScale > 0f)
            {
                paused = true;
                Time.timeScale = 0f;
            }
        }

        // Leaving from the pause screen. RunBank banks everything as the scene
        // unloads, exactly as if the run had ended.
        if (paused && !SettingsMenu.IsOpen && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            SceneManager.LoadScene("MainMenu");

        // (After the run ends, WaveHud's result panel has the buttons to retry
        // or go back to the menu.)
    }

    // OnGUI is Unity's old, simple UI system. Fine for debugging, not for a real game.
    void OnGUI()
    {
        if (player == null)
            return;

        // OnGUI draws on top of every canvas, so it would cover the settings window.
        if (SettingsMenu.IsOpen)
            return;

        if (paused)
        {
            if (centerStyle == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
                style.normal.textColor = Color.white;
                centerStyle = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter };
                rightStyle = new GUIStyle(style) { alignment = TextAnchor.UpperRight };
            }
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
                      "<size=56>PAUSED</size>\n\n" +
                      "<color=#ffd24a>Esc</color>  resume\n" +
                      "<color=#ffd24a>M</color>  quit to menu\n\n" +
                      "<size=22><color=#a0a0a0>Leaving keeps this run's coins, items and XP</color></size>", centerStyle);
            return;
        }

        // Hide while a menu (hero select, level-up) has the game paused.
        // OnGUI always draws on top of everything, so it would cover the menu.
        if (Time.timeScale == 0f && !GameStats.IsGameOver)
            return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.white;
            centerStyle = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter };
            rightStyle = new GUIStyle(style) { alignment = TextAnchor.UpperRight };
        }

        // Bottom-right: how fast the game runs, and how many enemies it's running.
        // Not top-right, where the DEV panel (F1) sits. Green at 55+, yellow down
        // to 30, red below that.
        string fpsColor = fps >= 55 ? "#80ff80" : fps >= 30 ? "#ffd24a" : "#ff6060";
        GUI.Label(new Rect(Screen.width - 320, Screen.height - 124, 300, 40), $"<color={fpsColor}>FPS  {fps}</color>", rightStyle);
        GUI.Label(new Rect(Screen.width - 320, Screen.height - 84, 300, 40), $"Mobs  {Enemy.AliveCount}", rightStyle);

        // HP, time, kills and coins are on the real HUD now (RunHud), with icons.

        // (The game-over message is shown by WaveHud.)
        if (player.IsDead && !GameStats.IsGameOver)
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), $"You fell!  Respawning in {Mathf.CeilToInt(player.RespawnTimeLeft)}...", centerStyle);
    }
}
