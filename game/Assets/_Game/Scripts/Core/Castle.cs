using UnityEngine;

// The thing you defend. If its health reaches zero, the game is over.
public class Castle : Structure
{
    public bool IsDestroyed { get; private set; }

    public override string DisplayName => "the castle";
    public override bool IsTargetable => !IsDestroyed;
    public override bool CanBeRepaired => !IsDestroyed && Health < maxHealth;

    // Used by the castle upgrade: multiplies max HP and current HP.
    public void MultiplyHealth(float multiplier)
    {
        maxHealth *= multiplier;
        Health *= multiplier;
    }

    protected override void OnHealthDepleted()
    {
        IsDestroyed = true;
        GameStats.IsGameOver = true;
        GameStats.SurvivedSeconds = Time.timeSinceLevelLoad;
        Time.timeScale = 0f; // freeze the game
    }
}
