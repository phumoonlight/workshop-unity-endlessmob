using System;
using TMPro;
using UnityEngine;

// The capture phase of a camp. After its troops are wiped out, a capture circle
// appears, and the gauge fills while the hero stands inside it:
// - hero inside, no enemies:    fills at full speed
// - hero inside, enemies too:   fills, but slower
// - hero outside:               stays where it is -- progress is never lost
// At 100% the camp is captured.
//
// (It used to be a tug of war: enemies could drain the gauge and retake the
// camp, and groups spawned to fight you for it. That is all in git history.)
public class CampCapture : MonoBehaviour
{
    [Tooltip("Radius of the circle, in metres. The disc on the ground is resized to match.")]
    [SerializeField] float radius = 6f;

    [Tooltip("Seconds of standing in an empty circle to capture.")]
    [SerializeField] float captureSeconds = 10f;

    [Tooltip("How fast the gauge fills while enemies share the circle with you. 0.35 = about a third of normal speed.")]
    [Range(0f, 1f)]
    [SerializeField] float speedWithEnemiesInside = 0.35f;

    [Header("Visuals")]
    [SerializeField] Renderer areaRenderer;  // faint disc showing the circle
    [SerializeField] Renderer fillRenderer;  // disc that grows with the gauge
    [SerializeField] TextMeshPro label;
    [SerializeField] Color idleColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] Color capturingColor = new Color(0.3f, 1f, 0.4f, 0.4f);
    [SerializeField] Color slowedColor = new Color(1f, 0.8f, 0.2f, 0.4f);

    public bool IsActive { get; private set; }
    public bool IsCaptured { get; private set; }
    public float Progress { get; private set; } // 0 .. 1

    public event Action Captured;

    PlayerHealth player;
    Camera cam;
    MaterialPropertyBlock block;
    readonly Collider[] hits = new Collider[128];

    void Awake()
    {
        player = FindAnyObjectByType<PlayerHealth>();
        cam = Camera.main;
        block = new MaterialPropertyBlock();

        // Size the disc from the radius, so changing the radius in the Inspector
        // can't leave the drawn circle a different size from the real one.
        areaRenderer.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

        SetVisualsActive(false);
    }

    // Called by the camp when its last troop dies.
    public void Begin()
    {
        IsActive = true;
        Progress = 0f;
        SetVisualsActive(true);
    }

    void Update()
    {
        if (!IsActive || IsCaptured)
            return;

        bool playerInside = !player.IsDead && FlatDistance(player.transform.position, transform.position) < radius;
        bool enemyInside = playerInside && IsAnyEnemyInside(); // only matters while you're in

        // Only ever goes up. Nobody inside, or only enemies: it simply waits.
        if (playerInside)
        {
            float speed = enemyInside ? speedWithEnemiesInside : 1f;
            Progress = Mathf.Min(1f, Progress + speed * Time.deltaTime / captureSeconds);
        }

        UpdateVisuals(playerInside, enemyInside);

        if (Progress >= 1f)
        {
            IsCaptured = true;
            SetVisualsActive(false);
            Captured?.Invoke();
        }
    }

    bool IsAnyEnemyInside()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, hits, Layers.EnemyMask);
        for (int i = 0; i < count; i++)
            if (hits[i].TryGetComponent(out Enemy enemy) && FlatDistance(enemy.transform.position, transform.position) < radius)
                return true;
        return false;
    }

    void UpdateVisuals(bool playerInside, bool enemyInside)
    {
        block.SetColor("_BaseColor", idleColor);
        areaRenderer.SetPropertyBlock(block);

        // The fill disc grows with the gauge.
        float size = radius * 2f * Progress;
        fillRenderer.transform.localScale = new Vector3(size, 0.01f, size);
        block.SetColor("_BaseColor", enemyInside ? slowedColor : capturingColor);
        fillRenderer.SetPropertyBlock(block);

        string gauge = $"<color=#80ff80>{Mathf.FloorToInt(Progress * 100f)}%</color>";

        if (playerInside && enemyInside)
            label.text = $"<color=#ffd24a>SLOWED</color>  {gauge}\n<size=60%>Clear the enemies out</size>";
        else if (playerInside)
            label.text = $"<color=#80ff80>CAPTURING</color>  {gauge}";
        else
            label.text = $"CAPTURE  {gauge}\n<size=60%>Stand in the circle</size>";
    }

    void LateUpdate()
    {
        if (label.gameObject.activeSelf)
            label.transform.rotation = cam.transform.rotation; // face the camera
    }

    void SetVisualsActive(bool active)
    {
        areaRenderer.gameObject.SetActive(active);
        fillRenderer.gameObject.SetActive(active);
        label.gameObject.SetActive(active);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
