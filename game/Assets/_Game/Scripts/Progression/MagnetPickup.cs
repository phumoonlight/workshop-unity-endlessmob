using UnityEngine;

// Base for anything the player picks up by walking near it (XP gems, coins).
// Sits and spins until the player is within their pickup radius, then flies into them.
// Each kind of pickup decides what happens on collection by overriding Collect.
public abstract class MagnetPickup : MonoBehaviour
{
    [Tooltip("Starting speed when flying to the player. It speeds up so it always catches them.")]
    [SerializeField] float flySpeed = 8f;
    [SerializeField] float flyAcceleration = 40f;

    // Shared by all pickups. PlayerExperience knows the pickup radius.
    static PlayerExperience player;
    bool isFlying;

    void Awake()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerExperience>();
    }

    void Update()
    {
        if (player == null)
            return;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f; // only care about distance along the ground
        float distance = toPlayer.magnitude;

        if (!isFlying)
        {
            transform.Rotate(0f, 120f * Time.deltaTime, 0f); // idle spin
            if (distance < player.PickupRadius)
                isFlying = true;
            return;
        }

        flySpeed += flyAcceleration * Time.deltaTime;
        float step = flySpeed * Time.deltaTime;

        // Close enough this frame? Collect it.
        if (distance <= step || distance < 0.5f)
        {
            Collect(player.gameObject);
            Vfx.Play(Vfx.Kind.Pickup, transform.position);
            Destroy(gameObject);
            return;
        }

        transform.position += toPlayer / distance * step;
    }

    // "abstract" = every kind of pickup must write its own version of this.
    protected abstract void Collect(GameObject playerObject);
}
