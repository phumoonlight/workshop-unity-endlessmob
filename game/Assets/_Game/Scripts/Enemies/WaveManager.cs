using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sends enemies in waves. Each wave has more (and tougher) enemies, arriving
// in groups from different directions. Between waves a countdown runs; the next
// wave starts when it reaches zero, or earlier if StartNextWave is called (DEV panel).
// Waves spawn around their target: the castle, or a Fort that lured them.
public class WaveManager : MonoBehaviour
{
    [SerializeField] Enemy enemyPrefab;
    [SerializeField] Castle castle;

    [Header("Brutes (large enemies)")]
    [SerializeField] Enemy brutePrefab;
    [Tooltip("Brutes join every Nth wave: 1 on wave N, 2 on wave 2N, ...")]
    [SerializeField] int bruteEveryNWaves = 5;

    [Header("Fast enemies")]
    [SerializeField] Enemy fastEnemyPrefab;
    [Tooltip("First wave that has fast enemies.")]
    [SerializeField] int fastEnemiesFromWave = 2;
    [Tooltip("Share of the wave that is fast enemies on that first wave. 0.1 = 10%.")]
    [SerializeField] float fastShareStart = 0.1f;
    [SerializeField] float fastSharePerWave = 0.02f;
    [SerializeField] float fastShareMax = 0.35f;

    [Header("Wave size")]
    [SerializeField] int firstWaveEnemies = 10;
    [SerializeField] int extraEnemiesPerWave = 5;

    [Tooltip("Each wave, enemy health grows by this fraction. 0.01 = +1% per wave.")]
    [SerializeField] float healthGrowthPerWave = 0.01f;

    [Header("Timing")]
    [Tooltip("Seconds before the first wave.")]
    [SerializeField] float firstWaveDelay = 60f;

    [Tooltip("Each later break is this many seconds longer than the one before.")]
    [SerializeField] float delayIncreasePerWave = 5f;

    [Tooltip("Breaks never get longer than this. 300 = 5 minutes.")]
    [SerializeField] float maxDelay = 300f;

    [Header("Spawning")]
    [Tooltip("How far from the castle the groups appear.")]
    [SerializeField] float spawnDistance = 32f;

    [Tooltip("How spread out each group is.")]
    [SerializeField] float groupRadius = 3f;

    [Tooltip("Every this many waves, enemies come from one more direction.")]
    [SerializeField] int wavesPerExtraDirection = 10;

    [Tooltip("Most directions enemies can come from in one wave.")]
    [SerializeField] int maxGroups = 4;

    [Tooltip("Seconds between each enemy appearing.")]
    [SerializeField] float spawnInterval = 0.15f;

    [Tooltip("Enemies never spawn further than this from the map center on X and Z.")]
    [SerializeField] float mapHalfSize = 95f;

    public int CurrentWave { get; private set; } // 0 = no wave started yet
    public bool WaveInProgress { get; private set; }

    // Enemies still to come + enemies alive. Spawned enemies are our children.
    public int EnemiesRemaining => enemiesLeftToSpawn + transform.childCount;

    // Seconds until the next wave starts by itself.
    public float TimeUntilNextWave { get; private set; }

    // What the current wave attacks: the castle, or a fort that lured it.
    public Structure WaveTarget { get; private set; }
    public bool IsLured => WaveTarget != null && !(WaveTarget is Castle);

    // The enemies of this wave, split into the groups they arrived in
    // (one per direction, plus one per camp's reinforcements).
    // Dead enemies stay in the lists as "null" (Unity's destroyed objects compare equal to null).
    public IReadOnlyList<List<Enemy>> Groups => groups;

    // Fired when a wave begins, with the wave number. Camps listen to send reinforcements.
    public event Action<int> WaveStarted;

    // Fired when every enemy of a wave is dead, with the wave number. Chests listen to this.
    public event Action<int> WaveCleared;

    readonly List<List<Enemy>> groups = new List<List<Enemy>>();
    Vector3[] directions = new Vector3[0];
    int enemiesLeftToSpawn;

    void Start()
    {
        PlanNextWave();
    }

    public void StartNextWave()
    {
        if (WaveInProgress || castle.IsDestroyed)
            return;

        CurrentWave++;
        groups.Clear();
        WaveInProgress = true;

        // A working fort lures the wave; otherwise it goes for the castle.
        Building fort = FortLure.PickRandom();
        WaveTarget = fort != null ? fort : castle;

        StartCoroutine(SpawnWave(CurrentWave));
        WaveStarted?.Invoke(CurrentWave);
    }

    // Enemy health multiplier for the current wave (camps use it too).
    public float HealthMultiplier => 1f + Mathf.Max(0, CurrentWave - 1) * healthGrowthPerWave;

    // Spawns a group of enemies at a position (used by camps) that joins the current wave.
    public void SpawnReinforcements(Vector3 center, int count)
    {
        var group = new List<Enemy>();
        groups.Add(group);
        for (int i = 0; i < count; i++)
            group.Add(SpawnEnemy(center, enemyPrefab));
    }

    // Reset the countdown and pick the directions for the next wave.
    void PlanNextWave()
    {
        int nextWave = CurrentWave + 1;

        // Wave 1: 60 s, wave 2: 65 s, wave 3: 70 s ... up to 5 minutes.
        TimeUntilNextWave = Mathf.Min(firstWaveDelay + delayIncreasePerWave * (nextWave - 1), maxDelay);

        // With wavesPerExtraDirection = 10: 1 direction on waves 1-10, 2 on 11-20, ...
        int groupCount = Mathf.Min(1 + (nextWave - 1) / wavesPerExtraDirection, maxGroups);

        directions = new Vector3[groupCount];
        float startAngle = UnityEngine.Random.Range(0f, 360f);
        for (int g = 0; g < groupCount; g++)
        {
            // Spread groups evenly around the target, with a little randomness.
            float angle = (startAngle + g * 360f / groupCount + UnityEngine.Random.Range(-30f, 30f)) * Mathf.Deg2Rad;
            directions[g] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }

    // A "coroutine" is a method that can pause itself with yield and
    // continue later, which is perfect for doing things over time.
    IEnumerator SpawnWave(int wave)
    {
        int enemyCount = firstWaveEnemies + (wave - 1) * extraEnemiesPerWave;

        var waveGroups = new List<Enemy>[directions.Length];
        for (int g = 0; g < directions.Length; g++)
        {
            waveGroups[g] = new List<Enemy>();
            groups.Add(waveGroups[g]);
        }

        // Every Nth wave, Brutes lead the attack (one per group, in turn).
        if (brutePrefab != null && bruteEveryNWaves > 0 && wave % bruteEveryNWaves == 0)
        {
            int brutes = wave / bruteEveryNWaves;
            for (int b = 0; b < brutes; b++)
            {
                int g = b % directions.Length;
                Vector3 center = WaveTarget.transform.position + directions[g] * spawnDistance;
                waveGroups[g].Add(SpawnEnemy(center, brutePrefab));
            }
        }

        enemiesLeftToSpawn = enemyCount;
        for (int i = 0; i < enemyCount; i++)
        {
            int g = i % directions.Length;
            Vector3 center = WaveTarget.transform.position + directions[g] * spawnDistance;
            waveGroups[g].Add(SpawnEnemy(center, PickEnemyType(wave)));
            enemiesLeftToSpawn--;

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // Mostly normal enemies, with a growing share of fast ones from fastEnemiesFromWave on.
    Enemy PickEnemyType(int wave)
    {
        if (fastEnemyPrefab == null || wave < fastEnemiesFromWave)
            return enemyPrefab;
        float fastShare = Mathf.Min(fastShareStart + fastSharePerWave * (wave - fastEnemiesFromWave), fastShareMax);
        return UnityEngine.Random.value < fastShare ? fastEnemyPrefab : enemyPrefab;
    }

    Enemy SpawnEnemy(Vector3 center, Enemy prefab)
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * groupRadius;
        Vector3 position = center + new Vector3(offset.x, 0f, offset.y);
        position.x = Mathf.Clamp(position.x, -mapHalfSize, mapHalfSize); // stay on the map
        position.z = Mathf.Clamp(position.z, -mapHalfSize, mapHalfSize);
        position.y = prefab.transform.position.y;

        Enemy enemy = Instantiate(prefab, position, Quaternion.identity, transform);
        enemy.MultiplyHealth(HealthMultiplier);
        enemy.SetPrimaryTarget(WaveTarget);
        return enemy;
    }

    void Update()
    {
        if (castle.IsDestroyed)
            return;

        // Between waves: count down, and start the wave when time runs out.
        if (!WaveInProgress)
        {
            TimeUntilNextWave -= Time.deltaTime;
            if (TimeUntilNextWave <= 0f)
                StartNextWave();
            return;
        }

        // The wave is over once everything has spawned and been killed.
        if (EnemiesRemaining == 0)
        {
            WaveInProgress = false;
            groups.Clear();
            WaveCleared?.Invoke(CurrentWave);
            PlanNextWave();
        }
    }
}
