using UnityEngine;

// Dropped by enemies. Gives XP when picked up.
public class ExperienceGem : MagnetPickup
{
    [SerializeField] int xpValue = 1;

    protected override void Collect(GameObject playerObject)
    {
        GameAudio.Play(GameAudio.Sfx.Gem);
        playerObject.GetComponent<PlayerExperience>().AddXp(xpValue);
    }
}
