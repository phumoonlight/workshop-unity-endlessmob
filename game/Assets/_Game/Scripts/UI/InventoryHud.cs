using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Draws the inventory as a row of slots along the bottom of the screen.
// The slots are built in code when the game starts, so there's nothing to place
// by hand in the scene and the row always matches the inventory's slot count.
public class InventoryHud : MonoBehaviour
{
    [SerializeField] Inventory inventory;

    [Tooltip("Width and height of one slot, in UI pixels.")]
    [SerializeField] float slotSize = 64f;

    [Tooltip("Gap between slots.")]
    [SerializeField] float spacing = 8f;

    [SerializeField] Color emptySlotColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] Color filledSlotColor = new Color(0f, 0f, 0f, 0.7f);

    [Tooltip("Flash colour when an item is used, and when the press did nothing.")]
    [SerializeField] Color usedFlashColor = new Color(0.4f, 1f, 0.5f, 0.85f);
    [SerializeField] Color refusedFlashColor = new Color(1f, 0.35f, 0.35f, 0.85f);

    [Tooltip("How long the flash takes to fade back to normal.")]
    [SerializeField] float flashSeconds = 0.25f;

    Image[] icons;      // the coloured square standing in for an item picture
    TMP_Text[] counts;  // "12" in the corner of the slot
    TMP_Text[] keys;    // "1".."0": which key uses this slot
    Image[] backs;

    // Per slot: how much flash is left, and what colour it is.
    float[] flashLeft;
    Color[] flashColors;

    ItemUser itemUser;

    void Start()
    {
        // Start, not Awake: the inventory creates its slots in its own Awake.
        Build();
        inventory.Changed += Redraw;

        // The player object owns both the inventory and the "use item" script.
        itemUser = inventory.GetComponent<ItemUser>();
        if (itemUser != null)
            itemUser.Used += OnItemUsed;

        Redraw();
    }

    // Unsubscribing matters: without it, a destroyed HUD would still be called
    // by the inventory's event and throw errors.
    void OnDestroy()
    {
        if (inventory != null)
            inventory.Changed -= Redraw;
        if (itemUser != null)
            itemUser.Used -= OnItemUsed;
    }

    void OnItemUsed(int slotIndex, bool success)
    {
        if (slotIndex < 0 || slotIndex >= backs.Length)
            return;

        flashLeft[slotIndex] = flashSeconds;
        flashColors[slotIndex] = success ? usedFlashColor : refusedFlashColor;
    }

    // Fade each flash back down to the slot's normal colour.
    void Update()
    {
        for (int i = 0; i < backs.Length; i++)
        {
            if (flashLeft[i] <= 0f)
                continue;

            flashLeft[i] -= Time.unscaledDeltaTime;
            Color normal = inventory.GetSlot(i).IsEmpty ? emptySlotColor : filledSlotColor;
            float amount = Mathf.Clamp01(flashLeft[i] / flashSeconds);
            backs[i].color = Color.Lerp(normal, flashColors[i], amount);
        }
    }

    void Build()
    {
        int count = inventory.SlotCount;
        backs = new Image[count];
        icons = new Image[count];
        counts = new TMP_Text[count];
        keys = new TMP_Text[count];
        flashLeft = new float[count];
        flashColors = new Color[count];

        // Lay the row out from the middle: with 10 slots, the centres run
        // -4.5, -3.5 ... +4.5 slot-widths from the middle of the screen.
        float step = slotSize + spacing;
        float firstX = -step * (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            backs[i] = NewImage("Slot" + i, transform, new Vector2(firstX + step * i, 0f), new Vector2(slotSize, slotSize), emptySlotColor);

            // The icon sits inside the slot with a small margin.
            float inner = slotSize - 14f;
            icons[i] = NewImage("Icon", backs[i].transform, Vector2.zero, new Vector2(inner, inner), Color.white);

            counts[i] = NewCount(backs[i].transform);

            // Slots 1..9 use those number keys; the tenth uses "0".
            keys[i] = NewKeyLabel(backs[i].transform, i < 9 ? (i + 1).ToString() : "0");
        }
    }

    void Redraw()
    {
        for (int i = 0; i < backs.Length; i++)
        {
            ItemBag.Slot slot = inventory.GetSlot(i);

            backs[i].color = slot.IsEmpty ? emptySlotColor : filledSlotColor;
            icons[i].enabled = !slot.IsEmpty;
            if (!slot.IsEmpty)
                icons[i].color = slot.Item.color;

            counts[i].text = slot.IsEmpty ? string.Empty : slot.Count.ToString();

            bool usable = !slot.IsEmpty && slot.Item.usable;
            keys[i].color = new Color(1f, 1f, 1f, usable ? 0.9f : 0.25f);
        }
    }

    // The key hint is dimmed for items you can't use, so the row doesn't
    // promise that pressing "4" will do something when slot 4 holds a rock.
    TMP_Text NewKeyLabel(Transform parent, string label)
    {
        var go = new GameObject("Key", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(4f, -2f);
        rect.sizeDelta = new Vector2(slotSize, 20f);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 14f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        return text;
    }

    // Small helpers so Build() above stays readable.
    static Image NewImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false; // the HUD should never swallow mouse clicks
        return image;
    }

    TMP_Text NewCount(Transform parent)
    {
        var go = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-4f, 2f);
        rect.sizeDelta = new Vector2(slotSize, 24f);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = 20f;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.raycastTarget = false;
        return text;
    }
}
