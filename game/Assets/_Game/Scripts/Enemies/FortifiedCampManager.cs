using System.Collections.Generic;
using UnityEngine;

// Places the large, fortified camps out near the edges of the map, each on a
// different side (north, east, south, west), and keeps them topped up: never
// more than maxCamps, and a cleared one is replaced after respawnSeconds.
// Tracks how many have been cleared: that number is the "tier", so each new
// camp is tougher than the last.
public class FortifiedCampManager : MonoBehaviour
{
    [SerializeField] FortifiedCamp campPrefab;
    [SerializeField] Castle castle;
    [SerializeField] Transform player;
    [SerializeField] EnemySpawner spawner;
    [SerializeField] LevelUpPopup messages;

    [Header("How many")]
    [Tooltip("Fortified camps on the map when the run begins.")]
    [SerializeField] int startingCamps = 1;
    [Tooltip("Never more than this many fortified camps at once.")]
    [SerializeField] int maxCamps = 2;
    [Tooltip("While below the limit, a new fortified camp appears this often. 300 = every 5 minutes.")]
    [SerializeField] float respawnSeconds = 300f;
    [Tooltip("New camps never appear closer to the player than this.")]
    [SerializeField] float minDistanceFromPlayer = 45f;

    [Header("Placement")]
    [Tooltip("How far out from the castle, toward the map edge.")]
    [SerializeField] float minDistance = 70f;
    [SerializeField] float maxDistance = 84f;

    [Tooltip("How far to the left/right along the map edge the camp may shift.")]
    [SerializeField] float sideSpread = 35f;

    [Tooltip("Camps stay within this distance of the map center on X and Z.")]
    [SerializeField] float mapHalfSize = 86f;

    public int ClearedCount { get; private set; }

    // Large camps not yet captured (they destroy themselves when captured).
    public int RemainingCamps => camps.Count;

    static readonly Vector3[] Sides = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

    readonly List<FortifiedCamp> camps = new List<FortifiedCamp>();
    float timer;
    Camera cam;
    public int Tier => ClearedCount;
    public float WaveHealthMultiplier => spawner != null ? spawner.HealthMultiplier : 1f;

    void Start()
    {
        cam = Camera.main;
        for (int i = 0; i < Mathf.Min(startingCamps, maxCamps); i++)
            TrySpawnCamp();
    }

    // Game time, so nothing appears while a menu has the game paused.
    void Update()
    {
        if (GameStats.IsGameOver)
            return;

        camps.RemoveAll(camp => camp == null); // cleared camps destroy themselves

        // Like the other camps: the timer only runs while below the limit.
        if (camps.Count < maxCamps)
        {
            timer += Time.deltaTime;
            if (timer >= respawnSeconds)
            {
                timer = 0f;
                if (TrySpawnCamp())
                    messages.ShowMessage("<color=#ff6060>A FORTIFIED CAMP ROSE</color>",
                                         "Somewhere near the edge of the map...");
            }
        }
        else
            timer = 0f;
    }

    // Picks a side of the map that has no camp yet, and a spot along it that is
    // far from the player and off-screen. Returns false if nowhere fits.
    bool TrySpawnCamp()
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector3 side = Sides[Random.Range(0, Sides.Length)];
            if (attempt < 30 && SideTaken(side))
                continue; // spread them round the map; give up on that only as a last resort

            Vector3 along = new Vector3(side.z, 0f, -side.x); // sideways along that edge
            Vector3 position = castle.transform.position
                               + side * Random.Range(minDistance, maxDistance)
                               + along * Random.Range(-sideSpread, sideSpread);
            position.x = Mathf.Clamp(position.x, -mapHalfSize, mapHalfSize);
            position.z = Mathf.Clamp(position.z, -mapHalfSize, mapHalfSize);
            position.y = 0f;

            if (player != null && FlatDistance(position, player.position) < minDistanceFromPlayer)
                continue;
            if (IsOnScreen(position))
                continue;

            // Face the middle of the map, so the gate points inward.
            Quaternion facing = Quaternion.LookRotation(-side);
            FortifiedCamp camp = Instantiate(campPrefab, position, facing, transform);
            camp.Setup(player, this);
            camp.Activated += OnActivated;
            camp.Cleared += OnCleared;
            camps.Add(camp);
            return true;
        }
        return false;
    }

    bool SideTaken(Vector3 side)
    {
        for (int i = 0; i < camps.Count; i++)
        {
            if (camps[i] == null)
                continue;
            Vector3 offset = camps[i].transform.position - castle.transform.position;
            offset.y = 0f;
            if (Vector3.Dot(offset.normalized, side) > 0.7f)
                return true;
        }
        return false;
    }

    bool IsOnScreen(Vector3 point)
    {
        if (cam == null)
            return false;
        Vector3 v = cam.WorldToViewportPoint(point);
        return v.z > 0f && v.x > -0.1f && v.x < 1.1f && v.y > -0.1f && v.y < 1.1f;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void OnActivated(FortifiedCamp camp)
    {
        messages.ShowMessage($"<color=#ff6060>FORTIFIED CAMP</color>  <size=70%>Tier {camp.Tier + 1}</size>",
                             "Archers and a tower guard this camp. Clear it to claim the land!");
    }

    void OnCleared(FortifiedCamp camp)
    {
        ClearedCount++;
        messages.ShowMessage("<color=#80ff80>FORTIFIED CAMP CLEARED!</color>",
                             "The next fortified camp will be stronger...");
    }
}
