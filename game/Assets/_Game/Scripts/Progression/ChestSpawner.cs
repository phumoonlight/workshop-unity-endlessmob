using UnityEngine;

// After every Nth wave is cleared, puts a treasure chest in front of the castle gate.
public class ChestSpawner : MonoBehaviour
{
    [SerializeField] TreasureChest chestPrefab;
    [SerializeField] WaveManager waves;
    [SerializeField] Castle castle;
    [SerializeField] PlayerHealth player;
    [SerializeField] LevelUpPopup messages;

    [Tooltip("A chest appears after waves 5, 10, 15... when this is 5.")]
    [SerializeField] int everyNWaves = 5;

    [Tooltip("Where the chest appears, relative to the castle. -Z is in front of the gate.")]
    [SerializeField] Vector3 offsetFromCastle = new Vector3(0f, 0f, -10f);

    [Tooltip("If a chest is still there, the next one is placed this far to the side.")]
    [SerializeField] float sideSpacing = 2.5f;

    [Header("Rewards: base + per wave")]
    [SerializeField] int baseCoins = 10;
    [SerializeField] int coinsPerWave = 2;
    [SerializeField] int baseGems = 5;
    [SerializeField] int gemsPerWave = 1;

    void OnEnable() => waves.WaveCleared += OnWaveCleared;
    void OnDisable() => waves.WaveCleared -= OnWaveCleared;

    void OnWaveCleared(int wave)
    {
        if (wave % everyNWaves != 0)
            return;

        // Unopened chests are our children. Put the new one beside them,
        // alternating right and left of the gate: 0, +1, -1, +2, -2...
        int waiting = transform.childCount;
        int step = (waiting + 1) / 2 * (waiting % 2 == 1 ? 1 : -1);

        Vector3 position = castle.transform.position + offsetFromCastle + Vector3.right * step * sideSpacing;
        position.y = 0f;

        TreasureChest chest = Instantiate(chestPrefab, position, Quaternion.identity, transform);
        chest.Setup(player, baseCoins + coinsPerWave * wave, baseGems + gemsPerWave * wave);

        if (messages != null)
            messages.ShowMessage("<color=#ffd24a>TREASURE CHEST!</color>", "A reward appeared in front of the castle gate");
    }
}
