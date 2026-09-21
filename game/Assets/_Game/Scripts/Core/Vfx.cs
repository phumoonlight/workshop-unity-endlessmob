using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// Short visual effects: the spark when something is hit, the puff when an enemy
// dies, the sparkle when a pickup is collected.
//
// Each effect is a prefab in Assets/_Game/Resources/Vfx/ named after the Kind
// value, so calling it needs no wiring:
//
//     Vfx.Play(Vfx.Kind.Hit, position);
//
// The effects are *pooled*. A busy fight hits enemies dozens of times a second,
// and creating then destroying a GameObject for each one makes rubbish that the
// garbage collector has to clear up later, which is felt as a hitch. Instead a
// handful of effect objects are made once and reused for the rest of the game:
// used ones are switched off and handed back, not thrown away.
public static class Vfx
{
    // Add a name here and a prefab called the same thing in Resources/Vfx.
    public enum Kind { Hit, Death, Pickup }

    const string Folder = "Vfx/";

    static readonly Dictionary<Kind, GameObject> prefabs = new Dictionary<Kind, GameObject>();
    static readonly Dictionary<Kind, ObjectPool<PooledVfx>> pools = new Dictionary<Kind, ObjectPool<PooledVfx>>();

    // Static fields survive pressing Play again in the editor, but the pooled
    // objects do not, so the pool would hand out effects Unity had destroyed.
    // This runs before every play session and starts the pools empty.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPools()
    {
        prefabs.Clear();
        pools.Clear();
    }

    // "scale" makes the same effect bigger for bigger things: the Brute's death
    // puff should not be the size of a rat's.
    public static void Play(Kind kind, Vector3 position, float scale = 1f)
    {
        ObjectPool<PooledVfx> pool = PoolFor(kind);
        if (pool == null)
            return; // no prefab for this effect yet: do nothing rather than break

        PooledVfx effect = pool.Get();
        if (effect == null)
        {
            // A leftover from a previous play session: throw the whole pool away
            // and build a fresh one rather than handing out dead objects.
            pools.Remove(kind);
            pool = PoolFor(kind);
            effect = pool != null ? pool.Get() : null;
            if (effect == null)
                return;
        }

        effect.transform.SetPositionAndRotation(position, Quaternion.identity);
        effect.transform.localScale = Vector3.one * scale;
        effect.System.Play();
    }

    static ObjectPool<PooledVfx> PoolFor(Kind kind)
    {
        if (pools.TryGetValue(kind, out ObjectPool<PooledVfx> pool))
            return pool;

        if (!prefabs.TryGetValue(kind, out GameObject prefab))
        {
            prefab = Resources.Load<GameObject>(Folder + kind);
            prefabs[kind] = prefab;
        }
        if (prefab == null)
        {
            pools[kind] = null;
            return null;
        }

        // The four callbacks are the whole of pooling: how to make one, what to
        // do when one is taken out, what to do when one comes back, and what to
        // do if the pool overflows and a spare really must be destroyed.
        pool = new ObjectPool<PooledVfx>(
            createFunc: () => Create(prefab, kind),
            actionOnGet: e => e.gameObject.SetActive(true),
            actionOnRelease: e => e.gameObject.SetActive(false),
            // When you press Stop, Unity empties every pool AFTER it has already
            // destroyed the scene's objects, so the effect may be gone by the
            // time this runs. Unity's "== null" is true for destroyed objects.
            actionOnDestroy: e => { if (e != null) Object.Destroy(e.gameObject); },
            collectionCheck: false, // skip the "released twice" check: it costs time
            defaultCapacity: 8,
            maxSize: 64);

        pools[kind] = pool;
        return pool;
    }

    static PooledVfx Create(GameObject prefab, Kind kind)
    {
        GameObject spawned = Object.Instantiate(prefab);
        // Kept alive across scene changes, so the pool never hands out an object
        // that Unity destroyed on the way back to the main menu.
        Object.DontDestroyOnLoad(spawned);

        PooledVfx effect = spawned.GetComponent<PooledVfx>();
        if (effect == null)
            effect = spawned.AddComponent<PooledVfx>();

        // When its last particle dies, the effect puts itself back in the pool.
        ObjectPool<PooledVfx> owner = pools[kind];
        effect.Finished += e => owner.Release(e);
        return effect;
    }
}
