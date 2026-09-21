using System.Collections.Generic;
using UnityEngine;

// Adds passive coin income while the building works (i.e. while this component is enabled;
// a ruined building switches it off). The DistrictManager adds up all of these.
public class IncomeBuilding : MonoBehaviour
{
    [SerializeField] float coinsPerMinute = 10f;

    static readonly List<IncomeBuilding> active = new List<IncomeBuilding>();

    public static float TotalPerMinute
    {
        get
        {
            float total = 0f;
            foreach (IncomeBuilding b in active)
                total += b.coinsPerMinute;
            return total;
        }
    }

    void OnEnable() => active.Add(this);
    void OnDisable() => active.Remove(this);
}
