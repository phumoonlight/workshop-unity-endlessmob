using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// A big enemy base near the map edge. Its troops (melee guards + archers) appear when
// the player first comes close, using the current "tier": every fortified camp cleared
// so far makes the next ones stronger. When all troops are dead the camp is cleared,
// drops coins and leaves an empty outpost foundation.
public class FortifiedCamp : MonoBehaviour
{
    [SerializeField] Enemy guardPrefab;
    [SerializeField] Enemy archerPrefab;
    [SerializeField] EnemyTower tower;
    [SerializeField] OutpostSite sitePrefab;

    [Tooltip("Leave a foundation to build an outpost on when cleared. Off while building is switched off.")]
    [SerializeField] bool leavesOutpostSite = false;

    [Header("Size")]
    [Tooltip("Rough radius of the camp's walls.")]
    [SerializeField] float radius = 8f;
    [SerializeField] float troopSpread = 6f;

    [Tooltip("Troops appear when the player comes this close.")]
    [SerializeField] float activateRange = 40f;

    [Tooltip("Troops chase the player within this distance of their post.")]
    [SerializeField] float guardAggroRange = 13f;

    [Header("Troops (base + per tier)")]
    [SerializeField] int baseGuards = 14;
    [SerializeField] int guardsPerTier = 4;
    [SerializeField] int baseArchers = 4;
    [SerializeField] int archersPerTier = 2;

    [Header("Strength (base + per tier)")]
    [SerializeField] float healthMultiplier = 2.5f;
    [Tooltip("Extra health per tier, as a fraction of the base. 0.5 = +50% per tier.")]
    [SerializeField] float healthPerTier = 0.5f;
    [SerializeField] float speedMultiplier = 1.2f;
    [SerializeField] float speedPerTier = 0.05f;
    [SerializeField] float towerDamagePerTier = 0.5f;

    [Header("Growth")]
    [Tooltip("Every this many seconds the camp gains one more troop, even before it wakes up.")]
    [SerializeField] float growthInterval = 60f;
    [SerializeField] int maxExtraTroops = 15;
    [Range(0f, 1f)]
    [SerializeField] float extraArcherChance = 0.3f;

    [Header("Capture")]
    [SerializeField] CampCapture capture;

    [Header("Reward")]
    [SerializeField] Coin coinPrefab;
    [SerializeField] ExperienceGem gemPrefab;

    // FormerlySerializedAs keeps the numbers already set on the prefabs: Unity
    // stores fields by name, so a plain rename would reset them to the default.
    [FormerlySerializedAs("chestCoins")]
    [SerializeField] int rewardCoins = 60;
    [FormerlySerializedAs("chestGems")]
    [SerializeField] int rewardGems = 30;

    [Tooltip("How far the loot scatters from the middle of the camp.")]
    [SerializeField] float rewardSpread = 6f;

    public float Radius => radius;
    public bool IsActivated { get; private set; }
    public int Tier { get; private set; }

    public event Action<FortifiedCamp> Activated;
    public event Action<FortifiedCamp> Cleared;

    readonly List<Enemy> troops = new List<Enemy>();
    Transform player;
    FortifiedCampManager manager;
    float growthTimer;
    int extraTroops;         // troops gained over time so far
    int pendingExtraTroops;  // gained before waking up, not spawned yet
    float troopHealth;
    float troopSpeed;

    public void Setup(Transform playerTransform, FortifiedCampManager owner)
    {
        player = playerTransform;
        manager = owner;
    }

    void Awake()
    {
        tower.enabled = false; // quiet until the camp wakes up
        growthTimer = growthInterval;
        if (capture != null)
        {
            capture.Captured += Clear;
        }
    }

    void Update()
    {
        // Every minute the camp grows, whether or not anyone has found it yet.
        growthTimer -= Time.deltaTime;
        if (growthTimer <= 0f)
        {
            growthTimer = growthInterval;
            if (extraTroops < maxExtraTroops && (capture == null || !capture.IsActive))
            {
                extraTroops++;
                if (IsActivated) SpawnExtraTroop();
                else pendingExtraTroops++;
            }
        }

        if (!IsActivated)
        {
            if (player != null && FlatDistance(player.position, transform.position) < activateRange)
                Activate();
            return;
        }

        troops.RemoveAll(t => t == null); // dead troops
        if (troops.Count > 0)
            return;

        // All troops dead: start the capture phase (or clear right away if there's no capture).
        if (capture == null)
            Clear();
        else if (!capture.IsActive)
        {
            // The garrison is gone, so the tower stands down too: capturing is
            // about holding the circle against the swarm, not dodging arrows.
            tower.enabled = false;
            capture.Begin();
        }
    }

    void Activate()
    {
        IsActivated = true;
        Tier = manager.Tier;

        troopHealth = healthMultiplier * (1f + healthPerTier * Tier) * manager.WaveHealthMultiplier;
        troopSpeed = speedMultiplier + speedPerTier * Tier;

        SpawnGarrison();
        for (int i = 0; i < pendingExtraTroops; i++)
            SpawnExtraTroop();
        pendingExtraTroops = 0;

        tower.enabled = true;
        tower.SetDamageMultiplier(1f + towerDamagePerTier * Tier);
        Activated?.Invoke(this);
    }

    void SpawnTroops(Enemy prefab, int count, float health, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * troopSpread;
            Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);
            position.y = prefab.transform.position.y;

            Enemy troop = Instantiate(prefab, position, Quaternion.identity, transform);
            troop.MultiplyHealth(health);
            troop.MultiplySpeed(speed);
            troop.MakeGuard(position, guardAggroRange);
            troops.Add(troop);
        }
    }

    void SpawnGarrison()
    {
        SpawnTroops(guardPrefab, baseGuards + guardsPerTier * Tier, troopHealth, troopSpeed);
        SpawnTroops(archerPrefab, baseArchers + archersPerTier * Tier, troopHealth, troopSpeed);
    }

    void SpawnExtraTroop()
    {
        Enemy prefab = UnityEngine.Random.value < extraArcherChance ? archerPrefab : guardPrefab;
        SpawnTroops(prefab, 1, troopHealth, troopSpeed);
    }

    void Clear()
    {
        // Leave an empty foundation where an outpost can be built...
        if (leavesOutpostSite && sitePrefab != null)
            Instantiate(sitePrefab, transform.position, transform.rotation, transform.parent);

        // ...and the loot spills out across the camp -- no chest to open.
        Loot.Scatter(coinPrefab, rewardCoins, transform.position, rewardSpread);
        Loot.Scatter(gemPrefab, rewardGems, transform.position, rewardSpread);

        Cleared?.Invoke(this);
        Destroy(gameObject);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
