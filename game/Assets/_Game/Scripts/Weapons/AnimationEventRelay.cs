using UnityEngine;

// Sits on the character model, next to its Animator, and passes the model's
// Animation Events up to the weapon on the Player.
//
// This little hop is needed because Unity delivers an Animation Event to the
// GameObject that owns the Animator -- here ModelSword or ModelBow -- while all
// the gameplay lives on the Player above it.
public class AnimationEventRelay : MonoBehaviour
{
    Weapon[] weapons;

    void Awake()
    {
        // "true" includes weapons on disabled objects, and picks up both the
        // sword and the bow; only the one for the chosen class is enabled.
        weapons = GetComponentsInParent<Weapon>(true);
        if (Weapon.TraceImpacts) Debug.Log($"[swing] relay awake on {name}, found {weapons.Length} weapons");
    }

    // Called by an Animation Event on the attack clips. The name here and the
    // Function field on the event have to match exactly.
    public void AttackImpact()
    {
        if (Weapon.TraceImpacts) Debug.Log($"[swing] relay AttackImpact on {name}");
        foreach (Weapon weapon in weapons)
            if (weapon.enabled)
                weapon.AnimationImpact();
    }
}
