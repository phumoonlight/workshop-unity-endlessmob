using UnityEngine;

// A defense tower: shoots arrows at the nearest enemy in range.
// It's a Weapon, so it could get damage/attack-speed upgrades later.
public class Tower : Weapon
{
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] float fireInterval = 0.9f;
    [SerializeField] float range = 14f;

    [Tooltip("Where arrows start (the top of the tower).")]
    [SerializeField] Transform firePoint;

    float cooldown;
    readonly Collider[] hits = new Collider[128];

    void Update()
    {
        cooldown -= Time.deltaTime;
        if (cooldown > 0f)
            return;

        Enemy target = FindNearestEnemy(transform.position, range, hits);
        if (target == null)
            return;

        cooldown = fireInterval / attackSpeedMultiplier;

        // Aim straight at the enemy (downward from the top of the tower).
        Vector3 direction = target.transform.position - firePoint.position;
        Projectile arrow = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        arrow.Launch(damageMultiplier);
    }
}
