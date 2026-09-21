using UnityEngine;

// Archer skill: for a few seconds, shoot much faster and your arrows fly faster.
public class RapidFireSkill : ClassSkill
{
    [SerializeField] AutoShooter bow;
    [SerializeField] float duration = 6f;

    [Tooltip("Extra attack speed while active, in percent. 100 = twice as fast.")]
    [SerializeField] float attackSpeedPercent = 100f;

    [Tooltip("Arrow speed while active, as a multiple of normal. 1.8 = 80% faster.")]
    [SerializeField] float projectileSpeedMultiplier = 1.8f;

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
