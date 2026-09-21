using UnityEngine;

// The base for every weapon. "abstract" means you never add a plain Weapon to an
// object, only a specific kind (AutoShooter, MeleeSlash...). Code that only needs
// "some weapon" (like upgrades) can talk to this base class and work with all of them.
public abstract class Weapon : MonoBehaviour
{
    [Header("Chain attack")]
    [Tooltip("Damage of each strike in the combo, in order. The last one is the final strike.")]
    [SerializeField] float[] chainDamage = { 1f, 1.25f, 1.5f };

    [Tooltip("If there's no attack for this many seconds, the combo starts over.")]
    [SerializeField] float chainResetTime = 2f;

    // Changed by upgrades. "protected" = visible to weapons that build on this class.
    protected float damageMultiplier = 1f;
    protected float attackSpeedMultiplier = 1f;

    int chainStep;
    float lastAttackTime = -999f;

    // Which strike of the combo we just made (0, 1, 2). The attack animation
    // plays the swing that matches it.
    protected int LastChainStep { get; private set; }

    PlayerModel model;
    bool lookedForModel;

    // A swing that has started but has not landed yet.
    bool impactPending;
    float impactDeadline;

    // Call once per attack: returns this strike's damage multiplier and whether it's
    // the final strike of the combo, then moves the combo on to the next strike.
    protected float NextChainStrike(out bool isFinalStrike)
    {
        if (Time.time - lastAttackTime > chainResetTime)
            chainStep = 0; // too long since the last attack: start over

        LastChainStep = chainStep;
        float strike = chainDamage[chainStep];
        isFinalStrike = chainStep == chainDamage.Length - 1;

        chainStep = (chainStep + 1) % chainDamage.Length;
        lastAttackTime = Time.time;
        return strike;
    }

    // ---- When does a swing actually land? ----------------------------------
    //
    // Not when the attack starts: the character winds up first. The animation
    // itself says when, through an Animation Event placed on the frame where
    // the blade connects, which calls AnimationEventRelay on the model, which
    // calls AnimationImpact below. So the damage is tied to the picture of the
    // blow instead of to a number we guessed.
    //
    // "fallbackSeconds" is a safety net for when no event arrives at all --
    // there is no character model, the swing was interrupted, someone forgot
    // the event on a new clip. Better a slightly mistimed hit than none.
    // Set to true to log every step of a swing (begin, animation event,
    // fallback, resolve) when attack timing needs investigating again.
    public static bool TraceImpacts = false;

    protected void BeginImpact(float fallbackSeconds)
    {
        if (TraceImpacts) Debug.Log($"[swing] {GetType().Name} begin t={Time.time:0.00} pendingAlready={impactPending} timeScale={Time.timeScale}");
        // Attacking again while the last swing is still in the air: land the
        // old one now rather than dropping it.
        if (impactPending)
            Resolve();

        impactPending = true;
        impactDeadline = Time.time + fallbackSeconds;
    }

    // Called by the animation, via the relay on the character model.
    public void AnimationImpact()
    {
        if (TraceImpacts) Debug.Log($"[swing] {GetType().Name} ANIMATION EVENT t={Time.time:0.00} pending={impactPending}");
        if (impactPending)
            Resolve();
    }

    // Weapons call this from their own Update so the safety net can run.
    protected void UpdateImpactFallback()
    {
        if (impactPending && Time.time >= impactDeadline)
        {
            if (TraceImpacts) Debug.Log($"[swing] {GetType().Name} FALLBACK fired t={Time.time:0.00}");
            Resolve();
        }
    }

    void Resolve()
    {
        if (TraceImpacts) Debug.Log($"[swing] {GetType().Name} RESOLVE t={Time.time:0.00}");
        impactPending = false;
        OnImpact();
    }

    // What the weapon does at the moment of impact. Each weapon fills this in.
    protected virtual void OnImpact() { }

    // Plays the swing matching the strike we just made. Does nothing if the
    // player is still the plain capsule with no character model on it.
    protected void PlayAttackAnimation()
    {
        if (!lookedForModel)
        {
            model = GetComponent<PlayerModel>();
            lookedForModel = true; // only look once, even when there's none
        }
        if (model != null)
            model.Attack(LastChainStep);
    }

    public float DamageMultiplier => damageMultiplier;

    public void AddDamagePercent(float percent) => damageMultiplier += percent / 100f;
    public void AddAttackSpeedPercent(float percent) => attackSpeedMultiplier += percent / 100f;

    // "virtual" = does nothing by default, but a weapon can override it if it makes sense.
    public virtual void AddProjectiles(int count) { }
    public virtual void AddAreaPercent(float percent) { }

    // Shared helper: the closest enemy within range, or null if there is none.
    protected static Enemy FindNearestEnemy(Vector3 position, float range, Collider[] buffer)
    {
        int count = Physics.OverlapSphereNonAlloc(position, range, buffer);

        Enemy nearest = null;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (!buffer[i].TryGetComponent(out Enemy enemy))
                continue;

            float distance = (enemy.transform.position - position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy;
            }
        }
        return nearest;
    }
}
