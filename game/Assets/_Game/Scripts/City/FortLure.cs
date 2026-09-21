using System.Collections.Generic;
using UnityEngine;

// Put this on a Fort. When a wave starts, the WaveManager may pick one working
// fort to lure the whole wave: it spawns around that fort and attacks it
// instead of the castle.
[RequireComponent(typeof(Building))]
public class FortLure : MonoBehaviour
{
    static readonly List<FortLure> all = new List<FortLure>();

    Building building;

    public Building Building => building;
    public bool CanLure => building != null && !building.IsRuined;

    void Awake() => building = GetComponent<Building>();
    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    // A random working fort, or null if there are none.
    public static Building PickRandom()
    {
        var working = new List<Building>();
        foreach (FortLure lure in all)
            if (lure.CanLure)
                working.Add(lure.building);
        return working.Count > 0 ? working[Random.Range(0, working.Count)] : null;
    }
}
