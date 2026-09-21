using TMPro;
using UnityEngine;

// Trains one Builder who walks around repairing structures near the hut.
// If the builder dies, a new one is trained after a delay; floating text
// above the hut shows the countdown.
public class BuilderHut : MonoBehaviour
{
    [SerializeField] Builder builderPrefab;

    [Tooltip("Builders only repair structures within this distance of the hut.")]
    [SerializeField] float range = 12f;

    [Tooltip("Seconds to train the first builder after the hut is built.")]
    [SerializeField] float firstBuilderDelay = 3f;

    [Tooltip("Seconds to train a new builder after the last one died.")]
    [SerializeField] float respawnDelay = 10f;

    [Tooltip("Floating text above the hut showing the training countdown.")]
    [SerializeField] TextMeshPro indicator;

    Builder builder;
    float trainTimeLeft;
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        trainTimeLeft = firstBuilderDelay;
    }

    void Update()
    {
        // Builder died (destroyed objects compare equal to null)? Count down to a new one.
        if (builder == null)
        {
            trainTimeLeft -= Time.deltaTime;
            if (trainTimeLeft <= 0f)
            {
                SpawnBuilder();
                trainTimeLeft = respawnDelay; // for next time
            }
        }

        if (indicator != null)
        {
            bool training = builder == null;
            indicator.gameObject.SetActive(training);
            if (training)
                indicator.text = $"Builder in {Mathf.CeilToInt(trainTimeLeft)}s";
        }
    }

    // Hide the countdown while the hut is ruined (this component is switched off then).
    void OnDisable()
    {
        if (indicator != null)
            indicator.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        // "Billboard": turn the text to face the camera so it's always readable.
        if (indicator != null && indicator.gameObject.activeSelf)
            indicator.transform.rotation = cam.transform.rotation;
    }

    void SpawnBuilder()
    {
        Vector3 spawn = transform.position + transform.forward * -1.8f; // in front of the door
        spawn.y = builderPrefab.transform.position.y;
        builder = Instantiate(builderPrefab, spawn, transform.rotation);
        builder.Init(this);
    }

    // Where the builder waits when there's nothing to fix.
    public Vector3 RestPoint => transform.position + transform.forward * -1.8f;

    // Is this structure damaged, not under attack, and within the hut's range?
    public bool CanRepairNow(Structure s)
    {
        if (s == null || !s.CanBeRepaired || s.IsRepairLocked)
            return false;

        Vector3 toWall = s.ClosestPoint(transform.position) - transform.position;
        toWall.y = 0f;
        return toWall.sqrMagnitude <= range * range;
    }

    // The structure in range with the lowest health percentage that can be repaired right now.
    public Structure FindMostDamaged()
    {
        Structure best = null;
        float lowestFraction = 1f;

        foreach (Structure s in Structure.All)
        {
            if (!CanRepairNow(s))
                continue;

            float fraction = s.Health / s.MaxHealth;
            if (fraction < lowestFraction)
            {
                lowestFraction = fraction;
                best = s;
            }
        }
        return best;
    }
}
