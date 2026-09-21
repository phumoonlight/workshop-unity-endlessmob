using UnityEngine;
using UnityEngine.Rendering;

// Drains the colour out of the screen and closes in a red vignette as the
// hero's health drops.
//
// This is how post-processing and gameplay talk to each other. The look lives
// entirely in a Volume profile (LowHealth.asset) -- the script never touches a
// colour. All it does is turn the Volume's WEIGHT up and down:
//
//   weight 0 = this Volume does nothing, you see the normal Global Volume
//   weight 1 = this Volume's settings fully replace the normal ones
//   0.5      = halfway between the two
//
// Because Volumes blend, this one only has to say what is DIFFERENT when
// you're hurt. Everything it doesn't mention (bloom, tonemapping) carries on
// from the Global Volume underneath.
[RequireComponent(typeof(Volume))]
public class LowHealthEffect : MonoBehaviour
{
    [SerializeField] PlayerHealth health;

    [Tooltip("The effect starts creeping in below this fraction of health. 0.5 = at half health.")]
    [Range(0f, 1f)]
    [SerializeField] float startsAt = 0.5f;

    [Tooltip("How fast the effect eases in and out, so a heal or a hit doesn't make it snap.")]
    [SerializeField] float fadeSpeed = 3f;

    Volume volume;

    void Awake()
    {
        volume = GetComponent<Volume>();
        volume.weight = 0f;
    }

    void Update()
    {
        if (health == null)
            return;

        float fraction = health.MaxHealth > 0f ? health.Health / health.MaxHealth : 1f;

        // 0 at "startsAt" health, climbing to 1 at zero health.
        float target = Mathf.InverseLerp(startsAt, 0f, fraction);
        if (health.IsDead)
            target = 0f; // the respawn countdown should look normal, not bleed red

        // Unscaled, so it keeps easing during a hit-stop freeze.
        volume.weight = Mathf.MoveTowards(volume.weight, target, fadeSpeed * Time.unscaledDeltaTime);
    }
}
