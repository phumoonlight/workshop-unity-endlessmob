using System.Collections.Generic;
using UnityEngine;

// Places small and medium enemy camps around the map, and keeps them topped up.
// Each size has a limit. While there are fewer than that, a new camp appears
// every so often; at the limit, nothing new appears until one is cleared.
// (Large camps are FortifiedCampManager's job.)
public class CampManager : MonoBehaviour
{
    [Tooltip("The medium camp.")]
    [SerializeField] EnemyCamp campPrefab;
    [SerializeField] EnemyCamp smallCampPrefab;
    [SerializeField] EnemySpawner spawner;
    [SerializeField] Castle castle;
    [SerializeField] Transform player;
    [SerializeField] LevelUpPopup messages;

    [Header("Medium camps")]
    [SerializeField] int startingCamps = 1;
    [Tooltip("Never more than this many medium camps at once.")]
    [SerializeField] int maxMediumCamps = 4;
    [Tooltip("While below the limit, a new medium camp appears this often. 120 = every 2 minutes.")]
    [SerializeField] float mediumRespawnSeconds = 120f;

    [Header("Small camps")]
    [SerializeField] int startingSmallCamps = 8;
    [Tooltip("Never more than this many small camps at once.")]
    [SerializeField] int maxSmallCamps = 8;
    [Tooltip("While below the limit, a new small camp appears this often. 60 = every minute.")]
    [SerializeField] float smallRespawnSeconds = 60f;
    [SerializeField] float smallMinDistanceFromCastle = 35f;
    [SerializeField] float smallMaxDistanceFromCastle = 90f;
    [SerializeField] float minDistanceBetweenSmallCamps = 15f;

    [Header("Placement")]
    [SerializeField] float minDistanceFromCastle = 45f;
    [SerializeField] float maxDistanceFromCastle = 85f;
    [SerializeField] float minDistanceFromPlayer = 30f;
    [SerializeField] float minDistanceBetweenCamps = 35f;

    [Tooltip("Camps must be at least this far from your buildings, fortified camps and outposts.")]
    [SerializeField] float minDistanceFromBases = 20f;

    [Tooltip("Camps stay within this distance of the map center on X and Z (the ground is 200 x 200).")]
    [SerializeField] float mapHalfSize = 92f;

    readonly List<EnemyCamp> smallCamps = new List<EnemyCamp>();
    readonly List<EnemyCamp> mediumCamps = new List<EnemyCamp>();

    float smallTimer;
    float mediumTimer;
    Camera cam;

    // Wait one frame so the fortified camps (placed in their own Start) exist first.
    System.Collections.IEnumerator Start()
    {
        cam = Camera.main;
        yield return null;
        for (int i = 0; i < Mathf.Min(startingCamps, maxMediumCamps); i++)
            SpawnCamp(announce: false);
        for (int i = 0; i < Mathf.Min(startingSmallCamps, maxSmallCamps); i++)
            SpawnSmallCamp();
    }

    // Game time, so no camps appear while a menu has the game paused.
    void Update()
    {
        if (GameStats.IsGameOver)
            return;

        // Cleared camps destroy themselves and turn into nulls in the lists.
        smallCamps.RemoveAll(camp => camp == null);
        mediumCamps.RemoveAll(camp => camp == null);

        // The timer only runs while below the limit, so a camp you clear is
        // replaced a full interval later -- not instantly because the clock
        // happened to be nearly up.
        if (smallCamps.Count < maxSmallCamps)
        {
            smallTimer += Time.deltaTime;
            if (smallTimer >= smallRespawnSeconds)
            {
                smallTimer = 0f;
                SpawnSmallCamp();
            }
        }
        else
            smallTimer = 0f;

        if (mediumCamps.Count < maxMediumCamps)
        {
            mediumTimer += Time.deltaTime;
            if (mediumTimer >= mediumRespawnSeconds)
            {
                mediumTimer = 0f;
                SpawnCamp(announce: true);
            }
        }
        else
            mediumTimer = 0f;
    }

    void SpawnSmallCamp()
    {
        Vector3 position = FindCampPosition(smallMinDistanceFromCastle, smallMaxDistanceFromCastle, minDistanceBetweenSmallCamps);
        EnemyCamp camp = Instantiate(smallCampPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
        camp.Setup(player, (spawner != null ? spawner.HealthMultiplier : 1f));
        smallCamps.Add(camp);
    }

    void SpawnCamp(bool announce)
    {
        Vector3 position = FindCampPosition(minDistanceFromCastle, maxDistanceFromCastle, minDistanceBetweenCamps);
        EnemyCamp camp = Instantiate(campPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
        camp.Setup(player, (spawner != null ? spawner.HealthMultiplier : 1f));
        camp.Discovered += OnCampDiscovered;
        camp.Cleared += OnCampCleared;
        mediumCamps.Add(camp);

        if (announce)
            messages.ShowMessage("<color=#ff6060>A NEW ENEMY CAMP APPEARED</color>", "Somewhere out in the wilds...");
    }

    void OnCampDiscovered(EnemyCamp camp)
    {
        if (!camp.Announces) return;
        messages.ShowMessage("<color=#ff6060>ENEMY CAMP DISCOVERED</color>",
            "Clear it for a chest");
    }

    void OnCampCleared(EnemyCamp camp)
    {
        if (!camp.Announces) return;
        messages.ShowMessage("<color=#80ff80>CAMP CLEARED!</color>", "<color=#ffd24a>Its loot spilled out</color>");
    }

    // Try random spots until one is far enough from the castle, the player, other camps
    // and your bases. If none fits (e.g. the city has grown big), search
    // further out from the castle, then relax the spacing a bit rather than give up.
    Vector3 FindCampPosition(float minDistanceFromCastle, float maxDistanceFromCastle, float minDistanceBetweenCamps)
    {
        for (int attempt = 0; attempt < 300; attempt++)
        {
            float reach = 1f + (attempt / 60) * 0.25f;       // every 60 tries, look 25% further out
            float relax = attempt < 240 ? 1f : 0.6f;          // last resort: smaller gaps
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(minDistanceFromCastle, maxDistanceFromCastle * reach);
            Vector3 candidate = castle.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;

            if (Mathf.Abs(candidate.x) > mapHalfSize || Mathf.Abs(candidate.z) > mapHalfSize)
                continue; // off the edge of the map

            if (FlatDistance(candidate, player.position) < minDistanceFromPlayer * relax)
                continue;

            if (relax >= 1f && IsOnScreen(candidate))
                continue; // don't pop up where the player can currently see

            bool tooCloseToCamp = TooClose(smallCamps, candidate, minDistanceBetweenCamps * relax)
                                  || TooClose(mediumCamps, candidate, minDistanceBetweenCamps * relax);
            if (tooCloseToCamp)
                continue;

            if (IsNearBase(candidate, minDistanceFromBases * relax))
                continue;

            return candidate;
        }

        // Fallback: straight out from the castle, away from the player.
        Vector3 away = castle.transform.position - player.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = Vector3.forward;
        return castle.transform.position + away.normalized * maxDistanceFromCastle;
    }

    // True if the spot is close to one of the player's structures, a fortified camp or an outpost site.
    bool IsNearBase(Vector3 point, float gap)
    {
        foreach (Structure s in Structure.All)
            if (!(s is Castle) && FlatDistance(point, s.transform.position) < gap)
                return true;
        foreach (FortifiedCamp f in FindObjectsByType<FortifiedCamp>(FindObjectsSortMode.None))
            if (FlatDistance(point, f.transform.position) < f.Radius + gap)
                return true;
        foreach (OutpostSite o in FindObjectsByType<OutpostSite>(FindObjectsSortMode.None))
            if (FlatDistance(point, o.transform.position) < o.Radius + gap)
                return true;
        return false;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static bool TooClose(List<EnemyCamp> list, Vector3 point, float distance)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && FlatDistance(point, list[i].transform.position) < distance)
                return true;
        return false;
    }

    // This used to ask the fog of war. With the fog switched off it answered
    // "visible" for every spot on the map, so every placement was rejected
    // until the fallback kicked in with the spacing cut to 60%.
    bool IsOnScreen(Vector3 point)
    {
        if (cam == null)
            return false;
        Vector3 v = cam.WorldToViewportPoint(point);
        return v.z > 0f && v.x > -0.1f && v.x < 1.1f && v.y > -0.1f && v.y < 1.1f;
    }
}
