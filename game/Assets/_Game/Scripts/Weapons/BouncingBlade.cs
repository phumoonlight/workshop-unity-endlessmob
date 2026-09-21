using System.Collections.Generic;
using UnityEngine;

// The Assassin's Bouncing Blade: a dagger that flies to an enemy, hits it, then
// jumps to the nearest enemy it hasn't hit yet, and so on.
//
// Unlike Projectile it does not fly straight and wait to bump into something.
// It always knows its target and steers at it, so it needs no collider: it
// simply counts as a hit once it gets close enough.
public class BouncingBlade : MonoBehaviour
{
    [SerializeField] float speed = 26f;
    [SerializeField] float damage = 15f;

    [Tooltip("How far the blade looks for its next target after a hit.")]
    [SerializeField] float bounceRange = 10f;

    [Tooltip("Close enough to the target to count as a hit.")]
    [SerializeField] float hitDistance = 0.5f;

    [Tooltip("Height the blade flies at. Low enough to hit short enemies.")]
    [SerializeField] float flyHeight = 0.7f;

    [Tooltip("Safety net: the blade disappears after this many seconds no matter what.")]
    [SerializeField] float maxLifetime = 8f;

    [Tooltip("Optional: the part that spins while flying (the dagger model).")]
    [SerializeField] Transform spinner;
    [SerializeField] float spinDegreesPerSecond = 1080f;

    Enemy target;
    int hitsLeft;

    // Enemies already hit, so the blade moves on instead of bouncing between two.
    // A HashSet answers "is this one in here?" quickly, however many it holds.
    readonly HashSet<Enemy> alreadyHit = new HashSet<Enemy>();
    readonly Collider[] nearby = new Collider[128]; // reused to avoid garbage

    // Called by the skill right after spawning.
    public void Launch(Enemy firstTarget, float damageMultiplier, int maxTargets)
    {
        target = firstTarget;
        damage *= damageMultiplier;
        hitsLeft = maxTargets;
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        if (spinner != null)
            spinner.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);

        // The target died on the way (killed by something else): pick another.
        if (target == null)
        {
            target = FindNextTarget();
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
        }

        Vector3 goal = target.transform.position;
        goal.y = flyHeight;

        // MoveTowards never overshoots, so a fast blade can't skip past its target.
        transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);

        Vector3 toGoal = goal - transform.position;
        if (toGoal.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toGoal);

        if (toGoal.sqrMagnitude <= hitDistance * hitDistance)
            Hit();
    }

    void Hit()
    {
        alreadyHit.Add(target);
        target.TakeDamage(damage);
        hitsLeft--;

        target = hitsLeft > 0 ? FindNextTarget() : null;
        if (target == null)
            Destroy(gameObject); // out of bounces, or nobody left in range
    }

    // The nearest enemy in bounce range that this blade hasn't hit yet.
    Enemy FindNextTarget()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, bounceRange, nearby, Layers.EnemyMask);

        Enemy nearest = null;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (!nearby[i].TryGetComponent(out Enemy enemy) || alreadyHit.Contains(enemy))
                continue;

            float distance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy;
            }
        }
        return nearest;
    }
}
