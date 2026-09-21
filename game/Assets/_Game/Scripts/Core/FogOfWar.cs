using System;
using UnityEngine;

// Covers the map with dark fog. The player, the castle and your buildings clear the
// fog around them. Places you've seen before stay half-dark ("explored");
// places you've never been are almost black.
//
// How it works: the map is split into a grid of cells (like pixels). Every few frames
// we work out which cells are visible, then paint that into a small texture whose
// transparency is shown on a big flat sheet lying just above the ground.
// Other scripts call FogOfWar.IsVisible(position) to hide enemies you can't see.
public class FogOfWar : MonoBehaviour
{
    public static FogOfWar Instance { get; private set; }

    [Tooltip("A flat sheet covering the whole map, with a transparent material.")]
    [SerializeField] Renderer fogRenderer;
    [SerializeField] PlayerHealth player;

    [Tooltip("Width of the map in meters (the ground is 200 x 200, centered on 0).")]
    [SerializeField] float mapSize = 200f;

    [Tooltip("Cells per side. More = sharper fog edges, but more work.")]
    [SerializeField] int resolution = 128;

    [Header("Vision (meters)")]
    [SerializeField] float playerVision = 18f;
    [SerializeField] float castleVision = 24f;
    [SerializeField] float buildingVision = 12f;

    [Header("Darkness (0 = clear, 1 = black)")]
    [Range(0f, 1f)] [SerializeField] float exploredDarkness = 0.55f;
    [Range(0f, 1f)] [SerializeField] float unexploredDarkness = 0.92f;

    [SerializeField] float updateInterval = 0.1f;

    bool[] visible;
    bool[] explored;
    Color32[] pixels;
    Texture2D texture;
    float timer;
    float cellSize;

    void Awake()
    {
        Instance = this;
        cellSize = mapSize / resolution;
        visible = new bool[resolution * resolution];
        explored = new bool[resolution * resolution];
        pixels = new Color32[resolution * resolution];

        texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear, // blends neighbouring cells = soft edges
        };
        fogRenderer.material.SetTexture("_BaseMap", texture);
        fogRenderer.enabled = true; // it's switched off in the scene so it doesn't block the editor view
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Destroy(texture);
    }

    void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f)
            return;
        timer = updateInterval;
        Refresh();
    }

    // True if a position can currently be seen. With no fog in the scene, everything is visible.
    public static bool IsVisible(Vector3 position)
    {
        return Instance == null || Instance.IsCellVisible(position);
    }

    bool IsCellVisible(Vector3 position)
    {
        int x = Mathf.FloorToInt((position.x + mapSize / 2f) / cellSize);
        int z = Mathf.FloorToInt((position.z + mapSize / 2f) / cellSize);
        if (x < 0 || z < 0 || x >= resolution || z >= resolution)
            return false;
        return visible[z * resolution + x];
    }

    void Refresh()
    {
        Array.Clear(visible, 0, visible.Length);

        if (player != null && !player.IsDead)
            Reveal(player.transform.position, playerVision);

        foreach (Structure s in Structure.All)
            Reveal(s.transform.position, s is Castle ? castleVision : buildingVision);

        // Paint the texture: alpha is how dark the fog is in each cell.
        byte exploredAlpha = (byte)(exploredDarkness * 255f);
        byte unexploredAlpha = (byte)(unexploredDarkness * 255f);
        for (int i = 0; i < pixels.Length; i++)
        {
            byte alpha = visible[i] ? (byte)0 : explored[i] ? exploredAlpha : unexploredAlpha;
            pixels[i] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    // Mark every cell within "radius" of "center" as visible (and explored).
    void Reveal(Vector3 center, float radius)
    {
        float cx = (center.x + mapSize / 2f) / cellSize;
        float cz = (center.z + mapSize / 2f) / cellSize;
        float r = radius / cellSize;

        int minX = Mathf.Max(0, Mathf.FloorToInt(cx - r));
        int maxX = Mathf.Min(resolution - 1, Mathf.CeilToInt(cx + r));
        int minZ = Mathf.Max(0, Mathf.FloorToInt(cz - r));
        int maxZ = Mathf.Min(resolution - 1, Mathf.CeilToInt(cz + r));

        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x + 0.5f - cx;
                float dz = z + 0.5f - cz;
                if (dx * dx + dz * dz > r * r)
                    continue;
                int i = z * resolution + x;
                visible[i] = true;
                explored[i] = true;
            }
        }
    }
}
