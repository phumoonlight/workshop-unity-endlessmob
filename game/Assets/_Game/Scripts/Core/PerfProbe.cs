using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

// A tiny performance recorder you can read afterwards.
//
// The Profiler window shows all this live, but it cannot be read by anyone who
// is not sitting in front of it. This samples the same counters Unity feeds the
// Profiler, then writes a plain summary to Logs/PerfReport.txt when you stop.
//
// Press F9 to start recording, F9 again to stop and write the report.
//
// Editor and development builds only, and it makes itself, so there is nothing
// to place in the scene and nothing shipped to players.
public class PerfProbe : MonoBehaviour
{
    static PerfProbe instance;

    ProfilerRecorder mainThread;    // how long the whole frame took on the CPU
    ProfilerRecorder gcAlloc;       // memory created this frame -- what pooling fixes
    ProfilerRecorder drawCalls;
    ProfilerRecorder triangles;

    bool recording;
    float startedAt;
    readonly List<float> frameMs = new List<float>();
    long gcTotal;
    long gcWorstFrame;
    long drawCallsPeak;
    long trianglesPeak;
    int enemiesPeak;
    int particleSystemsPeak;
    float nextCountAt;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateIfMissing()
    {
        if (instance != null)
            return;
        instance = new GameObject("PerfProbe").AddComponent<PerfProbe>();
        DontDestroyOnLoad(instance.gameObject);
    }
#endif

    void OnEnable()
    {
        // These are the same counters the Profiler window draws.
        mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
        gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
    }

    void OnDisable()
    {
        mainThread.Dispose();
        gcAlloc.Dispose();
        drawCalls.Dispose();
        triangles.Dispose();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            if (recording) StopAndWrite();
            else Begin();
        }

        if (!recording)
            return;

        // Main Thread is reported in nanoseconds.
        if (mainThread.Valid)
            frameMs.Add(mainThread.LastValue * 1e-6f);

        if (gcAlloc.Valid)
        {
            long bytes = gcAlloc.LastValue;
            gcTotal += bytes;
            if (bytes > gcWorstFrame) gcWorstFrame = bytes;
        }

        if (drawCalls.Valid && drawCalls.LastValue > drawCallsPeak) drawCallsPeak = drawCalls.LastValue;
        if (triangles.Valid && triangles.LastValue > trianglesPeak) trianglesPeak = triangles.LastValue;

        // Counting objects is too slow to do every frame, so once a second.
        if (Time.unscaledTime >= nextCountAt)
        {
            nextCountAt = Time.unscaledTime + 1f;
            int enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
            int systems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;
            if (enemies > enemiesPeak) enemiesPeak = enemies;
            if (systems > particleSystemsPeak) particleSystemsPeak = systems;
        }
    }

    // Deliberately not called Start: that is a Unity message, and Unity would
    // call it by itself on the first frame.
    void Begin()
    {
        recording = true;
        startedAt = Time.unscaledTime;
        nextCountAt = 0f;
        frameMs.Clear();
        gcTotal = 0;
        gcWorstFrame = 0;
        drawCallsPeak = 0;
        trianglesPeak = 0;
        enemiesPeak = 0;
        particleSystemsPeak = 0;
        Debug.Log("PerfProbe: recording. Press F9 again to stop and write the report.");
    }

    void StopAndWrite()
    {
        recording = false;
        float seconds = Time.unscaledTime - startedAt;
        if (frameMs.Count == 0)
        {
            Debug.LogWarning("PerfProbe: no frames recorded.");
            return;
        }

        // Sorting lets us read the typical frame and the bad ones separately.
        // An average alone hides stutter: a few 60ms frames barely move it.
        List<float> sorted = new List<float>(frameMs);
        sorted.Sort();
        float median = sorted[sorted.Count / 2];
        float p95 = sorted[Mathf.Min(sorted.Count - 1, Mathf.FloorToInt(sorted.Count * 0.95f))];
        float p99 = sorted[Mathf.Min(sorted.Count - 1, Mathf.FloorToInt(sorted.Count * 0.99f))];
        float worst = sorted[sorted.Count - 1];

        float total = 0f;
        int over16 = 0, over33 = 0;
        for (int i = 0; i < frameMs.Count; i++)
        {
            total += frameMs[i];
            if (frameMs[i] > 16.7f) over16++;
            if (frameMs[i] > 33.3f) over33++;
        }
        float average = total / frameMs.Count;

        StringBuilder r = new StringBuilder();
        r.AppendLine("Game performance report");
        r.AppendLine("recorded " + seconds.ToString("0.0") + "s, " + frameMs.Count + " frames, in the "
            + (Application.isEditor ? "EDITOR (numbers are inflated)" : "a build"));
        r.AppendLine();
        r.AppendLine("CPU main thread, milliseconds per frame");
        r.AppendLine("  average " + average.ToString("0.00") + "   (" + (1000f / Mathf.Max(0.01f, average)).ToString("0") + " fps)");
        r.AppendLine("  median  " + median.ToString("0.00"));
        r.AppendLine("  95th    " + p95.ToString("0.00") + "   <- the stutter you feel");
        r.AppendLine("  99th    " + p99.ToString("0.00"));
        r.AppendLine("  worst   " + worst.ToString("0.00"));
        r.AppendLine("  frames over 16.7ms (60fps): " + over16 + " of " + frameMs.Count
            + " (" + (100f * over16 / frameMs.Count).ToString("0") + "%)");
        r.AppendLine("  frames over 33.3ms (30fps): " + over33 + " of " + frameMs.Count
            + " (" + (100f * over33 / frameMs.Count).ToString("0") + "%)");
        r.AppendLine();
        r.AppendLine("Garbage collection -- this is what object pooling fixes");
        r.AppendLine("  allocated per frame, average: " + (gcTotal / (float)frameMs.Count / 1024f).ToString("0.0") + " KB");
        r.AppendLine("  worst single frame:           " + (gcWorstFrame / 1024f).ToString("0.0") + " KB");
        r.AppendLine("  total over the recording:     " + (gcTotal / 1048576f).ToString("0.0") + " MB");
        r.AppendLine();
        r.AppendLine("Rendering and scene");
        r.AppendLine("  draw calls, peak: " + drawCallsPeak);
        r.AppendLine("  triangles, peak:  " + trianglesPeak);
        r.AppendLine("  enemies alive, peak:     " + enemiesPeak);
        r.AppendLine("  particle systems, peak:  " + particleSystemsPeak);

        string folder = System.IO.Path.Combine(Application.dataPath, "../Logs");
        System.IO.Directory.CreateDirectory(folder);
        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(folder, "PerfReport.txt"));
        System.IO.File.WriteAllText(path, r.ToString());

        Debug.Log("PerfProbe: written to " + path + "\n" + r.ToString());
    }
}
