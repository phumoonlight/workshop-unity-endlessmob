using UnityEngine;

// The Swordsman's weapon: when an enemy is close, slash in an arc toward it,
// hitting every enemy inside the arc.
public class MeleeSlash : Weapon
{
    [SerializeField] float damage = 22f;

    [Tooltip("Seconds between slashes.")]
    [SerializeField] float attackInterval = 0.7f;

    [Tooltip("How far the slash reaches, in meters.")]
    [SerializeField] float range = 3.2f;

    [Tooltip("How wide the slash is, in degrees. 360 = full circle.")]
    [SerializeField] float arcAngle = 160f;

    [SerializeField] SlashEffect effectPrefab;

    [Tooltip("Safety net only: the animation itself says when the blade lands. This is how long to wait for it before landing the hit anyway.")]
    [SerializeField] float fallbackImpactDelay = 0.6f;

    [Header("Impact feel")]
    [Tooltip("Seconds the game crawls when a blow connects. The final strike gets double. Keep it tiny: 0.05 reads as weight, 0.2 reads as lag.")]
    [SerializeField] float hitStopSeconds = 0.045f;

    [Tooltip("How far the camera is thrown when a blow connects, in metres. The final strike gets triple.")]
    [SerializeField] float shakeStrength = 0.12f;

    [Tooltip("The combo's final strike reaches this much further and wider. 1.4 = +40%.")]
    [SerializeField] float finalStrikeSize = 1.4f;
    [SerializeField] Color finalStrikeColor = new Color(1f, 0.85f, 0.3f, 0.7f);

    float areaMultiplier = 1f; // raised by the Wide Slash upgrade
    float cooldown;

    // The swing in progress, remembered until the animation says it lands.
    Enemy pendingTarget;
    float pendingStrike;
    bool pendingFinal;
    readonly Collider[] hits = new Collider[256];

    public override void AddAreaPercent(float percent) => areaMultiplier += percent / 100f;

    void Update()
    {
        UpdateImpactFallback(); // in case the animation never reports a hit

        cooldown -= Time.deltaTime;
        if (cooldown > 0f)
            return;

        float reach = range * areaMultiplier;
        Enemy target = FindNearestEnemy(transform.position, reach, hits);
        if (target == null)
            return; // nobody close enough; check again next frame

        cooldown = attackInterval / attackSpeedMultiplier;

        // Chain attack: 100% -> 125% -> 150%, and the final strike is bigger.
        float strike = NextChainStrike(out bool isFinal);
        PlayAttackAnimation();

        // The swing sound starts here, with the wind-up, not at the impact: a
        // real sword recording is quiet at first and only swells as the blade
        // comes round, so starting it early is what puts its loudest moment on
        // the hit.
        GameAudio.Play(GameAudio.Sfx.Slash);

        // BeginImpact first: if the previous swing has not landed yet it lands
        // now, and it has to land with ITS values, not the ones below.
        BeginImpact(fallbackImpactDelay);

        // Remember the swing; OnImpact below finishes it when the animation
        // reaches the frame where the blade connects.
        pendingTarget = target;
        pendingStrike = strike;
        pendingFinal = isFinal;
    }

    // The moment the blade lands, announced by the animation itself.
    protected override void OnImpact()
    {
        // A hero who died during the wind-up shouldn't still land the hit.
        if (!enabled)
            return;

        Enemy target = pendingTarget;
        float strike = pendingStrike;
        bool isFinal = pendingFinal;

        float size = isFinal ? finalStrikeSize : 1f;
        float reach = range * areaMultiplier * size;

        // Aim where the target is now: it has had the whole wind-up to move,
        // and it may have died in the meantime, in which case we swing forward.
        Vector3 slashDirection = target != null
            ? Flat(target.transform.position - transform.position).normalized
            : Flat(transform.forward).normalized;
        if (slashDirection.sqrMagnitude < 0.001f)
            slashDirection = Vector3.forward;

        float halfArc = Mathf.Min(arcAngle * areaMultiplier * size, 360f) / 2f;

        // Hit every enemy that is in range AND inside the arc.
        int count = Physics.OverlapSphereNonAlloc(transform.position, reach, hits, Layers.EnemyMask);
        int landed = 0;
        for (int i = 0; i < count; i++)
        {
            if (!hits[i].TryGetComponent(out Enemy enemy))
                continue;

            Vector3 toEnemy = Flat(enemy.transform.position - transform.position);
            if (Vector3.Angle(slashDirection, toEnemy) <= halfArc)
            {
                enemy.TakeDamage(damage * damageMultiplier * strike);
                landed++;
            }
        }

        // Only a connecting blow shakes the screen and stops time. Swinging at
        // thin air should feel like nothing, which is half of why a real hit
        // feels like something.
        if (landed > 0)
        {
            HitStop.Freeze(isFinal ? hitStopSeconds * 2f : hitStopSeconds);
            CameraFollow.Shake(isFinal ? shakeStrength * 3f : shakeStrength, isFinal ? 0.25f : 0.15f);
        }

        // Show the swoosh.
        if (effectPrefab != null)
        {
            Vector3 effectPosition = transform.position;
            effectPosition.y = 0.15f; // just above the ground
            SlashEffect effect = Instantiate(effectPrefab, effectPosition, Quaternion.LookRotation(slashDirection));
            effect.Play(reach, halfArc * 2f, isFinal ? finalStrikeColor : (Color?)null);
        }
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
