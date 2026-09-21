using UnityEngine;

// The Brute's special attack. When the player or a structure is in reach, it stops,
// a red circle grows on the ground under it (the "telegraph", so you can dodge),
// then it slams: big damage to everything still inside the circle.
[RequireComponent(typeof(Enemy))]
public class BruteAttack : MonoBehaviour
{
    [Tooltip("Starts a slam when the player or a structure is this close.")]
    [SerializeField] float attackRange = 3f;
    [SerializeField] float slamRadius = 3.5f;

    [Tooltip("Seconds the warning circle grows before the slam lands.")]
    [SerializeField] float windup = 1.2f;
    [SerializeField] float cooldown = 2.5f;

    [SerializeField] float playerDamage = 40f;
    [SerializeField] float structureDamage = 120f;

    [Header("Warning circle")]
    [Tooltip("A flat disc 1 m wide (child of the Brute) that shows where the slam will hit.")]
    [SerializeField] Transform indicator;
    [SerializeField] Color warningColor = new Color(1f, 0.15f, 0.1f, 0.3f);
    [SerializeField] Color slamColor = new Color(1f, 0.6f, 0.2f, 0.75f);
    [SerializeField] float flashTime = 0.15f;

    Enemy enemy;
    PlayerHealth player;
    Renderer indicatorRenderer;
    MaterialPropertyBlock block;

    bool windingUp;
    float windupTimer;
    float cooldownLeft;
    float flashLeft;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        player = FindAnyObjectByType<PlayerHealth>();
        indicatorRenderer = indicator.GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        indicator.gameObject.SetActive(false);
    }

    void Update()
    {
        if (windingUp)
        {
            windupTimer += Time.deltaTime;
            ShowCircle(windupTimer / windup, warningColor);
            if (windupTimer >= windup)
                Slam();
            return;
        }

        // Brief bright flash after a slam, then hide the circle.
        if (flashLeft > 0f)
        {
            flashLeft -= Time.deltaTime;
            if (flashLeft <= 0f)
                indicator.gameObject.SetActive(false);
        }

        cooldownLeft -= Time.deltaTime;
        if (cooldownLeft <= 0f && IsTargetInReach())
        {
            windingUp = true;
            windupTimer = 0f;
            enemy.MovementLocked = true; // stand still while winding up
            indicator.gameObject.SetActive(true);
        }
    }

    void Slam()
    {
        windingUp = false;
        cooldownLeft = cooldown;
        enemy.MovementLocked = false;
        ShowCircle(1f, slamColor);
        flashLeft = flashTime;

        if (player != null && !player.IsDead && FlatDistance(player.transform.position, transform.position) < slamRadius)
        {
            player.TakeDamage(playerDamage);
            // Only when it actually catches you: a slam across the map should
            // not rattle your screen.
            CameraFollow.Shake(0.5f, 0.35f);
        }

        // Copy the list first, in case the damage changes it while we're looping.
        foreach (Structure s in new System.Collections.Generic.List<Structure>(Structure.All))
            if (s.IsTargetable && FlatDistance(s.ClosestPoint(transform.position), transform.position) < slamRadius)
                s.TakeDamage(structureDamage);
    }

    bool IsTargetInReach()
    {
        if (player != null && !player.IsDead && FlatDistance(player.transform.position, transform.position) < attackRange)
            return true;
        // Indexed, not foreach: this runs every frame, and foreach over an
        // IReadOnlyList allocates an enumerator every call.
        System.Collections.Generic.IReadOnlyList<Structure> structures = Structure.All;
        for (int i = 0; i < structures.Count; i++)
            if (structures[i].IsTargetable
                && FlatDistance(structures[i].ClosestPoint(transform.position), transform.position) < attackRange)
                return true;
        return false;
    }

    // Size the disc to "fraction" of the full slam circle. The Brute's body is scaled up,
    // so we divide by its scale to get the right size in the world.
    void ShowCircle(float fraction, Color color)
    {
        Vector3 parentScale = transform.lossyScale;
        float diameter = slamRadius * 2f * Mathf.Clamp01(fraction);
        indicator.localScale = new Vector3(diameter / parentScale.x, 0.01f / parentScale.y, diameter / parentScale.z);

        // Keep it flat on the ground under the Brute.
        indicator.position = new Vector3(transform.position.x, 0.06f, transform.position.z);
        indicator.rotation = Quaternion.identity;

        block.SetColor("_BaseColor", color);
        indicatorRenderer.SetPropertyBlock(block);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
