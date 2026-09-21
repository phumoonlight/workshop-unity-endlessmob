using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The run's main readouts, built in code on the HUD canvas:
// - a red HP bar just above the item bar
// - top-left: clock + time survived, skull + kills, coin + coins
//
// Every number is only turned into text when it changes. Building a string
// every frame for a value that changes once a second is garbage for nothing.
public class RunHud : MonoBehaviour
{
    [SerializeField] RectTransform canvasRoot;
    [SerializeField] PlayerHealth health;
    [SerializeField] PlayerWallet wallet;
    [SerializeField] EnemySpawner spawner;

    [Header("HP bar")]
    [Tooltip("Height of the bar's bottom edge above the bottom of the screen: just over the item bar.")]
    [SerializeField] float hpBarY = 134f;
    [SerializeField] Vector2 hpBarSize = new Vector2(720f, 22f);
    [SerializeField] Color hpColor = new Color(0.85f, 0.16f, 0.16f);
    [SerializeField] Color hpBackColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("Readouts (top-left)")]
    [SerializeField] float iconSize = 40f;
    [SerializeField] float fontSize = 34f;
    [SerializeField] float rowSpacing = 50f;

    RectTransform hpFill;
    TextMeshProUGUI hpText;
    TextMeshProUGUI timeText, killsText, coinsText;

    int shownHp = -1, shownMaxHp = -1, shownSeconds = -1, shownKills = -1, shownCoins = -1;

    void Start()
    {
        BuildHpBar();
        timeText = BuildRow(0, HudIcons.Clock, Color.white);
        killsText = BuildRow(1, HudIcons.Skull, Color.white);
        coinsText = BuildRow(2, HudIcons.Coin, new Color(1f, 0.85f, 0.3f));
    }

    void Update()
    {
        // HP: the bar moves every frame it changes; the text only on whole numbers.
        float max = Mathf.Max(1f, health.MaxHealth);
        hpFill.anchorMax = new Vector2(Mathf.Clamp01(health.Health / max), 1f);
        int hp = Mathf.CeilToInt(health.Health);
        int maxHp = Mathf.RoundToInt(health.MaxHealth);
        if (hp != shownHp || maxHp != shownMaxHp)
        {
            shownHp = hp;
            shownMaxHp = maxHp;
            hpText.text = $"{hp} / {maxHp}";
        }

        // The same clock as the big one at the top: stops during pauses.
        int seconds = Mathf.FloorToInt(spawner != null ? spawner.Elapsed : Time.timeSinceLevelLoad);
        if (seconds != shownSeconds)
        {
            shownSeconds = seconds;
            timeText.text = $"{seconds / 60}:{seconds % 60:00}";
        }

        if (GameStats.Kills != shownKills)
        {
            shownKills = GameStats.Kills;
            killsText.text = shownKills.ToString();
        }

        if (wallet.Coins != shownCoins)
        {
            shownCoins = wallet.Coins;
            coinsText.text = shownCoins.ToString();
        }
    }

    // ---- Building --------------------------------------------------------------

    void BuildHpBar()
    {
        // Anchored to the bottom-centre, so it stays over the item bar at any
        // screen size.
        RectTransform back = NewRect("HpBar", canvasRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        back.anchoredPosition = new Vector2(0f, hpBarY);
        back.sizeDelta = hpBarSize;
        back.gameObject.AddComponent<Image>().color = hpBackColor;

        // The fill stretches from the left edge to "health fraction" of the way
        // across -- anchorMax.x does all the work, like the XP bar.
        hpFill = NewRect("Fill", back, Vector2.zero, Vector2.one);
        hpFill.offsetMin = hpFill.offsetMax = Vector2.zero;
        hpFill.gameObject.AddComponent<Image>().color = hpColor;

        hpText = UiFactory.Text("Hp", back, Vector2.zero, hpBarSize, hpBarSize.y - 2f, TextAlignmentOptions.Center);
    }

    TextMeshProUGUI BuildRow(int index, Sprite icon, Color color)
    {
        float y = -24f - index * rowSpacing;

        RectTransform iconRect = NewRect("Icon", canvasRoot, new Vector2(0f, 1f), new Vector2(0f, 1f));
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = new Vector2(24f, y);
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        Image image = iconRect.gameObject.AddComponent<Image>();
        image.sprite = icon;
        image.raycastTarget = false;

        TextMeshProUGUI text = UiFactory.Text("Value", canvasRoot, Vector2.zero, new Vector2(300f, iconSize),
                                              fontSize, TextAlignmentOptions.Left);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(24f + iconSize + 12f, y);
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        return text;
    }

    static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0f);
        return rect;
    }
}
