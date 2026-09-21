using UnityEngine;

// Swordsman skill: spin for a few seconds, hitting every enemy around you again and again.
public class WhirlwindSkill : ClassSkill
{
    [SerializeField] float duration = 5f;
    [SerializeField] float radius = 4f;

    [Tooltip("Seconds between hits while spinning.")]
    [SerializeField] float tickInterval = 0.25f;

    [Tooltip("Damage of each hit (before the player's ATK bonus).")]
    [SerializeField] float damagePerHit = 13f;

    [Tooltip("How fast the sword whirls around, in degrees per second.")]
    [SerializeField] float spinSpeed = 900f;

    [SerializeField] Weapon weapon;          // for the ATK multiplier
    [SerializeField] Transform swordProp;    // the sword model that whirls around
    [SerializeField] SlashEffect effectPrefab;

    float timeLeft;
    float tickTimer;
    float angle;
    Vector3 propPosition;
    Quaternion propRotation;
    readonly Collider[] hits = new Collider[256];

    public override bool IsRunning => timeLeft > 0f;

    protected override void Activate()
    {
        timeLeft = duration;
        tickTimer = 0f;
        angle = 0f;
        propPosition = swordProp.localPosition;
        propRotation = swordProp.localRotation;
    }

    public override void Cancel()
    {
        if (!IsRunning)
            return;
        timeLeft = 0f;
        swordProp.localPosition = propPosition;
        swordProp.localRotation = propRotation;
    }

    protected override void Update()
    {
        if (IsRunning)
        {
            timeLeft -= Time.deltaTime;

            // Whirl the sword around the player.
            angle += spinSpeed * Time.deltaTime;
            Quaternion spin = Quaternion.Euler(0f, angle, 0f);
            swordProp.localPosition = spin * propPosition;
            swordProp.localRotation = spin * propRotation;

            tickTimer -= Time.deltaTime;
            if (tickTimer <= 0f)
            {
                tickTimer = tickInterval;
                HitAround();
            }

            if (timeLeft <= 0f)
            {
                // Put the sword back where it was.
                swordProp.localPosition = propPosition;
                swordProp.localRotation = propRotation;
            }
        }

        base.Update(); // cooldown handling
    }

    void HitAround()
    {
        float damage = damagePerHit * weapon.DamageMultiplier;
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, hits);
        for (int i = 0; i < count; i++)
            if (hits[i].TryGetComponent(out Enemy enemy))
                enemy.TakeDamage(damage);

        if (effectPrefab != null)
        {
            Vector3 position = transform.position;
            position.y = 0.15f;
            SlashEffect effect = Instantiate(effectPrefab, position, Quaternion.Euler(0f, angle, 0f));
            effect.Play(radius, 360f);
        }
    }
}
