using UnityEngine;

// The Assassin's weapon: automatically throws daggers at the nearest enemy in range.
// (It began as the Archer's bow, which is why the code still says "arrows".)
public class AutoShooter : Weapon
{
    [SerializeField] Projectile projectilePrefab;

    [Tooltip("Seconds between shots.")]
    [SerializeField] float fireInterval = 0.5f;

    [Tooltip("Only target enemies closer than this.")]
    [SerializeField] float range = 15f;

    [Tooltip("Height above the player's feet that arrows fly at.")]
    [SerializeField] float fireHeight = 0f;

    [Tooltip("Angle in degrees between arrows when firing more than one.")]
    [SerializeField] float spreadAngle = 12f;

    [Tooltip("Extra arrows on the combo's final strike.")]
    [SerializeField] int finalStrikeExtraArrows = 1;

    int projectilesPerShot = 1; // raised by the Multishot upgrade

    // Arrows fly this many times faster than normal (raised by Rapid Fire).
    public float ProjectileSpeedMultiplier { get; set; } = 1f;

    [Tooltip("Safety net only: the animation itself says when the arrow is released. This is how long to wait for it before firing anyway.")]
    [SerializeField] float fallbackReleaseDelay = 0.45f;

    float cooldown;
    readonly Collider[] hits = new Collider[128]; // reused every shot to avoid garbage

    // The shot in progress, remembered until the animation releases it.
    Enemy pendingTarget;
    float pendingStrike;
    int pendingArrows;

    // "override" replaces the do-nothing version from Weapon.
    public override void AddProjectiles(int count) => projectilesPerShot += count;

    void Update()
    {
        UpdateImpactFallback(); // in case the animation never reports a release

        cooldown -= Time.deltaTime;
        if (cooldown > 0f)
            return;

        Enemy target = FindNearestEnemy(transform.position, range, hits);
        if (target == null)
            return; // nothing to shoot; try again next frame

        cooldown = fireInterval / attackSpeedMultiplier;

        // Chain attack: 100% -> 125% -> 150%, and the final strike fires an extra arrow.
        float strike = NextChainStrike(out bool isFinal);
        int arrows = projectilesPerShot + (isFinal ? finalStrikeExtraArrows : 0);
        PlayAttackAnimation();

        // Remember the shot; OnImpact fires it when the animation reaches the
        // frame where the arrow leaves the bow. Arrows used to launch on the
        // first frame, before the character had drawn.
        // BeginImpact first, so a shot still waiting to be released fires with
        // its own values rather than this one's.
        BeginImpact(fallbackReleaseDelay);

        pendingTarget = target;
        pendingStrike = strike;
        pendingArrows = arrows;
    }

    // The moment the arrow is released, announced by the animation itself.
    protected override void OnImpact()
    {
        if (!enabled)
            return;

        Enemy target = pendingTarget;
        float strike = pendingStrike;
        int arrows = pendingArrows;

        GameAudio.Play(GameAudio.Sfx.Shoot);

        Vector3 origin = transform.position + Vector3.up * fireHeight;
        // Aim at the target's center (not flat), so arrows also hit short enemies
        // like the fast runner, which a flat shot at chest height would fly over.
        // The target has had the whole draw to move, and may have died, in which
        // case we simply shoot the way we are facing.
        Vector3 direction = target != null
            ? target.transform.position - origin
            : transform.forward;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector3.forward;
        Quaternion aim = Quaternion.LookRotation(direction);

        // Fan the arrows out evenly around the aim direction.
        // With 3 arrows and 12 degrees: -12, 0, +12.
        float firstAngle = -spreadAngle * (arrows - 1) / 2f;
        for (int i = 0; i < arrows; i++)
        {
            Quaternion rotation = aim * Quaternion.Euler(0f, firstAngle + spreadAngle * i, 0f);
            Projectile arrow = Instantiate(projectilePrefab, origin, rotation);
            arrow.Launch(damageMultiplier * strike, ProjectileSpeedMultiplier);
        }
    }
}
