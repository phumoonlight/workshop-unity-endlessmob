using System.Collections.Generic;
using UnityEngine;

// Reuses XP gems and coins instead of creating and destroying them.
//
// Almost every kill drops a gem, and every gem is collected a few seconds
// later, so this is the busiest create/destroy traffic in the game. A collected
// pickup is switched off and kept as a spare; the next drop switches a spare
// back on instead of building a new object.
//
// At heart a pool is only this: a stack of switched-off spares per prefab.
//
// Different from Vfx on purpose: those effects are kept alive across scenes,
// but a pickup belongs to its run. Gems left on the ground must vanish with
// the scene when the run ends -- and so do the spares. Unity's "== null" is
// true for destroyed objects, so Spawn just skips any spare that has died.
public static class PickupPool
{
    // One stack of spares for each prefab (gem, coin, big gem...).
    static readonly Dictionary<MagnetPickup, Stack<MagnetPickup>> spares = new Dictionary<MagnetPickup, Stack<MagnetPickup>>();

    // Statics survive pressing Play again in the editor: start each session empty.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPools() => spares.Clear();

    public static MagnetPickup Spawn(MagnetPickup prefab, Vector3 position)
    {
        if (!spares.TryGetValue(prefab, out Stack<MagnetPickup> stack))
        {
            stack = new Stack<MagnetPickup>();
            spares[prefab] = stack;
        }

        while (stack.Count > 0)
        {
            MagnetPickup spare = stack.Pop();
            if (spare == null)
                continue; // destroyed with the previous run's scene

            spare.transform.SetPositionAndRotation(position, Quaternion.identity);
            spare.gameObject.SetActive(true); // runs its OnEnable, which resets it
            return spare;
        }

        // No spare: make a new one, and note which stack it goes back to.
        MagnetPickup made = Object.Instantiate(prefab, position, Quaternion.identity);
        made.PoolPrefab = prefab;
        return made;
    }

    // Called by a pickup when it has been collected.
    public static void Release(MagnetPickup pickup)
    {
        // Not made by Spawn (placed in a scene by hand, say): nothing to return it to.
        if (pickup.PoolPrefab == null || !spares.TryGetValue(pickup.PoolPrefab, out Stack<MagnetPickup> stack))
        {
            Object.Destroy(pickup.gameObject);
            return;
        }

        pickup.gameObject.SetActive(false);
        stack.Push(pickup);
    }
}
