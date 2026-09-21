using UnityEngine;

// The arc drawn on the ground when the sword slashes.
//
// It isn't a picture or a canned animation: the shape is a crescent band built
// out of triangles in code, and it's rebuilt every frame so the slash *sweeps*.
// A "head" races around the arc like the blade tip, a "tail" chases it, and the
// band between them is what you see, thinning out toward the tail like a trail.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SlashEffect : MonoBehaviour
{
    [Tooltip("Seconds the whole effect lasts, from the blade starting to the trail vanishing.")]
    [SerializeField] float duration = 0.24f;

    [Tooltip("How much of that time the blade tip takes to cross the arc. 0.35 = the sweep is done a third of the way in, then the trail catches up.")]
    [Range(0.05f, 1f)]
    [SerializeField] float sweepTime = 0.35f;

    [Tooltip("How long the trail hangs back before it starts catching up. Bigger = a longer streak.")]
    [Range(0f, 0.9f)]
    [SerializeField] float trailLag = 0.3f;

    [Tooltip("Inner edge of the crescent, as a fraction of the slash reach. 1 would be a hairline, 0 a full pie slice.")]
    [Range(0f, 0.95f)]
    [SerializeField] float innerRadius = 0.55f;

    [Tooltip("How thin the band gets at the tail end. 0.2 = a fifth as thick as at the blade tip.")]
    [Range(0.05f, 1f)]
    [SerializeField] float tailThinness = 0.2f;

    [SerializeField] Color color = new Color(1f, 1f, 1f, 0.6f);

    const int Segments = 28; // how smooth the curve is

    MeshRenderer meshRenderer;
    MaterialPropertyBlock block;
    Mesh mesh;
    Vector3[] vertices;
    float age;
    float radius;
    float totalAngle;

    // tint: optional different color (e.g. gold for a combo's final strike).
    public void Play(float slashRadius, float angleDegrees, Color? tint = null)
    {
        if (tint.HasValue)
            color = tint.Value;

        radius = slashRadius;
        totalAngle = angleDegrees;

        meshRenderer = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();

        // Build the mesh once with a fixed number of triangles. Only the corner
        // positions change as it sweeps, which is far cheaper than rebuilding it.
        vertices = new Vector3[(Segments + 1) * 2];
        mesh = new Mesh { vertices = vertices, triangles = BuildStripTriangles() };
        GetComponent<MeshFilter>().mesh = mesh;

        Reshape(0f, 0f);
        SetAlpha(color.a);
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / duration; // 0 at the start, 1 at the end

        // The blade tip races ahead and eases to a stop at the end of the arc.
        float head = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / sweepTime));

        // The tail waits, then catches up, closing the streak as it goes.
        float tail = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - trailLag) / (1f - trailLag)));

        Reshape(tail, head);

        // Hold full strength while the blade is moving, then fade the streak out.
        float fade = Mathf.Clamp01((1f - t) / 0.55f);
        SetAlpha(color.a * fade);

        if (t >= 1f)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Meshes made in code are not cleaned up automatically.
        Destroy(mesh);
    }

    // Moves the band's corners to cover the arc between "tail" and "head"
    // (both 0 = the start of the swing, 1 = the end).
    void Reshape(float tail, float head)
    {
        float start = -totalAngle / 2f;

        for (int i = 0; i <= Segments; i++)
        {
            float along = (float)i / Segments; // 0 at the tail, 1 at the blade tip
            float angle = (start + totalAngle * Mathf.Lerp(tail, head, along)) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));

            // Thick at the tip, thin at the tail, so it reads as a trail.
            float thickness = Mathf.Lerp(tailThinness, 1f, along);
            float inner = Mathf.Lerp(radius, radius * innerRadius, thickness);

            vertices[i * 2] = direction * inner;
            vertices[i * 2 + 1] = direction * radius;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds(); // else it can be culled when the shape moves
    }

    // Two triangles per segment, stitching the inner and outer edges together.
    static int[] BuildStripTriangles()
    {
        var triangles = new int[Segments * 6];
        for (int i = 0; i < Segments; i++)
        {
            int v = i * 2;
            triangles[i * 6] = v;
            triangles[i * 6 + 1] = v + 1;
            triangles[i * 6 + 2] = v + 2;

            triangles[i * 6 + 3] = v + 2;
            triangles[i * 6 + 4] = v + 1;
            triangles[i * 6 + 5] = v + 3;
        }
        return triangles;
    }

    void SetAlpha(float alpha)
    {
        if (block == null)
            return;
        block.SetColor("_BaseColor", new Color(color.r, color.g, color.b, alpha));
        meshRenderer.SetPropertyBlock(block);
    }
}
