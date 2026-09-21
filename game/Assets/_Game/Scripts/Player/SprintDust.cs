using UnityEngine;

// Kicks up dust from the hero's feet while they sprint.
//
// Unlike the hit and death effects, this particle system is not spawned and
// thrown away: it lives on the Player the whole game and is simply switched on
// and off. That is the difference between a *burst* effect and a *looping* one.
//
// The emission is set to Rate over Distance in the prefab, not Rate over Time,
// so dust is produced per metre travelled. Sprint into a wall and you stop
// moving, so the dust stops by itself -- no code needed for that.
[RequireComponent(typeof(ParticleSystem))]
public class SprintDust : MonoBehaviour
{
    ParticleSystem dust;
    PlayerStamina stamina;
    bool emitting;

    void Awake()
    {
        dust = GetComponent<ParticleSystem>();
        stamina = GetComponentInParent<PlayerStamina>();

        SetEmitting(false); // no dust until the first sprint
    }

    void Update()
    {
        if (stamina == null)
            return;

        // Only touch the particle system when the answer actually changes,
        // rather than assigning the same value 60 times a second.
        bool wanted = stamina.IsSprinting;
        if (wanted != emitting)
            SetEmitting(wanted);
    }

    void SetEmitting(bool on)
    {
        emitting = on;

        // Turning the emission module off stops new dust but lets the puffs
        // already in the air finish fading, which is what stopping the system
        // outright would not do.
        ParticleSystem.EmissionModule emission = dust.emission;
        emission.enabled = on;
    }
}
