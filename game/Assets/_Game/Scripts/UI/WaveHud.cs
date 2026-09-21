using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The survival clock at the top of the screen, and the result panel when the
// run ends. (Kept its old name so the scene's reference to it still works; it
// used to show wave numbers.)
public class WaveHud : MonoBehaviour
{
    [SerializeField] EnemySpawner spawner;
    [SerializeField] RunBank bank;

    [SerializeField] TMP_Text infoText;

    [Header("Result panel")]
    [Tooltip("How dark the game behind the panel gets. Without this the text fights with the mob for attention.")]
    [SerializeField] Color veilColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] Color panelColor = new Color(0.08f, 0.09f, 0.12f, 0.97f);
    [SerializeField] Color stripColor = new Color(1f, 1f, 1f, 0.06f);
    [SerializeField] Color titleColor = new Color(1f, 0.31f, 0.31f);
    [SerializeField] Color coinColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] Color labelColor = new Color(0.63f, 0.65f, 0.7f);
    [SerializeField] Color retryColor = new Color(0.20f, 0.55f, 0.30f);
    [SerializeField] Color menuColor = new Color(0.35f, 0.35f, 0.40f);
    [Tooltip("Seconds for the panel to fade in.")]
    [SerializeField] float fadeSeconds = 0.4f;

    // What the label said last frame. Rebuilding the text every frame makes a
    // new string 60 times a second for something that changes about once a
    // second, and every one of those becomes garbage. Assigning to a TMP_Text
    // also makes it re-lay-out the characters, so skipping it saves both.
    string shown;

    // Built once, the moment the run ends. Nothing on it changes afterwards.
    CanvasGroup result;

    void Update()
    {
        if (GameStats.IsGameOver && result == null)
            BuildResult();

        // The game is frozen (timeScale 0) once the run ends, so deltaTime is 0.
        // unscaledDeltaTime keeps counting real seconds, which is what a menu
        // animation needs.
        if (result != null && result.alpha < 1f)
            result.alpha = Mathf.MoveTowards(result.alpha, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));

        string next = Compose();
        if (next == shown)
            return;

        shown = next;
        infoText.text = next;
    }

    string Compose()
    {
        // The panel reports the time; a second clock above it is just noise.
        if (GameStats.IsGameOver)
            return "";

        return $"<size=150%>{FormatTime(Seconds())}</size>";
    }

    float Seconds() => spawner != null ? spawner.Elapsed : Time.timeSinceLevelLoad;

    // ---- Result panel ------------------------------------------------------------

    void BuildResult()
    {
        // The top-most canvas the clock lives on: no extra scene wiring needed.
        Transform canvasRoot = infoText.canvas.rootCanvas.transform;

        // A dark veil over the whole screen, then the panel on top of it.
        GameObject veil = UiFactory.Box("RunResult", canvasRoot, Vector2.zero, new Vector2(4000f, 4000f), veilColor, blocksClicks: true).gameObject;
        veil.transform.SetAsLastSibling(); // drawn last = drawn on top

        // A CanvasGroup's alpha fades everything under it at once.
        result = veil.AddComponent<CanvasGroup>();
        result.alpha = 0f;

        Transform box = UiFactory.Box("Panel", veil.transform, Vector2.zero, new Vector2(900f, 580f), panelColor).transform;

        TextMeshProUGUI title = UiFactory.Text("Title", box, new Vector2(0f, 210f), new Vector2(860f, 110f), 96f,
                                               TextAlignmentOptions.Center, "YOU DIED");
        title.color = titleColor;
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 8f;
        UiFactory.Box("Rule", box, new Vector2(0f, 148f), new Vector2(420f, 4f), titleColor);

        // Three columns: icon, big number, small label.
        BuildStat(box, -280f, HudIcons.Clock, FormatTime(Seconds()), "SURVIVED", Color.white);
        BuildStat(box, 0f, HudIcons.Skull, GameStats.Kills.ToString(), "KILLS", Color.white);

        bool banked = bank != null && bank.Banked;
        string coins = banked ? $"+{bank.CoinsBanked}" : "0";
        BuildStat(box, 280f, HudIcons.Coin, coins, $"COINS  (TOTAL {PlayerProfile.Coins})", coinColor);

        // What the hero gained. Missing if no hero was ever chosen.
        if (banked && bank.Hero != null)
        {
            Transform strip = UiFactory.Box("ClassStrip", box, new Vector2(0f, -95f), new Vector2(780f, 64f), stripColor).transform;

            bool leveledUp = bank.ClassLevelAfter > bank.ClassLevelBefore;
            string level = leveledUp
                ? $"<color=#80ff80>{bank.Hero.displayName}  LV {bank.ClassLevelBefore} -> {bank.ClassLevelAfter}   LEVEL UP!</color>"
                : $"{bank.Hero.displayName}  LV {bank.ClassLevelAfter}";

            UiFactory.Text("Class", strip, new Vector2(-100f, 0f), new Vector2(540f, 64f), 30f,
                           TextAlignmentOptions.Left, level).fontStyle = FontStyles.Bold;
            UiFactory.Text("Xp", strip, new Vector2(240f, 0f), new Vector2(260f, 64f), 30f,
                           TextAlignmentOptions.Right, $"<color=#80c8ff>+{bank.ClassXpGained} class XP</color>");
        }

        // Buttons still work while the game is frozen: UI clicks don't depend
        // on timeScale. The Game scene already has the EventSystem they need.
        Button retry = UiFactory.Button("TryAgain", box, new Vector2(-170f, -205f), new Vector2(300f, 76f),
                                        retryColor, "TRY AGAIN", out _);
        retry.onClick.AddListener(Retry);

        Button menu = UiFactory.Button("MainMenu", box, new Vector2(170f, -205f), new Vector2(300f, 76f),
                                       menuColor, "MAIN MENU", out _);
        menu.onClick.AddListener(ToMainMenu);
    }

    // Reloading the scene we are in starts a fresh run with the same hero.
    // RunBank has already saved everything by now.
    void Retry() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    void ToMainMenu() => SceneManager.LoadScene("MainMenu");

    void BuildStat(Transform box, float x, Sprite icon, string value, string label, Color valueColor)
    {
        Image image = UiFactory.Box("Icon", box, new Vector2(x, 90f), new Vector2(52f, 52f), Color.white);
        image.sprite = icon;

        TextMeshProUGUI number = UiFactory.Text("Value", box, new Vector2(x, 25f), new Vector2(270f, 64f), 54f,
                                                TextAlignmentOptions.Center, value);
        number.color = valueColor;
        number.fontStyle = FontStyles.Bold;

        UiFactory.Text("Label", box, new Vector2(x, -25f), new Vector2(270f, 30f), 20f,
                       TextAlignmentOptions.Center, label).color = labelColor;
    }

    // 125.3 seconds -> "2:05". Counting up, so round down.
    static string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(Mathf.Max(0f, seconds));
        return $"{s / 60}:{s % 60:00}";
    }
}
