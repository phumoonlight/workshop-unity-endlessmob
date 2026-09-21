using UnityEngine;

// A bullet that flies straight forward and damages the first enemy it touches.
public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 18f;
    [SerializeField] float damage = 10f;

    [Tooltip("Seconds before the bullet disappears if it hits nothing.")]
    [SerializeField] float lifetime = 2f;

    bool hasHit;

    // Called by the weapon right after spawning, so upgrades can boost the arrow.
    public void Launch(float damageMultiplier, float speedMultiplier = 1f)
    {
        damage *= damageMultiplier;
        speed *= speedMultiplier;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    // Called by Unity when our trigger collider overlaps another collider.
    void OnTriggerEnter(Collider other)
    {
        if (hasHit || !other.TryGetComponent(out Enemy enemy))
            return;

        hasHit = true; // stops one bullet hitting two enemies in the same frame
        enemy.TakeDamage(damage);
        Destroy(gameObject);
    }
}
