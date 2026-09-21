using UnityEngine;

// Pays out coins from every working income building (like the Resource Outpost).
public class PassiveIncome : MonoBehaviour
{
    [SerializeField] PlayerWallet wallet;

    float coinProgress; // fraction of a coin earned so far

    public float PerMinute => IncomeBuilding.TotalPerMinute;

    void Update()
    {
        // Earn a little every frame; pay out whole coins as they add up.
        coinProgress += PerMinute / 60f * Time.deltaTime;
        if (coinProgress >= 1f)
        {
            int whole = Mathf.FloorToInt(coinProgress);
            wallet.Add(whole);
            coinProgress -= whole;
        }
    }
}
