using UnityEngine;

// Assassin skill: for a few seconds, throw much faster and your daggers fly faster.
public class RapidFireSkill : ClassSkill
{
    [SerializeField] AutoShooter bow;
    [SerializeField] float duration = 6f;

    [Tooltip("Extra attack speed while active, in percent. 100 = twice as fast, 200 = three times.")]
    [SerializeField] float attackSpeedPercent = 200f;

    [Tooltip("Dagger speed while active, as a multiple of normal. 2.2 = 120% faster.")]
    [SerializeField] float projectileSpeedMultiplier = 2.2f;

    float timeLeft;

    public override bool IsRunning => timeLeft > 0f;

    protected override void Activate()
    {
        timeLeft = duration;
        bow.AddAttackSpeedPercent(attackSpeedPercent);
        bow.ProjectileSpeedMultiplier *= projectileSpeedMultiplier;
    }

    public override void Cancel()
    {
        if (IsRunning)
            End();
    }

    protected override void Update()
    {
        if (IsRunning)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
                End();
        }
        base.Update(); // cooldown handling
    }

    // Take the bonus away again.
    void End()
    {
        timeLeft = 0f;
        bow.AddAttackSpeedPercent(-attackSpeedPercent);
        bow.ProjectileSpeedMultiplier /= projectileSpeedMultiplier;
    }
}
