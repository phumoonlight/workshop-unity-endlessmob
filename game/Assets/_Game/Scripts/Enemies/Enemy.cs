using UnityEngine;
using UnityEngine.Serialization;

// A basic enemy. What it does depends on its Role (see below): a Hunter goes
// straight for the hero (every enemy the spawner makes), a Guard stays at its
// camp, an Attacker marches on the castle (the shelved castle mode).
// At any moment it is in one State: Idle, Chase or Attack (see State below).
// Dies when health runs out.
[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [SerializeField] float maxHealth = 20f;
    [SerializeField] float moveSpeed = 3f;

    [Tooltip("Chase the player instead of the castle when they're closer than this.")]
    [SerializeField] float aggroRange = 5f;

    [Tooltip("Once it spots the player it never gives up the chase (normal enemies give up when the player gets away).")]
    [FormerlySerializedAs("ignoresPlayer")] // keeps the value saved under the old name
    [SerializeField] bool relentless;

    [Tooltip("Damage of one melee hit on the player.")]
    [SerializeField] float playerDamage = 10f;

    [Tooltip("Attack a district building instead of the castle when it's closer than this.")]
    [SerializeField] float buildingAggroRange = 6f;

    [Tooltip("Damage per second to the castle or a building while touching its walls.")]
    [FormerlySerializedAs("castleDamage")] // keeps the value saved under the old name
    [SerializeField] float structureDamage = 5f;

    [Tooltip("How close (in meters) the enemy must be to start a swing at the player.")]
    [SerializeField] float contactRange = 1.2f;

    [Header("Melee attack")]
    [Tooltip("Seconds from the start of a swing until the hit lands. Stepping away in this time dodges it.")]
    [SerializeField] float attackWindUp = 0.4f;

    [Tooltip("Seconds after the hit lands before the enemy moves or swings again.")]
    [SerializeField] float attackRecover = 0.6f;

    [Tooltip("How close (in meters) to a wall counts as touching it.")]
    [FormerlySerializedAs("castleContactRange")]
    [SerializeField] float structureContactRange = 0.9f;

    [Tooltip("The XP gem this enemy drops when it dies.")]
    [SerializeField] ExperienceGem xpGemPrefab;

    [Tooltip("The coin this enemy may drop when it dies.")]
    [SerializeField] Coin coinPrefab;

    [Tooltip("Chance to drop coins. 0.3 = 30%.")]
    [Range(0f, 1f)]
    [SerializeField] float coinDropChance = 0.3f;

    [Tooltip("How many XP gems / coins drop (big enemies drop a pile).")]
    [SerializeField] int gemDropCount = 1;
    [SerializeField] int coinDropCount = 1;

    [Header("Ranged (archers)")]
    [Tooltip("Ranged enemies stop at a distance and shoot instead of walking into you.")]
    [SerializeField] bool isRanged;
    [SerializeField] float attackRange = 9f;
    [SerializeField] float shootInterval = 1.6f;
    [SerializeField] float rangedDamage = 8f;
    [SerializeField] EnemyProjectile projectilePrefab;

    [Header("Camp guard")]
    [Tooltip("A camp guard chases the player when they're closer than this.")]
    [SerializeField] float guardAggroRange = 9f;

    [Tooltip("A camp guard stops chasing when the player is this far from the camp.")]
    [SerializeField] float guardLeashRange = 16f;

    // What kind of enemy this is. An "enum" is one variable that holds exactly
    // one choice from a named list -- like a traffic light, which is red OR
    // yellow OR green, never two at once. This used to be two yes/no switches
    // (isGuard, huntsPlayerOnly), which also allowed "both yes": a combination
    // that means nothing. With an enum that mistake cannot be written.
    enum Role
    {
        Attacker, // marches on the castle, fights what it meets (the shelved castle mode)
        Guard,    // stays at a camp, chases a hero who comes close, then walks home
        Hunter    // survivor mode: only ever goes for the hero
    }

    Role role = Role.Attacker;

    // What the enemy is doing right now. Role is set once and never changes;
    // State changes all the time, like a traffic light cycling: Chase until the
    // hero is in reach, Attack for one swing, then back to Chase. A variable
    // that says "which step are we on" plus timers that move it to the next
    // step is what people call a "state machine".
    enum State
    {
        Idle,   // nowhere to go (hero down, guard already home): stand still
        Chase,  // walk towards the target
        Attack  // stand and swing: the hit lands after attackWindUp seconds
    }

    State state = State.Idle;
    float attackTimer;   // seconds since this swing started
    bool attackLanded;   // has this swing done its damage yet?
    EnemyModel model;    // plays the swing animation (null on a model-less enemy)

    Vector3 guardHome;
    float guardAggroOverride = -1f;
    // Looking for a building to attack is spread out in time. The first scan is
    // staggered randomly so a wave of enemies spawned together doesn't all scan
    // on the same frame.
    const float buildingScanInterval = 0.3f;
    float nextBuildingScan; // staggered in Awake: Random cannot be used out here
    Structure cachedBuilding;

    Structure targetStructure; // the castle or building we're heading for (null = none)
    Structure primaryTarget;   // what this wave is after (the castle, or a fort that lured it)
    Vector3? aimPoint;         // what we'd attack right now (for ranged enemies)
    bool lockedOnPlayer;       // relentless enemies: has it spotted the player?
    float shootCooldown;

    float health;
    Rigidbody body;
    Renderer[] allRenderers; // hidden together while in fog of war, and flashed together when hit
    bool shownInFog = true;
    MaterialPropertyBlock flashBlock;
    float flashTimer;

    // Shared by all enemies, so we only search the scene once.
    static PlayerHealth player;
    static Castle castle;

    // How many enemies exist right now, anywhere. Camps use this to stop
    // growing forever: 158 at once is what makes the game stutter.
    static int aliveCount;
    public static int AliveCount => aliveCount;

    void OnDestroy()
    {
        aliveCount--;
    }

    void Awake()
    {
        aliveCount++;

        // Every attack searches the Enemy layer only (see Layers), so an enemy
        // on any other layer could never be hit. The prefabs are set in the
        // Inspector; this catches a new prefab where that was forgotten.
        if (gameObject.layer != Layers.Enemy && Layers.Enemy >= 0)
        {
            Debug.LogWarning($"{name} is not on the '{Layers.EnemyName}' layer. Fixed for now -- set it on the prefab.", this);
            gameObject.layer = Layers.Enemy;
        }

        // Stagger the first building scan so a wave spawned on one frame does
        // not all scan on the same frame afterwards.
        nextBuildingScan = Time.time + Random.Range(0f, buildingScanInterval);

        health = maxHealth;
        body = GetComponent<Rigidbody>();
        model = GetComponent<EnemyModel>();
        allRenderers = GetComponentsInChildren<Renderer>();
        flashBlock = new MaterialPropertyBlock();

        if (player == null)
            player = FindAnyObjectByType<PlayerHealth>();
        if (castle == null)
            castle = FindAnyObjectByType<Castle>();
    }

    // Called by the wave manager so later waves have tougher enemies.
    public void MultiplyHealth(float multiplier)
    {
        maxHealth *= multiplier;
        health = maxHealth;
    }

    public void MultiplySpeed(float multiplier) => moveSpeed *= multiplier;

    // While true the enemy stands still (e.g. a Brute winding up its slam).
    public bool MovementLocked { get; set; }

    // Turns this enemy into a camp guard: it stays near "home" instead of
    // marching on the castle, and only chases players who come close.
    // aggroRange < 0 keeps the default guard aggro range.
    public void MakeGuard(Vector3 home, float aggroRange = -1f)
    {
        role = Role.Guard;
        guardHome = home;
        guardAggroOverride = aggroRange;
    }

    // Head for the player from any distance and keep chasing (scout squads).
    // If the player dies, it goes back to attacking the castle.
    public void HuntPlayer() => lockedOnPlayer = true;

    // What this enemy marches on (e.g. a fort that lured the wave). Falls back to the castle.
    public void SetPrimaryTarget(Structure target) => primaryTarget = target;

    // Survivor mode: stricter than HuntPlayer above. Never stops for a building,
    // never falls back to the castle -- only ever the hero, from anywhere on the
    // map. Set by EnemySpawner on everything it makes.
    public void HuntPlayerOnly() => role = Role.Hunter;

    // Moves the enemy instantly (the spawner uses it to bring back stragglers).
    // Through the Rigidbody, so physics doesn't see it as a very fast push.
    public void Teleport(Vector3 position)
    {
        body.position = position;
        transform.position = position;
        body.linearVelocity = Vector3.zero;
    }

    // FixedUpdate runs in step with the physics engine. Use it for moving Rigidbodies.
    void FixedUpdate()
    {
        // One question -- "what am I?" -- picks the behaviour. Adding a new
        // kind of enemy later means one new name in Role and one new line here.
        Vector3? target;
        switch (role)
        {
            case Role.Guard:  target = GuardTarget();    break;
            case Role.Hunter: target = HunterTarget();   break;
            default:          target = AttackerTarget(); break;
        }

        // Step 1: which state are we in this moment?
        if (target == null || MovementLocked)
            state = State.Idle;
        else if (state != State.Attack) // a swing that has started is always finished first
        {
            if (IsPlayerInRange(contactRange))
                StartAttack();
            else
                state = State.Chase;
        }

        // Step 2: do what that state does.
        switch (state)
        {
            case State.Idle:   body.linearVelocity = Vector3.zero; break;
            case State.Attack: UpdateAttack(); break;
            default:           Chase(target.Value); break;
        }
    }

    void StartAttack()
    {
        state = State.Attack;
        attackTimer = 0f;
        attackLanded = false;
        if (model != null)
            model.Attack();
    }

    // One swing: stand still, face the hero, land the hit part-way through,
    // then a short recovery. The timer is what moves this state along.
    void UpdateAttack()
    {
        body.linearVelocity = Vector3.zero;
        if (player != null)
            FaceTowards(player.transform.position);
        attackTimer += Time.fixedDeltaTime;

        if (!attackLanded && attackTimer >= attackWindUp)
        {
            attackLanded = true;
            // Stepping out of reach during the wind-up dodges the hit.
            if (IsPlayerInRange(contactRange))
                player.TakeDamage(playerDamage);
        }

        if (attackTimer >= attackWindUp + attackRecover)
            state = State.Chase; // next FixedUpdate decides: swing again, or chase
    }

    void Chase(Vector3 target)
    {
        // Archers stop once they're close enough to shoot.
        if (isRanged && aimPoint != null && FlatDistance(aimPoint.Value, transform.position) <= attackRange * 0.85f)
        {
            body.linearVelocity = Vector3.zero;
            FaceTowards(aimPoint.Value);
            return;
        }

        Vector3 direction = target - transform.position;
        direction.y = 0f;
        direction.Normalize();

        // Setting velocity (instead of teleporting) lets enemies push each other
        // apart instead of stacking inside one another.
        body.linearVelocity = direction * moveSpeed;
        if (direction != Vector3.zero)
            body.MoveRotation(Quaternion.LookRotation(direction));
    }

    void FaceTowards(Vector3 point)
    {
        Vector3 look = point - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.001f)
            body.MoveRotation(Quaternion.LookRotation(look));
    }

    void Update()
    {
        // Only show while the player (or their buildings) can see this spot.
        bool visible = FogOfWar.IsVisible(transform.position);
        if (visible != shownInFog)
        {
            shownInFog = visible;
            foreach (Renderer r in allRenderers)
                r.enabled = visible;
        }

        // (Hurting the player happens in the Attack state, in FixedUpdate.)

        // Hurt the structure we're attacking while touching its walls.
        if (targetStructure != null && targetStructure.IsTargetable
            && FlatDistance(targetStructure.ClosestPoint(transform.position), transform.position) < structureContactRange)
            targetStructure.TakeDamage(structureDamage * Time.deltaTime);

        // Archers shoot at whatever they're attacking.
        if (isRanged)
        {
            shootCooldown -= Time.deltaTime;
            if (shootCooldown <= 0f && aimPoint != null && FlatDistance(aimPoint.Value, transform.position) <= attackRange)
            {
                shootCooldown = shootInterval;
                Shoot(aimPoint.Value);
            }
        }

        // Turn the white "hit flash" off again after a moment.
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
                foreach (Renderer r in allRenderers)
                    r.SetPropertyBlock(null);
        }
    }

    // The first item lands on the spot; the rest scatter around it.
    void Drop(MagnetPickup prefab, int index)
    {
        Vector3 position = transform.position;
        if (index > 0)
        {
            Vector2 offset = Random.insideUnitCircle * 1.8f;
            position += new Vector3(offset.x, 0f, offset.y);
        }
        position.y = prefab.transform.position.y;
        PickupPool.Spawn(prefab, position); // reuses a collected one when it can
    }

    // Normal enemies: chase a nearby player, otherwise attack a nearby building,
    // otherwise march on the castle.
    // "Vector3?" means "a Vector3, or null for nowhere to go".
    Vector3? AttackerTarget()
    {
        targetStructure = FindNearbyBuilding();
        if (targetStructure == null)
        {
            if (primaryTarget != null && primaryTarget.IsTargetable)
                targetStructure = primaryTarget;
            else if (castle != null && !castle.IsDestroyed)
                targetStructure = castle;
        }

        float playerRange = isRanged ? Mathf.Max(aggroRange, attackRange) : aggroRange;
        bool playerAlive = player != null && !player.IsDead;
        if (relentless && playerAlive && IsPlayerInRange(playerRange))
            lockedOnPlayer = true;  // spotted: chase forever
        if (!playerAlive)
            lockedOnPlayer = false; // player died: go back to attacking

        if (lockedOnPlayer || IsPlayerInRange(playerRange))
        {
            aimPoint = player.transform.position;
            return player.transform.position;
        }
        if (targetStructure != null)
        {
            aimPoint = targetStructure.ClosestPoint(transform.position);
            return targetStructure.transform.position;
        }
        aimPoint = null;
        return null;
    }

    // Hunters (survivor mode): the hero, from anywhere on the map, and nothing else.
    Vector3? HunterTarget()
    {
        targetStructure = null; // never stop to hit a building on the way
        if (player != null && !player.IsDead)
        {
            aimPoint = player.transform.position;
            return player.transform.position;
        }
        aimPoint = null;
        return null; // hero is down: stand still
    }

    void Shoot(Vector3 target)
    {
        if (projectilePrefab == null)
            return;
        Vector3 origin = transform.position + Vector3.up * 0.4f;
        Vector3 direction = target - origin;
        direction.y = 0f; // fly flat
        if (direction.sqrMagnitude < 0.001f)
            return;
        EnemyProjectile arrow = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
        arrow.Launch(rangedDamage);
    }

    // The closest attackable building (not the castle) within buildingAggroRange.
    //
    // Two things matter here, because this runs for every enemy alive and there
    // can be over a hundred of them:
    //  - it is thrown away and redone only a few times a second, not 50 times.
    //    Buildings do not move, so the answer is good for a while.
    //  - the loop walks the list by index. "foreach" over an IReadOnlyList goes
    //    through the interface and allocates an enumerator object each time,
    //    which at 158 enemies x 50 physics steps is thousands of pieces of
    //    garbage a second.
    Structure FindNearbyBuilding()
    {
        if (Time.time < nextBuildingScan)
            return cachedBuilding != null && cachedBuilding.IsTargetable ? cachedBuilding : null;

        nextBuildingScan = Time.time + buildingScanInterval;

        Structure best = null;
        float bestDistance = buildingAggroRange;
        System.Collections.Generic.IReadOnlyList<Structure> structures = Structure.All;
        for (int i = 0; i < structures.Count; i++)
        {
            Structure s = structures[i];
            if (s is Castle || !s.IsTargetable)
                continue;
            float d = FlatDistance(s.ClosestPoint(transform.position), transform.position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = s;
            }
        }

        cachedBuilding = best;
        return best;
    }

    // Camp guards: chase a nearby player (but not too far from camp), otherwise go home.
    Vector3? GuardTarget()
    {
        float aggro = guardAggroOverride > 0f ? guardAggroOverride : guardAggroRange;
        float leash = Mathf.Max(guardLeashRange, aggro + 4f);
        if (IsPlayerInRange(aggro) && FlatDistance(player.transform.position, guardHome) < leash)
        {
            aimPoint = player.transform.position;
            return player.transform.position;
        }
        aimPoint = null;
        if (FlatDistance(transform.position, guardHome) > 1f)
            return guardHome;
        return null; // already home: stand still
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    bool IsPlayerInRange(float range)
    {
        if (player == null || player.IsDead)
            return false;

        Vector3 offset = player.transform.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude < range * range;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        GameAudio.Play(GameAudio.Sfx.Hit);

        // A spark at about chest height, where a blow would land. The box is one
        // unit tall before its scale, so a quarter of that scale is half way up.
        Vfx.Play(Vfx.Kind.Hit, transform.position + Vector3.up * transform.localScale.y * 0.25f);

        // Flash white for a split second so hits feel responsive.
        flashBlock.SetColor("_BaseColor", Color.white);
        foreach (Renderer r in allRenderers)
            r.SetPropertyBlock(flashBlock);
        flashTimer = 0.08f;

        if (health <= 0f)
            Die();
    }

    void Die()
    {
        GameStats.Kills++;
        GameAudio.Play(GameAudio.Sfx.EnemyDie);

        // Bigger enemies leave a bigger puff, so a Brute's death reads as an event.
        Vfx.Play(Vfx.Kind.Death, transform.position + Vector3.up * transform.localScale.y * 0.25f, transform.localScale.y);

        // Loot: normal enemies drop 1 gem (and maybe a coin); big ones drop a pile.
        for (int i = 0; i < gemDropCount && xpGemPrefab != null; i++)
            Drop(xpGemPrefab, i);

        // Random.value is a random number from 0 to 1.
        if (coinPrefab != null && Random.value < coinDropChance)
            for (int i = 0; i < coinDropCount; i++)
                Drop(coinPrefab, i + 1);

        Destroy(gameObject);
    }
}
