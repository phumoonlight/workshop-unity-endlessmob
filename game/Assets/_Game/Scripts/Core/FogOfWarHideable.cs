using UnityEngine;

// Hides the listed renderers while this spot is covered by fog of war.
// Used on camps: hidden until you (or your buildings) first see part of them.
// After that they stay on the map as a remembered landmark, even in fog.
public class FogOfWarHideable : MonoBehaviour
{
    [Tooltip("What to hide. Only list the camp's own parts, not its guards (enemies hide themselves).")]
    [SerializeField] Renderer[] renderers;

    [Tooltip("Once seen, keep showing it in the fog (so you remember where it is).")]
    [SerializeField] bool rememberOnceSeen = true;

    [Tooltip("Size of the thing: it shows if any point within this distance is visible.")]
    [SerializeField] float radius = 5f;

    [SerializeField] float checkInterval = 0.2f;

    bool shown = true;
    bool seen;
    float timer;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f)
            return;
        timer = checkInterval;

        if (IsAnyPartVisible())
            seen = true;
        bool visible = seen && (rememberOnceSeen || IsAnyPartVisible());
        if (visible == shown)
            return;

        shown = visible;
        foreach (Renderer r in renderers)
            if (r != null)
                r.enabled = shown;
    }

    // Check the center and 8 points around the edge.
    bool IsAnyPartVisible()
    {
        if (FogOfWar.IsVisible(transform.position))
            return true;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            Vector3 point = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (FogOfWar.IsVisible(point))
                return true;
        }
        return false;
    }
}
