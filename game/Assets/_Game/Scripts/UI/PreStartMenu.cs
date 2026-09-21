using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The screen between the main menu and a run: pick a hero (each card shows its
// class level and XP), see your coins, visit the shop, then START.
//
// The hero cards are built in code from the class assets, so a new class shows
// up here just by being added to the list. The START / SHOP / BACK buttons are
// placed in the scene (copies of the main menu's buttons, to match their look).
public class PreStartMenu : MonoBehaviour
{
    [SerializeField] RectTransform canvasRoot;

    [Tooltip("The heroes to choose from, left to right. Keys 1, 2, 3... pick them.")]
    [SerializeField] PlayerClassData[] classes;

    [SerializeField] Button startButton;
    [SerializeField] Button backButton;
    [SerializeField] ShopMenu shop;

    [SerializeField] string gameScene = "Game";
    [SerializeField] string menuScene = "MainMenu";

    [Header("Look")]
    [SerializeField] Color cardColor = new Color(0.13f, 0.15f, 0.20f, 1f);
    [SerializeField] Color selectedColor = new Color(0.16f, 0.33f, 0.22f, 1f);
    [SerializeField] Color barBackColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] Color barFillColor = new Color(1f, 0.82f, 0.25f, 1f);

    Image[] cards;
    TextMeshProUGUI coinsText;
    int selected;
    int shownCoins = -1;

    void Start()
    {
        Time.timeScale = 1f; // in case we arrived from a paused or finished run

        // Start on the hero you played last, or the first one.
        selected = Mathf.Max(0, System.Array.IndexOf(classes, RunSettings.ChosenClass));

        coinsText = UiFactory.Text("Coins", canvasRoot, new Vector2(700f, 470f), new Vector2(400f, 60f), 40f,
                                   TextAlignmentOptions.Right);

        BuildCards();
        startButton.onClick.AddListener(StartRun);
        backButton.onClick.AddListener(Back);
        Select(selected);
    }

    void Update()
    {
        // The shop is shown on top of this screen; buying changes the coins, so
        // keep the number current -- but only rebuild the text when it changes.
        if (PlayerProfile.Coins != shownCoins)
        {
            shownCoins = PlayerProfile.Coins;
            coinsText.text = $"<color=#ffd24a>Coins  {shownCoins}</color>";
        }

        // While the shop is open, the keyboard belongs to it (Esc closes it).
        if (shop != null && shop.IsOpen)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        for (int i = 0; i < classes.Length && i < 9; i++)
            if (keyboard[Key.Digit1 + i].wasPressedThisFrame || keyboard[Key.Numpad1 + i].wasPressedThisFrame)
                Select(i);

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            StartRun();
        else if (keyboard.escapeKey.wasPressedThisFrame)
            Back();
    }

    void Select(int index)
    {
        selected = index;
        for (int i = 0; i < cards.Length; i++)
            cards[i].color = i == selected ? selectedColor : cardColor;
    }

    void StartRun()
    {
        RunSettings.ChosenClass = classes[selected];
        SceneManager.LoadScene(gameScene);
    }

    void Back()
    {
        SceneManager.LoadScene(menuScene);
    }

    // ---- The hero cards --------------------------------------------------------

    void BuildCards()
    {
        const float width = 560f;
        const float gap = 60f;
        float firstX = -(width + gap) * (classes.Length - 1) / 2f;

        cards = new Image[classes.Length];
        for (int i = 0; i < classes.Length; i++)
            cards[i] = BuildCard(classes[i], i, new Vector2(firstX + (width + gap) * i, 40f), new Vector2(width, 500f));
    }

    Image BuildCard(PlayerClassData hero, int index, Vector2 position, Vector2 size)
    {
        // The whole card is one big button: click anywhere on it to pick that hero.
        Image card = UiFactory.Box(hero.displayName, canvasRoot, position, size, cardColor, blocksClicks: true);
        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card;
        button.onClick.AddListener(() => Select(index));
        // A card's colour shows whether it's picked, so don't let the button's own
        // hover/press tint fight with that.
        button.transition = Selectable.Transition.None;

        Transform t = card.transform;
        float top = size.y / 2f;
        UiFactory.Text("Key", t, new Vector2(-size.x / 2f + 40f, top - 36f), new Vector2(60f, 40f), 28f,
                       TextAlignmentOptions.Center, $"<color=#a0a0a0>[{index + 1}]</color>");
        UiFactory.Text("Name", t, new Vector2(0f, top - 60f), new Vector2(size.x - 40f, 64f), 52f,
                       TextAlignmentOptions.Center, hero.displayName.ToUpper());

        // The class's own description (written on the class asset) already says
        // its HP, speed and range, so it's shown as-is rather than repeating stats.
        UiFactory.Text("Description", t, new Vector2(0f, 40f), new Vector2(size.x - 60f, 220f), 26f,
                       TextAlignmentOptions.Top, hero.description);

        // Class level, with a bar for how far into the next level this hero is.
        int level = PlayerProfile.ClassLevel(hero, out int into, out int needed);
        UiFactory.Text("Level", t, new Vector2(0f, -top + 110f), new Vector2(size.x - 40f, 50f), 36f,
                       TextAlignmentOptions.Center, $"<color=#ffd24a>Class LV {level}</color>");
        BuildBar(t, new Vector2(0f, -top + 65f), new Vector2(size.x - 100f, 18f), needed > 0 ? (float)into / needed : 0f);
        UiFactory.Text("Xp", t, new Vector2(0f, -top + 32f), new Vector2(size.x - 40f, 30f), 22f,
                       TextAlignmentOptions.Center, $"<color=#a0a0a0>{into} / {needed} class XP</color>");
        return card;
    }

    // A bar: a dark background with a coloured fill stretched to "fraction" of it.
    // The fill's anchors do the work -- anchorMax.x = 0.4 means "reach 40% across".
    void BuildBar(Transform parent, Vector2 position, Vector2 size, float fraction)
    {
        Image back = UiFactory.Box("Bar", parent, position, size, barBackColor);
        Image fill = UiFactory.Box("Fill", back.transform, Vector2.zero, Vector2.zero, barFillColor);
        RectTransform rect = fill.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
