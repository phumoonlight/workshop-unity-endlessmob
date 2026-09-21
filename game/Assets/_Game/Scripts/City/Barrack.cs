using System.Collections.Generic;
using UnityEngine;

// Lets the player hire soldiers who guard the area around the barrack.
public class Barrack : MonoBehaviour
{
    [SerializeField] Soldier soldierPrefab;
    [SerializeField] int soldierCost = 15;
    [SerializeField] int maxSoldiers = 5;

    [Tooltip("Soldiers stand guard within this distance of the barrack.")]
    [SerializeField] float postRadius = 4f;

    readonly List<Soldier> soldiers = new List<Soldier>();

    public int SoldierCost => soldierCost;
    public int MaxSoldiers => maxSoldiers;

    public int SoldierCount
    {
        get
        {
            soldiers.RemoveAll(s => s == null); // dead soldiers
            return soldiers.Count;
        }
    }

    public bool HasRoom => SoldierCount < maxSoldiers;

    // Returns null on success, otherwise the reason it failed.
    public string TryHire(PlayerWallet wallet)
    {
        if (!HasRoom)
            return "Barrack is full";
        if (!wallet.TrySpend(soldierCost))
            return "Not enough coins";

        Vector2 offset = Random.insideUnitCircle * postRadius;
        Vector3 post = transform.position + new Vector3(offset.x, 0f, offset.y);
        post.y = soldierPrefab.transform.position.y;

        Soldier soldier = Instantiate(soldierPrefab, transform.position + Vector3.up * post.y, Quaternion.identity);
        soldier.Init(post);
        soldiers.Add(soldier);
        return null;
    }
}
