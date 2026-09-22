using UnityEngine;

// Sits on the enemy's character model, next to its Animator, and passes the
// swing clip's Animation Event up to the Enemy script on the parent.
//
// Unity delivers an Animation Event to the GameObject that owns the Animator
// (the "Model" child), while the Enemy that does the damage lives on the root
// above it. Same little hop as AnimationEventRelay on the hero.
public class EnemySwingRelay : MonoBehaviour
{
    Enemy enemy;

    void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
    }

    // Called by the AttackImpact event on the swing clip. The name here and the
    // Function field on the event have to match exactly.
    public void AttackImpact()
    {
        if (enemy != null)
            enemy.SwingLanded();
    }
}
