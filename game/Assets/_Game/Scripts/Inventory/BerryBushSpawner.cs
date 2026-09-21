using System.Collections.Generic;
using UnityEngine;

// Scatters berry bushes over the map when the game starts, keeping them out of
// the castle grounds and not too close to each other.
public class BerryBushSpawner : MonoBehaviour
{
    [SerializeField] BerryBush bushPrefab;

    [Tooltip("How many bushes to scatter.")]
    [SerializeField] int bushCount = 40;

    [Tooltip("Keep clear of the castle grounds by this much.")]
    [SerializeField] float minDistanceFromCastle = 14f;

    [Tooltip("Bushes stay within this distance of the map center on X and Z (the ground is 200 x 200).")]
    [SerializeField] float mapHalfSize = 92f;

    [Tooltip("Two bushes are never closer together than this.")]
    [SerializeField] float minDistanceBetweenBushes = 9f;

    readonly List<Vector3> placed = new List<Vector3>();

    void Start()
    {
        Castle castle = FindAnyObjectByType<Castle>();
        Vector3 center = castle != null ? castle.transform.position : Vector3.zero;

        for (int i = 0; i < bushCount; i++)
        {
            // Try a few random spots and take the first good one. Giving up after
            // 30 tries stops the game freezing if the map is already crowded.
            for (int attempt = 0; attempt < 30; attempt++)
            {
                Vector3 spot = RandomSpot(center);
                if (!IsGoodSpot(spot, center))
                    continue;

                Spawn(spot);
                break;
            }
        }
    }

    Vector3 RandomSpot(Vector3 center)
    {
        return new Vector3(
            center.x + Random.Range(-mapHalfSize, mapHalfSize),
            0f,
            center.z + Random.Range(-mapHalfSize, mapHalfSize));
    }

    bool IsGoodSpot(Vector3 spot, Vector3 center)
    {
        if (Vector3.Distance(spot, center) < minDistanceFromCastle)
            return false;

        foreach (Vector3 other in placed)
            if (Vector3.Distance(spot, other) < minDistanceBetweenBushes)
                return false;

        return true;
    }

    void Spawn(Vector3 spot)
    {
        // A random turn and size so a field of bushes doesn't look copy-pasted.
        BerryBush bush = Instantiate(bushPrefab, spot, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
        bush.transform.localScale *= Random.Range(0.85f, 1.2f);
        placed.Add(spot);
    }
}
