using TMPro;
using UnityEngine;

// When the player is far from the castle and from all their buildings, they're in the
// "wilderness". Out there, every minute there's a chance that an enemy scout squad
// (melee + archers + fast units) finds them and hunts them down.
public class WildernessScouts : MonoBehaviour
{
    [SerializeField] PlayerHealth player;
    [SerializeField] Castle castle;
    [SerializeField] WaveManager waves;
    [SerializeField] LevelUpPopup messages;
    [SerializeField] TMP_Text label;

    [Header("What counts as wilderness")]
    [Tooltip("Further than this from the castle...")]
    [SerializeField] float castleSafeRadius = 35f;
    [Tooltip("...and further than this from every building or outpost.")]
    [SerializeField] float structureSafeRadius = 20f;

    [Header("Encounters")]
    [SerializeField] float checkInterval = 60f;
    [Range(0f, 1f)]
    [SerializeField] float encounterChance = 0.5f;
    [SerializeField] float spawnDistanceMin = 26f;
    [SerializeField] float spawnDistanceMax = 32f;

    [Header("Squad")]
    [SerializeField] Enemy meleePrefab;
    [SerializeField] Enemy archerPrefab;
    [SerializeField] Enemy fastPrefab;
    [SerializeField] Vector2Int meleeCount = new Vector2Int(2, 3);   // min, max
    [SerializeField] Vector2Int archerCount = new Vector2Int(1, 2);
    [SerializeField] Vector2Int fastCount = new Vector2Int(1, 2);

    public bool InWilderness { get; private set; }

    float timer;

    void Start()
    {
        timer = checkInterval;
    }

    void Update()
    {
        bool active = !player.IsDead && Time.timeScale > 0f && !GameStats.IsGameOver;
        InWilderness = active && IsWilderness(player.transform.position);
        label.enabled = InWilderness;

        // The timer only runs while out in the wilderness (it pauses at home).
        if (!InWilderness)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;
        timer = checkInterval;

        if (Random.value < encounterChance)
            SpawnSquad();
    }

    bool IsWilderness(Vector3 position)
    {
        if (FlatDistance(position, castle.transform.position) < castleSafeRadius)
            return false;
        foreach (Structure s in Structure.All)
            if (!(s is Castle) && FlatDistance(position, s.transform.position) < structureSafeRadius)
                return false;
        return true;
    }

    void SpawnSquad()
    {
        // The squad arrives together from one random direction, out of sight.
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);
        Vector3 center = player.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;

        SpawnSome(meleePrefab, meleeCount, center);
        SpawnSome(archerPrefab, archerCount, center);
        SpawnSome(fastPrefab, fastCount, center);

        if (messages != null)
            messages.ShowMessage("<color=#ff9060>SCOUT SQUAD!</color>", "Enemy scouts are hunting you");
    }

    void SpawnSome(Enemy prefab, Vector2Int range, Vector3 center)
    {
        int count = Random.Range(range.x, range.y + 1);
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 3f;
            Vector3 position = center + new Vector3(offset.x, 0f, offset.y);
            position.x = Mathf.Clamp(position.x, -95f, 95f);
            position.z = Mathf.Clamp(position.z, -95f, 95f);
            position.y = prefab.transform.position.y;

            Enemy enemy = Instantiate(prefab, position, Quaternion.identity);
            enemy.MultiplyHealth(waves.HealthMultiplier);
            enemy.HuntPlayer();
        }
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
