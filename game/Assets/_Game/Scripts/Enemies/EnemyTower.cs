using UnityEngine;

// A tower in a fortified camp. Shoots arrows at the player when they're in range.
public class EnemyTower : MonoBehaviour
{
    [SerializeField] EnemyProjectile projectilePrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] float range = 15f;
    [SerializeField] float fireInterval = 1.3f;
    [SerializeField] float damage = 10f;

    float damageMultiplier = 1f;
    float cooldown;
    PlayerHealth player;

    public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;

    void Awake()
    {
        player = FindAnyObjectByType<PlayerHealth>();
    }

    void Update()
    {
        cooldown -= Time.deltaTime;
        if (cooldown > 0f || player == null || player.IsDead)
            return;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > range * range)
            return;

        cooldown = fireInterval;
        Vector3 direction = player.transform.position - firePoint.position; // aim down at the player
        EnemyProjectile arrow = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        arrow.Launch(damage * damageMultiplier);
    }
}
