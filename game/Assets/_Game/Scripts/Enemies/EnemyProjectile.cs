using UnityEngine;

// An enemy arrow. Flies straight and damages the player or a structure it hits.
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] float speed = 14f;
    [SerializeField] float lifetime = 3f;

    float damage = 8f;
    bool hasHit;

    public void Launch(float damageAmount)
    {
        damage = damageAmount;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit)
            return;

        if (other.TryGetComponent(out PlayerHealth player))
        {
            hasHit = true;
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.TryGetComponent(out Structure structure))
        {
            hasHit = true;
            structure.TakeDamage(damage);
            Destroy(gameObject);
        }
        // Anything else (other enemies, the ground, pickups) is ignored.
    }
}
