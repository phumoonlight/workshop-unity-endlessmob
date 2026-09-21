using UnityEngine;

// A worker from a Builder Hut. Walks to the most damaged structure near the hut
// and repairs it; goes back to the hut when there's nothing to fix.
// Enemies touching the builder hurt it.
public class Builder : MonoBehaviour
{
    [SerializeField] float maxHealth = 40f;
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float repairPerSecond = 15f;

    [Tooltip("How close to the wall the builder must be to repair it.")]
    [SerializeField] float repairReach = 1.3f;

    [Tooltip("Damage per second from each enemy touching the builder.")]
    [SerializeField] float damageFromEachEnemy = 10f;
    [SerializeField] float contactRange = 1.0f;

    BuilderHut hut;
    Structure target; // the building currently being repaired
    float health;
    readonly Collider[] hits = new Collider[32];

    public void Init(BuilderHut home)
    {
        hut = home;
    }

    void Awake()
    {
        health = maxHealth;
    }

    void Update()
    {
        if (hut == null)
        {
            Destroy(gameObject); // the hut is gone
            return;
        }

        // Finish the current job before picking a new one. Only switch early if it can't be
        // repaired right now (fully fixed, under attack, destroyed or out of range).
        if (!hut.CanRepairNow(target))
            target = hut.FindMostDamaged();

        if (target != null)
        {
            Vector3 wall = target.ClosestPoint(transform.position);
            if (FlatDistance(transform.position, wall) > repairReach)
                MoveTowards(wall);
            else
                target.Repair(repairPerSecond * Time.deltaTime);
            Face(wall);
        }
        else if (FlatDistance(transform.position, hut.RestPoint) > 0.3f)
        {
            MoveTowards(hut.RestPoint);
            Face(hut.RestPoint);
        }

        TakeContactDamage();
    }

    void TakeContactDamage()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, contactRange, hits);
        int touching = 0;
        for (int i = 0; i < count; i++)
            if (hits[i].TryGetComponent(out Enemy _))
                touching++;

        health -= touching * damageFromEachEnemy * Time.deltaTime;
        if (health <= 0f)
            Destroy(gameObject); // the hut notices and starts training a new one
    }

    void MoveTowards(Vector3 destination)
    {
        destination.y = transform.position.y;
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
    }

    void Face(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
