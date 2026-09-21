using UnityEngine;

// Archer skill: fire several quick volleys of arrows in a wide fan.
public class BarrageSkill : ClassSkill
{
    [SerializeField] Projectile arrowPrefab;
    [SerializeField] Weapon weapon; // for the ATK multiplier

    [SerializeField] int volleys = 3;
    [SerializeField] int arrowsPerVolley = 12;

    [Tooltip("Width of the fan, in degrees.")]
    [SerializeField] float arcAngle = 120f;

    [SerializeField] float volleyInterval = 0.15f;

    [Tooltip("Aim at the nearest enemy within this range; otherwise shoot where you're facing.")]
    [SerializeField] float aimRange = 20f;

    [Tooltip("Height the arrows fly at. Low enough to hit short enemies.")]
    [SerializeField] float arrowHeight = 0.7f;

    int volleysLeft;
    float volleyTimer;
    readonly Collider[] hits = new Collider[128];

    public override bool IsRunning => volleysLeft > 0;

    protected override void Activate()
    {
        volleysLeft = volleys;
        volleyTimer = 0f;
    }

    public override void Cancel() => volleysLeft = 0;

    protected override void Update()
    {
        if (IsRunning)
        {
            volleyTimer -= Time.deltaTime;
            if (volleyTimer <= 0f)
            {
                volleyTimer = volleyInterval;
                FireVolley();
                volleysLeft--;
            }
        }

        base.Update(); // cooldown handling
    }

    void FireVolley()
    {
        Vector3 aim = AimDirection();
        Vector3 origin = new Vector3(transform.position.x, arrowHeight, transform.position.z);

        // Spread the arrows evenly across the fan, centered on the aim direction.
        for (int i = 0; i < arrowsPerVolley; i++)
        {
            float t = arrowsPerVolley == 1 ? 0.5f : i / (float)(arrowsPerVolley - 1);
            float yaw = Mathf.Lerp(-arcAngle / 2f, arcAngle / 2f, t);
            Quaternion rotation = Quaternion.LookRotation(aim) * Quaternion.Euler(0f, yaw, 0f);
            Projectile arrow = Instantiate(arrowPrefab, origin, rotation);
            arrow.Launch(weapon.DamageMultiplier);
        }
    }

    Vector3 AimDirection()
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

        Vector3 direction = nearest != null ? nearest.transform.position - transform.position : transform.forward;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }
}
