using UnityEngine;

// How many coins the player has. Later, a shop or upgrades can spend them.
public class PlayerWallet : MonoBehaviour
{
    [SerializeField] int startingCoins = 100;

    public int Coins { get; private set; }

    void Awake()
    {
        Coins = startingCoins;
    }

    public void Add(int amount)
    {
        Coins += amount;
    }

    // Returns false (and spends nothing) if there aren't enough coins.
    public bool TrySpend(int amount)
    {
        if (Coins < amount)
            return false;
        Coins -= amount;
        return true;
    }
}
