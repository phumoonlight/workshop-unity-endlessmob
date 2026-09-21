using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// An enemy camp protected by guards. Used for both small and medium camps
// (the prefabs just have different settings).
// - "Discovered" once the player comes close. Discovered camps may send reinforcements each wave.
// - "Cleared" once every guard is dead: it leaves a reward chest and disappears.
public class EnemyCamp : MonoBehaviour
{
    [SerializeField] Enemy guardPrefab;
    [SerializeField] int guardCount = 8;

    [Tooltip("Up to this many extra guards, picked at random (0 = always exactly Guard Count).")]
    [SerializeField] int randomExtraGuards = 0;

    [Tooltip("Optional: archers mixed in with the guards.")]
    [SerializeField] Enemy archerPrefab;
    [Range(0f, 1f)]
    [SerializeField] float archerChance = 0f;

    [Tooltip("Does this camp send reinforcements when a wave starts (once discovered)?")]
    [SerializeField] bool sendsReinforcements = true;

    [Tooltip("Show big messages when this camp is discovered or cleared?")]
    [SerializeField] bool announce = true;

    [Tooltip("How far from the camp center guards stand.")]
    [SerializeField] float guardSpread = 4f;

    [Tooltip("The camp is discovered when the player comes this close.")]
    [SerializeField] float discoverRange = 25f;

    [Header("Growth")]
    [Tooltip("Every this many seconds the camp adds one more guard. 0 = never grows.")]
    [SerializeField] float growthInterval = 60f;

    [Tooltip("Camps stop growing while this many enemies are alive in the world. Every enemy is an animated character, so an unbounded count is what makes the frame rate fall apart. 0 = no limit.")]
    [SerializeField] int worldEnemyBudget = 120;
    [SerializeField] int maxGuards = 20;

    [Header("Capture")]
    [Tooltip("Optional. If set, wiping the guards starts a capture phase instead of clearing right away.")]
    [SerializeField] CampCapture capture;

    [Header("Reward")]
    [SerializeField] Coin coinPrefab;
    [SerializeField] ExperienceGem gemPrefab;

    // FormerlySerializedAs keeps the numbers already set on the prefabs: Unity
    // stores fields by name, so a plain rename would reset them to the default.
    [FormerlySerializedAs("chestCoins")]
    [SerializeField] int rewardCoins = 25;
    [FormerlySerializedAs("chestGems")]
    [SerializeField] int rewardGems = 12;

    [Tooltip("How far the loot scatters from the middle of the camp.")]
    [SerializeField] float rewardSpread = 3f;

    public bool SendsReinforcements => sendsReinforcements;
    public bool Announces => announce;
    public bool IsDiscovered { get; private set; }
    public bool IsCleared { get; private set; }

    public event Action<EnemyCamp> Discovered;
    public event Action<EnemyCamp> Cleared;

    readonly List<Enemy> guards = new List<Enemy>();
    Transform player;
    float healthMultiplier = 1f;
    float growthTimer;

    // Called by the CampManager right after creating the camp.
    public void Setup(Transform playerTransform, float guardHealthMultiplier)
    {
        player = playerTransform;
        healthMultiplier = guardHealthMultiplier;
        growthTimer = growthInterval;

        if (capture != null)
            capture.Captured += Clear;
        SpawnGarrison();
    }

    void SpawnGarrison()
    {
        int count = guardCount + UnityEngine.Random.Range(0, randomExtraGuards + 1);
        for (int i = 0; i < count; i++)
            SpawnGuard();
    }

    void SpawnGuard()
    {
        Enemy prefab = archerPrefab != null && UnityEngine.Random.value < archerChance ? archerPrefab : guardPrefab;
        Vector2 offset = UnityEngine.Random.insideUnitCircle * guardSpread;
        Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);
        position.y = prefab.transform.position.y;

        Enemy guard = Instantiate(prefab, position, Quaternion.identity, transform);
        guard.MultiplyHealth(healthMultiplier);
        guard.MakeGuard(position); // each guard stays at its own spot
        guards.Add(guard);
    }

    void Update()
    {
        if (IsCleared)
            return;

        if (!IsDiscovered && player != null)
        {
            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < discoverRange * discoverRange)
            {
                IsDiscovered = true;
                Discovered?.Invoke(this);
            }
        }

        // Dead guards become null; remove them. With no guards left the camp is
        // cleared, or (if it has a capture circle) the capture phase starts.
        guards.RemoveAll(guard => guard == null);
        if (guards.Count == 0)
        {
            if (capture == null)
                Clear();
            else if (!capture.IsActive)
                capture.Begin();
            return; // no more growth once the guards are gone
        }

        // Left alone, the camp slowly grows (discovered or not).
        if (growthInterval <= 0f)
            return;
        growthTimer -= Time.deltaTime;
        if (growthTimer <= 0f)
        {
            growthTimer = growthInterval;
            // Each camp already has its own maxGuards, but camps themselves keep
            // being added, so the world total still ran away without this.
            bool worldFull = worldEnemyBudget > 0 && Enemy.AliveCount >= worldEnemyBudget;
            if (guards.Count < maxGuards && !worldFull)
                SpawnGuard();
        }
    }

    void Clear()
    {
        IsCleared = true;
        IsDiscovered = true; // clearing a camp you never "saw" still counts

        // The loot spills out where the camp stood -- no chest to walk to and open.
        Loot.Scatter(coinPrefab, rewardCoins, transform.position, rewardSpread);
        Loot.Scatter(gemPrefab, rewardGems, transform.position, rewardSpread);

        Cleared?.Invoke(this);
        Destroy(gameObject); // the tents disappear
    }
}
