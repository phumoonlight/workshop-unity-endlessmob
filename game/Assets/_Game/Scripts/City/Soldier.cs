using UnityEngine;

// A hired soldier. Stands at its post; when enemies come near the post it
// walks up to the closest one and hits it. Enemies touching it hurt it.
public class Soldier : MonoBehaviour
{
    [SerializeField] float maxHealth = 60f;
    [SerializeField] float moveSpeed = 4.5f;

    [SerializeField] float attackDamage = 8f;
    [SerializeField] float attackInterval = 0.8f;
    [SerializeField] float attackRange = 1.6f;

    [Tooltip("Soldiers go after enemies that come this close to their post.")]
    [SerializeField] float guardRange = 12f;

    [Tooltip("Damage per second from each enemy touching the soldier.")]
    [SerializeField] float damageFromEachEnemy = 10f;
    [SerializeField] float contactRange = 1.1f;

    float health;
    float cooldown;
    Vector3 post;
    readonly Collider[] hits = new Collider[64];

    public void Init(Vector3 guardPost)
    {
        post = guardPost;
    }

    void Awake()
    {
        health = maxHealth;
        post = transform.position;
    }

    void Update()
    {
        cooldown -= Time.deltaTime;

        Enemy target = FindNearestEnemy(post, guardRange);
        if (target != null)
        {
            if (FlatDistance(transform.position, target.transform.position) > attackRange)
                MoveTowards(target.transform.position);
            else if (cooldown <= 0f)
            {
                cooldown = attackInterval;
                target.TakeDamage(attackDamage);
            }
            Face(target.transform.position);
        }
        else if (FlatDistance(transform.position, post) > 0.3f)
        {
            MoveTowards(post);
            Face(post);
        }

        TakeContactDamage();
    }

    void TakeContactDamage()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, contactRange, hits, Layers.EnemyMask);
        int touching = 0;
        for (int i = 0; i < count; i++)
            if (hits[i].TryGetComponent(out Enemy _))
                touching++;

        health -= touching * damageFromEachEnemy * Time.deltaTime;
        if (health <= 0f)
            Destroy(gameObject);
    }

    Enemy FindNearestEnemy(Vector3 center, float range)
    {
        int count = Physics.OverlapSphereNonAlloc(center, range, hits, Layers.EnemyMask);
        Enemy nearest = null;
        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (!hits[i].TryGetComponent(out Enemy enemy))
                continue;
            float d = FlatDistance(transform.position, enemy.transform.position);
            if (d < best)
            {
                best = d;
                nearest = enemy;
            }
        }
        return nearest;
    }

    void MoveTowards(Vector3 destination)
    {
        destination.y = transform.position.y; // stay at our own height
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
