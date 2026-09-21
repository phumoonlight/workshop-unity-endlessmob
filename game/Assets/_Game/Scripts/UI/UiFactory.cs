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
}
