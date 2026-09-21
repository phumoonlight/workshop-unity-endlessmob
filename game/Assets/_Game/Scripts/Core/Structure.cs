using System.Collections.Generic;
using UnityEngine;

// Anything built that has health, can be attacked by enemies and repaired by the
// player: the castle and district buildings. Each kind decides what happens when
// its health runs out (castle: game over, building: becomes a ruin).
[RequireComponent(typeof(Collider))]
public abstract class Structure : MonoBehaviour
{
    // Every structure currently in the scene, so enemies and the repair
    // script can find them without searching the whole scene.
    static readonly List<Structure> all = new List<Structure>();
    public static IReadOnlyList<Structure> All => all;

    [SerializeField] protected float maxHealth = 300f;

    [Tooltip("Seconds after taking damage before it can be repaired again. 0 = no lock.")]
    [SerializeField] float repairLockSeconds = 3f;

    float lastDamageTime = -999f;

    public float Health { get; protected set; }
    public float MaxHealth => maxHealth;

    // True for a few seconds after being hit: no repairs while under attack.
    public bool IsRepairLocked => RepairLockTimeLeft > 0f;
    public float RepairLockTimeLeft => Mathf.Max(0f, repairLockSeconds - (Time.time - lastDamageTime));

    public abstract string DisplayName { get; }

    // Can enemies attack it right now?
    public abstract bool IsTargetable { get; }

    // Can the player repair it right now?
    public virtual bool CanBeRepaired => Health < maxHealth;

    Collider body;

    protected virtual void Awake()
    {
        Health = maxHealth;
        body = GetComponent<Collider>();
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    // The point on the structure's collider nearest to a position.
    // Used to tell when an enemy (or the player) is touching the wall.
    public Vector3 ClosestPoint(Vector3 position) => body.ClosestPoint(position);

    // The box the structure takes up in the world (used to stop buildings overlapping).
    public Bounds Bounds => body.bounds;

    public void TakeDamage(float amount)
    {
        if (!IsTargetable)
            return;

        lastDamageTime = Time.time;
        Health = Mathf.Max(0f, Health - amount);
        if (Health <= 0f)
            OnHealthDepleted();
    }

    public void Repair(float amount)
    {
        if (!CanBeRepaired || IsRepairLocked)
            return;

        Health = Mathf.Min(maxHealth, Health + amount);
        if (Health >= maxHealth)
            OnFullyRepaired();
    }

    protected abstract void OnHealthDepleted();
    protected virtual void OnFullyRepaired() { }
}
