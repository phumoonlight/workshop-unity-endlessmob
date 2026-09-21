using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// During a wave, shows a red arrow at the edge of the screen for every enemy group
// that is completely off-screen, pointing toward it. Groups you can see get no arrow.
public class OffscreenEnemyIndicators : MonoBehaviour
{
    [SerializeField] WaveManager waves;

    [Tooltip("A triangle pointing RIGHT. It gets rotated to point at the enemies.")]
    [SerializeField] Sprite arrowSprite;
    [SerializeField] Color color = new Color(1f, 0.2f, 0.2f, 0.9f);
    [SerializeField] float arrowSize = 64f;

    [Tooltip("Distance in pixels between the arrows and the screen edge.")]
    [SerializeField] float edgeMargin = 60f;

    Camera cam;
    readonly List<Image> arrows = new List<Image>(); // reused between frames

    void Awake()
    {
        cam = Camera.main;
    }

    // LateUpdate: after enemies and the camera have moved this frame.
    void LateUpdate()
    {
        int used = 0;

        if (waves.WaveInProgress)
        {
            foreach (List<Enemy> group in waves.Groups)
            {
                if (TryGetOffscreenCenter(group, out Vector3 center))
                    PlaceArrow(GetArrow(used++), center);
            }
        }

        // Hide arrows we didn't need this frame.
        for (int i = used; i < arrows.Count; i++)
            arrows[i].enabled = false;
    }

    // True if the group has living enemies and NONE of them are on screen.
    // "center" is the average position of the living enemies.
    bool TryGetOffscreenCenter(List<Enemy> group, out Vector3 center)
    {
        center = Vector3.zero;
        int alive = 0;

        foreach (Enemy enemy in group)
        {
            if (enemy == null)
                continue; // dead

            Vector3 viewport = cam.WorldToViewportPoint(enemy.transform.position);
            bool onScreen = viewport.z > 0f && viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f;
            if (onScreen)
                return false; // the player can see this group

            center += enemy.transform.position;
            alive++;
        }

        if (alive == 0)
            return false;

        center /= alive;
        return true;
    }

    void PlaceArrow(Image arrow, Vector3 worldTarget)
    {
        // Direction from the screen center to the target, in pixels.
        Vector3 screenPoint = cam.WorldToScreenPoint(worldTarget);
        Vector2 screenCenter = new Vector2(Screen.width, Screen.height) / 2f;
        Vector2 direction = (Vector2)screenPoint - screenCenter;
        if (screenPoint.z < 0f)
            direction = -direction; // target is behind the camera
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.up;
        direction.Normalize();

        // Push the arrow out from the center until it touches the edge rectangle.
        float halfWidth = screenCenter.x - edgeMargin;
        float halfHeight = screenCenter.y - edgeMargin;
        float scale = Mathf.Min(halfWidth / Mathf.Max(Mathf.Abs(direction.x), 0.0001f),
                                halfHeight / Mathf.Max(Mathf.Abs(direction.y), 0.0001f));

        arrow.enabled = true;
        arrow.rectTransform.position = screenCenter + direction * scale; // overlay canvas = screen pixels
        arrow.rectTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        // Gentle pulse so it catches the eye.
        float pulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f);
        arrow.rectTransform.localScale = Vector3.one * pulse;
    }

    Image GetArrow(int index)
    {
        while (arrows.Count <= index)
        {
            var go = new GameObject("OffscreenArrow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>();
            image.sprite = arrowSprite;
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(arrowSize, arrowSize);
            arrows.Add(image);
        }
        return arrows[index];
    }
}
