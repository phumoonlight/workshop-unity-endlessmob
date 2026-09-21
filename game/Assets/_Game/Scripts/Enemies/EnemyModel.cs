using UnityEngine;

// Shows an animated character in place of the enemy's plain box, and keeps its
// stand/walk/run animation in step with how fast the enemy is really moving.
//
// Same idea as PlayerModel: the model is only what you see. The gameplay still
// runs on the box's Rigidbody and collider, so nothing about how enemies chase,
// hit or die has changed.
[RequireComponent(typeof(Rigidbody))]
public class EnemyModel : MonoBehaviour
{
    [Tooltip("Animator on the character model under this enemy. Found automatically if left empty.")]
    [SerializeField] Animator animator;

    [Tooltip("How quickly the animation eases between standing, walking and running.")]
    [SerializeField] float blendSmoothing = 0.12f;

    Rigidbody body;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator == null)
            return;

        // Ask the Rigidbody how fast we actually moved rather than what speed we
        // asked for: an enemy shoving through a crowd moves slower than its
        // moveSpeed, and its legs should slow down with it.
        Vector3 velocity = body.linearVelocity;
        velocity.y = 0f; // ignore falling

        // The last two arguments ease the value instead of snapping it.
        animator.SetFloat("Speed", velocity.magnitude, blendSmoothing, Time.deltaTime);
    }
}
