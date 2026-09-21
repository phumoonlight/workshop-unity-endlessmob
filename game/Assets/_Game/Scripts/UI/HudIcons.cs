using UnityEngine;

// Small HUD icons drawn with maths, so the game has icons before it has icon art
// -- the same idea as the placeholder sounds in GameAudio.
//
// Each icon is built from simple shapes. For every pixel we ask "how far is this
// from the edge of the shape?" (a "signed distance": negative inside, positive
// outside) and fade the last pixel of the edge, which gives smooth, non-jagged
// outlines at any size. Swap in real art later by returning a Sprite from an
// imported image instead.
public static class HudIcons
{
    const int Size = 64;

    static Sprite coin, skull, clock;

    public static Sprite Coin => coin != null ? coin : (coin = Draw(PaintCoin));
    public static Sprite Skull => skull != null ? skull : (skull = Draw(PaintSkull));
    public static Sprite Clock => clock != null ? clock : (clock = Draw(PaintClock));

    delegate Color Painter(float x, float y);

    static Sprite Draw(Painter paint)
    {
        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                pixels[y * Size + x] = paint(x + 0.5f, y + 0.5f); // pixel centres
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    // ---- The three icons (0,0 is bottom-left, 64,64 top-right) ---------------

    static Color PaintCoin(float x, float y)
    {
        Color gold = new Color(1f, 0.80f, 0.22f);
        Color dark = new Color(0.78f, 0.52f, 0.08f);
        Color shine = new Color(1f, 0.96f, 0.75f);

        Color c = Clear;
        c = Over(c, gold, Circle(x, y, 32, 32, 28));
        c = Over(c, dark, Ring(x, y, 32, 32, 20, 3f));          // the rim of the face
        c = Over(c, shine, Circle(x, y, 23, 42, 5));            // a glint
        return c;
    }

    static Color PaintSkull(float x, float y)
    {
        Color bone = new Color(0.93f, 0.92f, 0.88f);

        float head = Coverage(Mathf.Min(Circle(x, y, 32, 38, 22), Box(x, y, 32, 17, 12, 9)));
        // Eyes and nose are holes, cut out of the bone.
        float holes = Coverage(Mathf.Min(Mathf.Min(Circle(x, y, 23.5f, 36, 6.5f), Circle(x, y, 40.5f, 36, 6.5f)),
                                         Box(x, y, 32, 25, 2.5f, 3.5f)));
        // Gaps between the teeth.
        float gaps = Coverage(Mathf.Min(Box(x, y, 27, 11, 1f, 4f), Box(x, y, 37, 11, 1f, 4f)));
        float alpha = head * (1f - holes) * (1f - gaps);
        return new Color(bone.r, bone.g, bone.b, alpha);
    }

    static Color PaintClock(float x, float y)
    {
        Color face = new Color(0.15f, 0.17f, 0.22f);
        Color white = new Color(0.95f, 0.95f, 0.95f);

        Color c = Clear;
        c = Over(c, face, Circle(x, y, 32, 32, 27));
        c = Over(c, white, Ring(x, y, 32, 32, 25, 3.5f));
        c = Over(c, white, Segment(x, y, 32, 32, 32, 50, 2.5f)); // minute hand to 12
        c = Over(c, white, Segment(x, y, 32, 32, 44, 32, 2.5f)); // hour hand to 3
        c = Over(c, white, Circle(x, y, 32, 32, 3.5f));
        return c;
    }

    // ---- Shapes as signed distances (negative = inside) ----------------------

    static float Circle(float x, float y, float cx, float cy, float r)
    {
        return Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) - r;
    }

    static float Ring(float x, float y, float cx, float cy, float r, float thickness)
    {
        return Mathf.Abs(Circle(x, y, cx, cy, r)) - thickness / 2f;
    }

    // A box centred on (cx, cy) reaching hx left/right and hy up/down.
    static float Box(float x, float y, float cx, float cy, float hx, float hy)
    {
        float dx = Mathf.Abs(x - cx) - hx;
        float dy = Mathf.Abs(y - cy) - hy;
        return new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude + Mathf.Min(Mathf.Max(dx, dy), 0f);
    }

    // A thick line from (ax, ay) to (bx, by) with round ends.
    static float Segment(float x, float y, float ax, float ay, float bx, float by, float thickness)
    {
        Vector2 p = new Vector2(x - ax, y - ay);
        Vector2 ab = new Vector2(bx - ax, by - ay);
        float t = Mathf.Clamp01(Vector2.Dot(p, ab) / ab.sqrMagnitude);
        return (p - ab * t).magnitude - thickness / 2f;
    }

    // ---- Turning distances into colour ----------------------------------------

    static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

    // 1 well inside the shape, 0 outside, and a one-pixel fade across the edge.
    static float Coverage(float distance) => Mathf.Clamp01(0.5f - distance);

    // Paints a shape of one colour over what's already there.
    static Color Over(Color below, Color colour, float distance)
    {
        float a = Coverage(distance) * colour.a;
        float outA = a + below.a * (1f - a);
        if (outA <= 0f)
            return Clear;
        Color mixed = (colour * a + below * below.a * (1f - a)) / outA;
        mixed.a = outA;
        return mixed;
    }
}
