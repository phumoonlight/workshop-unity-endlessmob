using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The shop panel, opened from the PreStart scene.
//
// Spends and earns banked coins (PlayerProfile.Coins) and buys into / sells from
// the stash (PlayerProfile.Stash) -- the items the next run starts with. The
// whole panel is built in code when the scene opens, so the only things placed
// in the scene are the SHOP button and this script.
public class ShopMenu : MonoBehaviour
{
    [SerializeField] RectTransform canvasRoot;
    [SerializeField] Button openButton;

    [Tooltip("What the shop sells, top to bottom. Each item's price is set on the item itself.")]
    [SerializeField] ItemData[] forSale;

    [Header("Look")]
    [SerializeField] Color panelColor = new Color(0.08f, 0.09f, 0.12f, 0.97f);
    [SerializeField] Color buyColor = new Color(0.20f, 0.55f, 0.25f);
    [SerializeField] Color sellColor = new Color(0.65f, 0.40f, 0.12f);
    [SerializeField] Color closeColor = new Color(0.35f, 0.35f, 0.40f);

    // One line of the shop.
    class Row
    {
        public ItemData item;
        public TextMeshProUGUI owned;
        public Button buy;
        public Button sell;
    }

    GameObject shop;
    TextMeshProUGUI coinsText;
    TextMeshProUGUI messageText;
    Row[] rows;
    Image[] stashIcons;
    TextMeshProUGUI[] stashCounts;
    float messageTimer;

    // PreStart asks this so Esc closes the shop rather than leaving the scene.
    public bool IsOpen => shop != null && shop.activeSelf;

    void Start()
    {
        BuildShop();
        shop.SetActive(false);
        openButton.onClick.AddListener(() => SetOpen(true));
        Refresh();
    }

    void Update()
    {
        if (shop.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetOpen(false);

        if (messageTimer > 0f)
        {
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f)
                messageText.text = "";
        }
    }

    void SetOpen(bool open)
    {
        // UI is drawn in hierarchy order: later children cover earlier ones.
        // Other screens build their own UI in code too, possibly after this
        // one, so move the shop to the end every time it opens -- on top.
        if (open)
            shop.transform.SetAsLastSibling();
        shop.SetActive(open);
        messageText.text = "";
        Refresh();
    }

    // ---- Buying and selling --------------------------------------------------

    void Buy(ItemData item)
    {
        if (PlayerProfile.Coins < item.buyPrice)
            Say("<color=#ff6060>Not enough coins</color>");
        // Check for room BEFORE taking the money, so a full stash never eats coins.
        else if (!PlayerProfile.Stash.CanFit(item, 1))
            Say("<color=#ff6060>Your stash is full</color>");
        else
        {
            PlayerProfile.Coins -= item.buyPrice;
            PlayerProfile.Stash.Add(item, 1);
            PlayerProfile.Save();
            Say($"Bought a {item.displayName}");
        }
        Refresh();
    }

    void Sell(ItemData item)
    {
        if (PlayerProfile.Stash.Remove(item, 1))
        {
            PlayerProfile.Coins += item.sellPrice;
            PlayerProfile.Save();
            Say($"Sold a {item.displayName} for {item.sellPrice}");
        }
        Refresh();
    }

    void Say(string message)
    {
        messageText.text = message;
        messageTimer = 2f;
    }

    // Everything shown is re-read from the profile, so it can never drift from
    // the real numbers.
    void Refresh()
    {
        coinsText.text = $"<color=#ffd24a>Coins  {PlayerProfile.Coins}</color>";

        foreach (Row row in rows)
        {
            row.owned.text = $"Owned  {PlayerProfile.Stash.CountOf(row.item)}";
            row.buy.interactable = PlayerProfile.Coins >= row.item.buyPrice;
            row.sell.interactable = PlayerProfile.Stash.CountOf(row.item) > 0;
        }

        for (int i = 0; i < stashIcons.Length; i++)
        {
            ItemBag.Slot slot = PlayerProfile.Stash.GetSlot(i);
            stashIcons[i].enabled = !slot.IsEmpty;
            if (!slot.IsEmpty)
                stashIcons[i].color = slot.Item.color;
            stashCounts[i].text = slot.IsEmpty ? "" : slot.Count.ToString();
        }

    }

    // ---- Building the panel --------------------------------------------------

    void BuildShop()
    {
        // A dark veil over the whole screen. It blocks clicks, so the buttons
        // underneath can't be pressed while shopping.
        Image veil = UiFactory.Box("Shop", canvasRoot, Vector2.zero, new Vector2(4000f, 4000f),
                                   new Color(0f, 0f, 0f, 0.7f), blocksClicks: true);
        shop = veil.gameObject;
        Transform panel = UiFactory.Box("Panel", shop.transform, Vector2.zero, new Vector2(1180f, 820f), panelColor).transform;

        UiFactory.Text("Title", panel, new Vector2(-300f, 355f), new Vector2(500f, 70f), 56f,
                       TextAlignmentOptions.Left, "SHOP");
        coinsText = UiFactory.Text("Coins", panel, new Vector2(300f, 355f), new Vector2(500f, 70f), 40f,
                                   TextAlignmentOptions.Right);

        rows = new Row[forSale.Length];
        for (int i = 0; i < forSale.Length; i++)
            rows[i] = BuildRow(panel, forSale[i], 230f - i * 125f);

        UiFactory.Text("StashTitle", panel, new Vector2(0f, -170f), new Vector2(1000f, 40f), 26f,
                       TextAlignmentOptions.Center, "<color=#a0a0a0>Your stash: you start the next run with these</color>");
        BuildStash(panel, -235f);

        messageText = UiFactory.Text("Message", panel, new Vector2(0f, -305f), new Vector2(1000f, 40f), 30f,
                                     TextAlignmentOptions.Center);

        Button close = UiFactory.Button("Close", panel, new Vector2(0f, -365f), new Vector2(280f, 70f),
                                        closeColor, "Close  [Esc]", out _);
        close.onClick.AddListener(() => SetOpen(false));
    }

    Row BuildRow(Transform panel, ItemData item, float y)
    {
        Row row = new Row { item = item };

        UiFactory.Box("Icon", panel, new Vector2(-500f, y), new Vector2(80f, 80f), item.color);
        UiFactory.Text("Name", panel, new Vector2(-150f, y + 22f), new Vector2(600f, 44f), 34f,
                       TextAlignmentOptions.Left, item.displayName);
        UiFactory.Text("Description", panel, new Vector2(-150f, y - 20f), new Vector2(600f, 40f), 22f,
                       TextAlignmentOptions.Left, $"<color=#c0c0c0>{item.description}</color>");
        row.owned = UiFactory.Text("Owned", panel, new Vector2(170f, y), new Vector2(160f, 40f), 24f,
                                   TextAlignmentOptions.Center);

        row.buy = UiFactory.Button("Buy", panel, new Vector2(335f, y), new Vector2(150f, 70f),
                                   buyColor, $"Buy {item.buyPrice}", out _);
        row.sell = UiFactory.Button("Sell", panel, new Vector2(495f, y), new Vector2(150f, 70f),
                                    sellColor, $"Sell {item.sellPrice}", out _);

        // Each button needs to know which item it belongs to. "item" here is a
        // separate variable for every row, so each lambda keeps its own.
        row.buy.onClick.AddListener(() => Buy(item));
        row.sell.onClick.AddListener(() => Sell(item));
        return row;
    }

    // The same coloured squares the in-game inventory bar uses.
    void BuildStash(Transform panel, float y)
    {
        int count = PlayerProfile.Stash.SlotCount;
        stashIcons = new Image[count];
        stashCounts = new TextMeshProUGUI[count];

        const float size = 72f;
        const float step = size + 10f;
        float firstX = -step * (count - 1) / 2f;
        for (int i = 0; i < count; i++)
        {
            Image back = UiFactory.Box("Slot" + i, panel, new Vector2(firstX + step * i, y), new Vector2(size, size),
                                       new Color(1f, 1f, 1f, 0.08f));
            stashIcons[i] = UiFactory.Box("Icon", back.transform, Vector2.zero, new Vector2(size - 16f, size - 16f), Color.white);
            stashCounts[i] = UiFactory.Text("Count", back.transform, new Vector2(-4f, -22f), new Vector2(size, 26f), 22f,
                                            TextAlignmentOptions.Right);
        }
    }
}
