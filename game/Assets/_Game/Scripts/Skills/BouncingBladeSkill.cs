using UnityEngine;

// Assassin skill: throw one dagger that bounces from enemy to enemy.
// The bouncing itself lives on the dagger (BouncingBlade); this skill only
// picks the first target and throws it.
public class BouncingBladeSkill : ClassSkill
{
    [SerializeField] BouncingBlade bladePrefab;
    [SerializeField] Weapon weapon; // for the ATK multiplier

    [Tooltip("The blade stops after hitting this many enemies.")]
    [SerializeField] int maxTargets = 20;

    [Tooltip("The first target must be within this range.")]
    [SerializeField] float aimRange = 20f;

    [Tooltip("Height the blade starts at.")]
    [SerializeField] float throwHeight = 0.7f;

    readonly Collider[] hits = new Collider[128];
    Enemy firstTarget;

    // Nobody in range: keep the skill ready instead of wasting the cooldown.
    protected override bool CanActivate()
    {
        firstTarget = FindNearestEnemy();
        return firstTarget != null;
    }

    protected override void Activate()
    {
        Vector3 origin = new Vector3(transform.position.x, throwHeight, transform.position.z);
        Vector3 direction = firstTarget.transform.position - origin;
        direction.y = 0f;
        Quaternion rotation = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : transform.rotation;

        GameAudio.Play(GameAudio.Sfx.Shoot);
        BouncingBlade blade = Instantiate(bladePrefab, origin, rotation);
        blade.Launch(firstTarget, weapon.DamageMultiplier, maxTargets);
    }

    Enemy FindNearestEnemy()
    {
        Enemy nearest = null;
        float best = float.MaxValue;
        int count = Physics.OverlapSphereNonAlloc(transform.position, aimRange, hits);
        for (int i = 0; i < count; i++)
        {
            if (!hits[i].TryGetComponent(out Enemy enemy))
                continue;
            float d = (enemy.transform.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                nearest = enemy;
            }
        }
        return nearest;
    }
}
