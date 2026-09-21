using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cheat panel for testing. Click DEV (or press F1) to show/hide it.
public class DevTools : MonoBehaviour
{
    [SerializeField] PlayerWallet wallet;
    [SerializeField] PlayerExperience experience;
    [SerializeField] WaveManager waves;

    [SerializeField] GameObject panel;
    [SerializeField] Button toggleButton;
    [SerializeField] Button addCoins100;
    [SerializeField] Button addCoins1000;
    [SerializeField] Button addXp10;
    [SerializeField] Button levelUp;
    [SerializeField] Button startWave;

    [Header("Spawn enemies (they rush the castle)")]
    [SerializeField] Castle castle;
    [SerializeField] Enemy meleePrefab;
    [SerializeField] Enemy archerPrefab;
    [SerializeField] Enemy fastPrefab;
    [SerializeField] Button spawnMelee;
    [SerializeField] Button spawnArchers;
    [SerializeField] Button spawnFast;
    [SerializeField] Enemy brutePrefab;
    [SerializeField] Button spawnBrute;
    [SerializeField] int spawnCount = 5;

    [Tooltip("How far from the castle they appear. Inside the castle's vision, so you can see them.")]
    [SerializeField] float spawnDistance = 22f;

    void Awake()
    {
        panel.SetActive(false);

        // Cheats are for testing: in a release build (not the editor, not a
        // Development Build) the DEV button is hidden and F1 does nothing.
        // Players can't give themselves coins that the shop would then bank.
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        toggleButton.gameObject.SetActive(false);
        enabled = false; // no Update, so no F1
#else
        Wire();
#endif
    }

    void Wire()
    {
        toggleButton.onClick.AddListener(Toggle);
        addCoins100.onClick.AddListener(() => wallet.Add(100));
        addCoins1000.onClick.AddListener(() => wallet.Add(1000));
        addXp10.onClick.AddListener(() => experience.AddXp(10));
        levelUp.onClick.AddListener(() => experience.AddXp(experience.XpToNextLevel - experience.CurrentXp));
        startWave.onClick.AddListener(waves.StartNextWave);
        spawnMelee.onClick.AddListener(() => SpawnGroup(meleePrefab));
        spawnArchers.onClick.AddListener(() => SpawnGroup(archerPrefab));
        spawnFast.onClick.AddListener(() => SpawnGroup(fastPrefab));
        spawnBrute.onClick.AddListener(() => SpawnGroup(brutePrefab, 1));
    }

    // A group at a random spot around the hero, hunting them like everything
    // the spawner makes.
    void SpawnGroup(Enemy prefab) => SpawnGroup(prefab, spawnCount);

    void SpawnGroup(Enemy prefab, int count)
    {
        if (hero == null)
            hero = FindAnyObjectByType<PlayerHealth>();
        Vector3 origin = hero != null ? hero.transform.position : castle.transform.position;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 center = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnDistance;
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 2.5f;
            Vector3 position = center + new Vector3(offset.x, 0f, offset.y);
            position.y = prefab.transform.position.y;
            Enemy enemy = Instantiate(prefab, position, Quaternion.identity);
            enemy.HuntPlayerOnly();
        }
    }

    PlayerHealth hero;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            Toggle();
    }

    void Toggle() => panel.SetActive(!panel.activeSelf);
}
