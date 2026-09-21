using System;
using UnityEngine;

// Sits on every effect prefab and says "I have finished" so Vfx can take the
// object back instead of destroying it.
//
// OnParticleSystemStopped is called by Unity itself, but only when the particle
// system's Stop Action is set to Callback -- which is why the prefabs use that
// instead of Destroy now.
[RequireComponent(typeof(ParticleSystem))]
public class PooledVfx : MonoBehaviour
{
    public event Action<PooledVfx> Finished;

    ParticleSystem system;

    public ParticleSystem System
    {
        get
        {
            if (system == null)
                system = GetComponent<ParticleSystem>();
            return system;
        }
    }

    void OnParticleSystemStopped()
    {
        Finished?.Invoke(this);
    }
}
