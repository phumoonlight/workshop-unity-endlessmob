using UnityEngine;

// Freezes the game for a few hundredths of a second when a blow lands.
//
// It is the cheapest trick in action games: stopping everything for 50ms makes
// a hit feel like it had weight, because the eye reads the pause as the blade
// meeting something solid. Any longer and it just feels like lag.
//
// Nothing to place in the scene -- like GameAudio, it makes itself.
public class HitStop : MonoBehaviour
{
    // Not a true 0. This game already uses "timeScale == 0" to mean paused
    // (choosing a class, the level-up popup, game over), and several scripts
    // check that to hide their prompts. Crawling at 5% reads the same to the
    // eye without pretending to be a pause.
    const float FrozenScale = 0.05f;

    static HitStop instance;
    float frozenUntil;   // unscaled, so the freeze itself doesn't delay the end of it

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateIfMissing()
    {
        if (instance != null)
            return;
        instance = new GameObject("HitStop").AddComponent<HitStop>();
        DontDestroyOnLoad(instance.gameObject);
    }

    public static void Freeze(float seconds)
    {
        if (instance == null || seconds <= 0f)
            return;
        instance.Begin(seconds);
    }

    void Begin(float seconds)
    {
        // Don't touch a real pause. If the game is already stopped, or running
        // at some speed we didn't set, leave it alone.
        if (!Mathf.Approximately(Time.timeScale, 1f) && !IsFrozen)
            return;

        Time.timeScale = FrozenScale;
        // Overlapping hits extend the freeze rather than cutting it short.
        frozenUntil = Mathf.Max(frozenUntil, Time.unscaledTime + seconds);
    }

    bool IsFrozen => Mathf.Approximately(Time.timeScale, FrozenScale);

    void Update()
    {
        if (frozenUntil <= 0f || Time.unscaledTime < frozenUntil)
            return;

        frozenUntil = 0f;

        // Only hand time back if it is still ours. Something else may have
        // paused the game properly during the freeze -- a level-up popup, the
        // castle falling -- and that must win.
        if (IsFrozen)
            Time.timeScale = 1f;
    }
}
