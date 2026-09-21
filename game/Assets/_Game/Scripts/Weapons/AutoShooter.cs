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

    [Header("Final strike: a small bouncing blade")]
    [Tooltip("Thrown along with the daggers on the combo's final strike. Leave empty for none.")]
    [SerializeField] BouncingBlade finalStrikeBladePrefab;

    [Tooltip("How many enemies the small blade can hit (the Q skill's blade hits 20).")]
    [SerializeField] int finalStrikeBladeTargets = 5;

    [Tooltip("The small blade's damage, as a share of the Q skill blade's. 0.5 = half.")]
    [SerializeField] float finalStrikeBladeDamage = 0.5f;

    [Tooltip("The small blade's size next to the Q skill's blade, so you can tell them apart.")]
    [SerializeField] float finalStrikeBladeSize = 0.6f;

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
    bool pendingIsFinal;

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

        // Chain attack: 100% -> 125% -> 150%, and the final strike also throws
        // a small bouncing blade (it used to be one extra arrow).
        float strike = NextChainStrike(out bool isFinal);
        int arrows = projectilesPerShot;
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
        pendingIsFinal = isFinal;
    }

    // The moment the arrow is released, announced by the animation itself.
    protected override void OnImpact()
    {
        if (!enabled)
            return;

        Enemy target = pendingTarget;
        float strike = pendingStrike;
        int arrows = pendingArrows;
        bool isFinal = pendingIsFinal;

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

        if (isFinal && finalStrikeBladePrefab != null)
            ThrowSmallBlade(target, origin, aim, strike);
    }

    // The final strike's bonus: the same blade as the Q skill, but smaller,
    // weaker and with fewer bounces. If the target already died, the blade
    // looks for another enemy by itself (or vanishes if there is none).
    void ThrowSmallBlade(Enemy target, Vector3 origin, Quaternion aim, float strike)
    {
        BouncingBlade blade = Instantiate(finalStrikeBladePrefab, origin, aim);
        blade.transform.localScale *= finalStrikeBladeSize;
        blade.Launch(target, damageMultiplier * strike * finalStrikeBladeDamage, finalStrikeBladeTargets);
    }
}
