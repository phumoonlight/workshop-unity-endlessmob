using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Small helpers for building UI from code: a box, a line of text, a button.
// Everything is anchored to the middle of its parent, so positions read as
// "this far from the centre", which is easy to lay out by hand.
public static class UiFactory
{
    public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static Image Box(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool blocksClicks = false)
    {
        Image image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = blocksClicks;
        return image;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, Vector2 position, Vector2 size,
                                       float fontSize, TextAlignmentOptions alignment, string text = "")
    {
        TextMeshProUGUI label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.text = text;
        label.raycastTarget = false; // text should never swallow a click meant for a button
        return label;
    }

    public static Button Button(string name, Transform parent, Vector2 position, Vector2 size,
                                Color color, string text, out TextMeshProUGUI label)
    {
        Image background = Box(name, parent, position, size, color, blocksClicks: true);
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background; // the box darkens when hovered and pressed
        label = Text("Label", background.transform, Vector2.zero, size, 28f, TextAlignmentOptions.Center, text);
        return button;
    }

    // A 0..1 slider: a thin track, a coloured fill, and a handle to drag.
    //
    // Unity's Slider component does no drawing itself. It only needs to be told
    // which rectangle is the fill and which is the handle, and it then moves
    // their ANCHORS as the value changes -- the same trick as the HP and XP bars.
    // The two "area" objects just mark out how far the fill and handle may travel.
    public static Slider Slider(string name, Transform parent, Vector2 position, Vector2 size, Color fillColor)
    {
        const float HandleWidth = 26f;
        const float TrackHeight = 12f;

        RectTransform root = Rect(name, parent, position, size);

        // The track also catches clicks, so clicking anywhere on it jumps there.
        Image track = Box("Track", root, Vector2.zero, new Vector2(size.x, TrackHeight),
                          new Color(0f, 0f, 0f, 0.6f), blocksClicks: true);

        RectTransform fillArea = Rect("Fill Area", root, Vector2.zero, new Vector2(size.x, TrackHeight));
        RectTransform fill = Stretched("Fill", fillArea);
        fill.gameObject.AddComponent<Image>().color = fillColor;

        // Narrower than the track by one handle, so the handle never hangs over the ends.
        RectTransform handleArea = Rect("Handle Slide Area", root, Vector2.zero, new Vector2(size.x - HandleWidth, size.y));
        RectTransform handle = Stretched("Handle", handleArea);
        handle.sizeDelta = new Vector2(HandleWidth, 0f); // fixed width, full height
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Color.white;

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage; // the handle darkens when hovered and dragged
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    // A child that fills its parent exactly.
    static RectTransform Stretched(string name, Transform parent)
    {
        RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
}
