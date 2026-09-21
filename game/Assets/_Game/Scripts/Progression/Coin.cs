using UnityEngine;

// Sometimes dropped by enemies, and dropped in piles by cleared camps.
public class Coin : MagnetPickup
{
    [SerializeField] int value = 1;

    protected override void Collect(GameObject playerObject)
    {
        GameAudio.Play(GameAudio.Sfx.Coin);
        playerObject.GetComponent<PlayerWallet>().Add(value);
    }
}
