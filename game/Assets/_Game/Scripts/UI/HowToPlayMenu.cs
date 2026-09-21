using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The main menu's HOW TO PLAY panel. Built in code, like the shop, so the only
// thing placed in the scene is the button that opens it.
//
// The text lives here as plain strings: when a control or rule changes, this is
// the one place to update it.
public class HowToPlayMenu : MonoBehaviour
{
    [SerializeField] RectTransform canvasRoot;
    [SerializeField] Button openButton;

    [SerializeField] Color panelColor = new Color(0.08f, 0.09f, 0.12f, 0.97f);
    [SerializeField] Color closeColor = new Color(0.35f, 0.35f, 0.40f);

    const string Key = "<color=#ffd24a>";
    const string End = "</color>";
    const string Heading = "<size=120%><color=#80c8ff>";
    const string HeadingEnd = "</color></size>";

    static readonly string Controls =
        Heading + "CONTROLS" + HeadingEnd + "\n" +
        Key + "WASD" + End + "   move\n" +
        Key + "Shift" + End + "   sprint (uses stamina)\n" +
        Key + "Q / R" + End + "   class skills\n" +
        Key + "1 - 0" + End + "   use the item in that slot\n" +
        Key + "Hold E" + End + "   pick berries from a bush\n" +
        Key + "Mouse wheel" + End + "   zoom in and out\n" +
        Key + "Esc" + End + "   pause\n\n" +
        "Your hero attacks the nearest enemy\nby themselves. You only move.";

    static readonly string Rules =
        Heading + "SURVIVE" + HeadingEnd + "\n" +
        "Enemies never stop coming, and there are\n" +
        "more of them, and tougher, the longer you last.\n" +
        "Pick up the gems they drop to gain levels.\n" +
        "When you fall, the run is over.\n\n" +

        Heading + "CAMPS" + HeadingEnd + "\n" +
        "Defeat a camp's guards, then stand in its\n" +
        "circle to capture it. Enemies in the circle\n" +
        "slow the capture. A captured camp spills coins\n" +
        "and gems. Bigger camps, bigger rewards.\n\n" +

        Heading + "BETWEEN RUNS" + HeadingEnd + "\n" +
        "Coins, items and XP are kept when a run ends,\n" +
        "even if you quit early from the pause screen.\n" +
        "XP raises your " + Key + "class level" + End + ": each run starts there.\n" +
        "Spend coins in the " + Key + "SHOP" + End + " on berries, relics\n" +
        "(get back up once) and armor (more max HP).";

    GameObject panel;

    void Start()
    {
        Build();
        panel.SetActive(false);
        openButton.onClick.AddListener(Open);
    }

    void Open()
    {
        panel.transform.SetAsLastSibling(); // drawn last = drawn on top
        panel.SetActive(true);
    }

    void Update()
    {
        if (panel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            panel.SetActive(false);
    }

    void Build()
    {
        // Same dark veil as the shop: blocks clicks on the menu underneath.
        panel = UiFactory.Box("HowToPlay", canvasRoot, Vector2.zero, new Vector2(4000f, 4000f),
                              new Color(0f, 0f, 0f, 0.7f), blocksClicks: true).gameObject;
        Transform box = UiFactory.Box("Panel", panel.transform, Vector2.zero, new Vector2(1500f, 840f), panelColor).transform;

        UiFactory.Text("Title", box, new Vector2(0f, 365f), new Vector2(1300f, 70f), 56f,
                       TextAlignmentOptions.Center, "HOW TO PLAY");

        // Two columns: the controls you need right away on the left, the rules
        // on the right.
        UiFactory.Text("Controls", box, new Vector2(-400f, 10f), new Vector2(560f, 620f), 30f,
                       TextAlignmentOptions.TopLeft, Controls);
        UiFactory.Text("Rules", box, new Vector2(300f, 10f), new Vector2(780f, 620f), 27f,
                       TextAlignmentOptions.TopLeft, Rules);

        Button close = UiFactory.Button("Close", box, new Vector2(0f, -370f), new Vector2(280f, 70f),
                                        closeColor, "Close  [Esc]", out _);
        close.onClick.AddListener(() => panel.SetActive(false));
    }
}
