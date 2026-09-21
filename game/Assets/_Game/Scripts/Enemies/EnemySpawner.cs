using UnityEngine;

// Vampire Survivors style: enemies keep arriving from just outside the edge of
// the screen and rush the hero, and there are more of them the longer you last.
// No waves, no breaks -- the pressure only goes up.
//
// (The old WaveManager is still in the scene, switched off, for when castle
// defense comes back.)
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] PlayerHealth player;

    [Header("Enemy types")]
    [SerializeField] Enemy meleePrefab;
    [SerializeField] Enemy fastPrefab;
    [SerializeField] Enemy rangedPrefab;
    [SerializeField] Enemy brutePrefab;

    [Tooltip("Share of ordinary spawns that are each type. They don't have to add up to 1 -- they're weighed against each other.")]
    [SerializeField] float meleeShare = 0.85f;
    [SerializeField] float fastShare = 0.10f;
    [SerializeField] float rangedShare = 0.05f;

    [Header("Brutes (rare)")]
    [Tooltip("Seconds survived before the first Brute. 180 = 3 minutes.")]
    [SerializeField] float firstBruteAt = 180f;
    [Tooltip("Seconds between Brutes after that.")]
    [SerializeField] float bruteEvery = 120f;

    [Header("How many, how fast")]
    [Tooltip("Enemies per second at the very start.")]
    [SerializeField] float startPerSecond = 0.7f;
    [Tooltip("Added to the spawn rate for every minute survived.")]
    [SerializeField] float extraPerSecondEachMinute = 0.45f;
    [Tooltip("The spawn rate never goes above this.")]
    [SerializeField] float maxPerSecond = 6f;
    [Tooltip("Stop spawning while this many of our enemies are alive. Every enemy is an animated character, so this protects the frame rate.")]
    [SerializeField] int maxAlive = 120;

    [Tooltip("Enemy health grows by this fraction per minute survived. 0.08 = +8% a minute.")]
    [SerializeField] float healthGrowthPerMinute = 0.08f;

    [Header("Placement")]
    [Tooltip("How far past the edge of the screen enemies appear, in metres.")]
    [SerializeField] float offscreenMargin = 4f;
    [Tooltip("Enemies left this many times further away than the spawn ring are moved back in front of you, so nothing gets lost behind.")]
    [SerializeField] float relocateBeyond = 2.2f;
    [Tooltip("Enemies never appear further than this from the map center on X and Z.")]
    [SerializeField] float mapHalfSize = 95f;

    // Seconds survived. Uses game time, so it stops during level-up and hero select.
    public float Elapsed { get; private set; }

    // Camps and fortified camps use this too, so the whole world toughens together.
    public float HealthMultiplier => 1f + healthGrowthPerMinute * Elapsed / 60f;

    // Our enemies are our children, so counting them is free.
    public int Alive => transform.childCount;

    float owed;          // fractions of an enemy carried over between frames
    float nextBruteAt;
    float relocateTimer;
    float spawnRing;     // current distance from the player to just off-screen
    Camera cam;

    void Start()
    {
        cam = Camera.main;
        nextBruteAt = firstBruteAt;
    }

    void Update()
    {
        if (player == null || GameStats.IsGameOver)
            return;

        Elapsed += Time.deltaTime;
        if (player.IsDead)
            return;

        spawnRing = MeasureSpawnRing();

        // At 0.7 a second, one enemy shows up roughly every 1.4 seconds. The
        // fraction is carried over so slow rates still spawn exactly on average.
        float perSecond = Mathf.Min(startPerSecond + extraPerSecondEachMinute * Elapsed / 60f, maxPerSecond);
        owed += perSecond * Time.deltaTime;
        while (owed >= 1f)
        {
            owed -= 1f;
            if (Alive >= maxAlive)
            {
                owed = 0f; // full: don't bank a flood for when space frees up
                break;
            }
            Spawn(PickType());
        }

        if (brutePrefab != null && Elapsed >= nextBruteAt)
        {
            nextBruteAt += bruteEvery;
            Spawn(brutePrefab);
        }

        // Once a second, bring back anything that fell far behind.
        relocateTimer -= Time.deltaTime;
        if (relocateTimer <= 0f)
        {
            relocateTimer = 1f;
            RelocateStragglers();
        }
    }

    Enemy PickType()
    {
        float total = meleeShare + fastShare + rangedShare;
        float roll = Random.value * total;
        if (roll < fastShare && fastPrefab != null) return fastPrefab;
        roll -= fastShare;
        if (roll < rangedShare && rangedPrefab != null) return rangedPrefab;
        return meleePrefab;
    }

    void Spawn(Enemy prefab)
    {
        if (prefab == null)
            return;

        Vector3 position = OffscreenPoint();
        position.y = prefab.transform.position.y;

        Enemy enemy = Instantiate(prefab, position, Quaternion.identity, transform);
        enemy.MultiplyHealth(HealthMultiplier);
        enemy.HuntPlayerOnly();
    }

    // Enemies that end up far behind (you sprinted away) are moved back to the
    // edge of the screen, like Vampire Survivors does. Otherwise they would take
    // up the maxAlive budget while walking for a minute to reach you.
    void RelocateStragglers()
    {
        float limit = spawnRing * relocateBeyond;
        Vector3 center = player.transform.position;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Vector3 offset = child.position - center;
            offset.y = 0f;
            if (offset.sqrMagnitude < limit * limit)
                continue;

            Vector3 point = OffscreenPoint();
            point.y = child.position.y;
            if (child.TryGetComponent(out Enemy enemy))
                enemy.Teleport(point);
        }
    }

    // A spot just past the edge of what the camera can see.
    Vector3 OffscreenPoint()
    {
        Vector3 center = player.transform.position;
        Vector3 point = center;

        // A few tries: near the map's edge, a spot can get pulled back on-screen
        // by the clamp, so pick another direction until one is truly hidden.
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            point = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRing;
            point.x = Mathf.Clamp(point.x, -mapHalfSize, mapHalfSize);
            point.z = Mathf.Clamp(point.z, -mapHalfSize, mapHalfSize);
            if (!IsOnScreen(point))
                break;
        }
        return point;
    }

    bool IsOnScreen(Vector3 point)
    {
        if (cam == null)
            return false;
        Vector3 v = cam.WorldToViewportPoint(point);
        // z below 0 means behind the camera: can't be seen.
        return v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
    }

    // How far from the player the corners of the screen reach on the ground,
    // plus a margin. Measured each frame because the zoom changes it: zoomed out,
    // enemies have to start further away to still be off-screen.
    float MeasureSpawnRing()
    {
        const float fallback = 40f;
        if (cam == null)
            return fallback;

        Vector3 center = player.transform.position;
        Plane ground = new Plane(Vector3.up, center);
        float furthest = 0f;

        for (int corner = 0; corner < 4; corner++)
        {
            Vector3 viewport = new Vector3(corner % 2, corner / 2, 0f);
            Ray ray = cam.ViewportPointToRay(viewport);
            // In the close-up zoom the top of the screen looks at the sky, so
            // that ray never meets the ground: treat it as "far".
            if (!ground.Raycast(ray, out float distance))
                return fallback + offscreenMargin;

            Vector3 hit = ray.GetPoint(distance) - center;
            hit.y = 0f;
            furthest = Mathf.Max(furthest, hit.magnitude);
        }

        return Mathf.Clamp(furthest, 12f, 60f) + offscreenMargin;
    }
}
