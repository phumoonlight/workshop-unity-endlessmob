using System.Collections.Generic;
using UnityEngine;

// Every sound effect in the game.
//
// A sound is looked for as a real audio file first, in
// Assets/_Game/Resources/Sfx/, named after the Sfx value: Sfx/Slash, Sfx/Coin
// and so on (.wav, .ogg or .mp3 all work). Drop a file in with the right name
// and it plays -- no code change needed.
//
// That name can also be a *folder* holding several takes of the same sound. If
// it is, one is picked at random each time, never the same one twice in a row,
// so twenty sword swings in a row don't sound like a loop.
//
// If there is no file, the clip is *generated with maths* instead: a sound is
// just a long list of numbers between -1 and 1, and a speaker pushes air in and
// out following that list. A sine wave gives a clean beep, random numbers give a
// hiss, and fading the numbers down over time makes it stop. Those are only
// placeholders, so they stay silent unless you switch them on below.
//
// Anywhere in the game, just call:  GameAudio.Play(GameAudio.Sfx.Hit);
// Nothing to set up in the scene: the first call creates the object itself.
public class GameAudio : MonoBehaviour
{
    // The list of sounds the game can make. Add a name here and a recipe in Build().
    public enum Sfx { Slash, Shoot, Hit, EnemyDie, Gem, Coin, LevelUp, PlayerHurt, Harvest, Eat }

    [Tooltip("Turn every sound effect off, real files included.")]
    [SerializeField] bool muted = false;

    [Tooltip("Play the maths-generated placeholder for sounds that have no audio file yet. Off, so only real files are heard.")]
    [SerializeField] bool playGeneratedSounds = false;

    // Where real audio files are looked for, inside a Resources folder.
    const string SfxFolder = "Sfx/";

    // Volumes are not fields here: they live in SoundSettings, where the
    // settings window changes them and they are saved between sessions.

    [Tooltip("How many sounds can overlap. Past this, the oldest one is cut off.")]
    [SerializeField] int voiceCount = 16;

    [Tooltip("The same sound won't restart sooner than this, so 20 hits at once don't turn into a wall of noise.")]
    [SerializeField] float repeatDelay = 0.04f;

    // Samples per second. 44100 is CD quality and what most hardware expects.
    const int SampleRate = 44100;

    static GameAudio instance;

    AudioSource[] voices;      // we play through a handful of sources, round-robin
    int nextVoice;
    readonly Dictionary<Sfx, AudioClip[]> clips = new Dictionary<Sfx, AudioClip[]>();
    readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
    readonly Dictionary<Sfx, int> lastVariation = new Dictionary<Sfx, int>();

    // Runs automatically once the scene has loaded. If nobody put a GameAudio in
    // the scene (to tweak the volume in the Inspector), we make one ourselves.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateIfMissing()
    {
        if (instance != null)
            return;
        new GameObject("GameAudio").AddComponent<GameAudio>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // only ever one
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // survives going back to the main menu

        voices = new AudioSource[voiceCount];
        for (int i = 0; i < voices.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D: equally loud wherever it happens
            voices[i] = source;
        }
    }

    // The one method the rest of the game uses.
    // "pitch" lets the caller say something with the sound: 1 is the file as
    // recorded, higher is lighter and quicker, lower is heavier. The weapons
    // use it so the three strikes of a combo don't all sound the same.
    public static void Play(Sfx sfx, float volume = 1f, float pitch = 1f)
    {
        if (instance != null)
            instance.PlayInternal(sfx, volume, pitch);
    }

    void PlayInternal(Sfx sfx, float volume, float pitch)
    {
        if (muted)
            return;

        // Don't let the same sound retrigger every frame.
        if (lastPlayed.TryGetValue(sfx, out float last) && Time.unscaledTime - last < MinGap(sfx))
            return;
        lastPlayed[sfx] = Time.unscaledTime;

        // Find the clips the first time this sound is asked for, then keep
        // them. LoadAll handles both cases: one file called Slash, or a folder
        // called Slash with many takes inside. An empty result is remembered
        // too, so a missing sound isn't looked up again on every single swing.
        if (!clips.TryGetValue(sfx, out AudioClip[] options))
        {
            options = Resources.LoadAll<AudioClip>(SfxFolder + sfx);
            if ((options == null || options.Length == 0) && playGeneratedSounds)
                options = new AudioClip[] { Build(sfx) };
            clips[sfx] = options;
        }

        if (options == null || options.Length == 0)
            return; // no file for this sound yet, and placeholders are off

        AudioClip clip = options[PickVariation(sfx, options.Length)];

        AudioSource source = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;

        // A little random pitch so repeated hits don't sound like a machine,
        // on top of whatever pitch the caller asked for.
        source.pitch = pitch * Random.Range(0.94f, 1.06f);
        source.PlayOneShot(clip, volume * BaseVolume(sfx) * SoundSettings.Master);
    }

    // Pick which take to play. Rolling again when we land on the one we just
    // used is what stops the "same clip twice" that the ear picks up instantly.
    int PickVariation(Sfx sfx, int count)
    {
        if (count <= 1)
            return 0;

        int index = Random.Range(0, count);
        if (lastVariation.TryGetValue(sfx, out int previous) && index == previous)
            index = (index + 1) % count;

        lastVariation[sfx] = index;
        return index;
    }

    // Getting hurt fires every frame while an enemy touches you, so it needs a
    // much longer gap than a one-off sound like a coin.
    // Base loudness of single sounds, on top of the master volume. The files
    // are all normalised (as loud as they can be), so the ones you hear
    // constantly have their own slider or they drown out everything else.
    // Sounds without their own setting play at the file's full volume.
    static float BaseVolume(Sfx sfx)
    {
        switch (sfx)
        {
            case Sfx.Slash: return SoundSettings.Slash;
            case Sfx.Shoot: return SoundSettings.Shoot;
            default: return 1f;
        }
    }

    float MinGap(Sfx sfx) => sfx == Sfx.PlayerHurt ? 0.45f : repeatDelay;

    // ---- The recipes -------------------------------------------------------

    static AudioClip Build(Sfx sfx)
    {
        switch (sfx)
        {
            // Swoosh: hiss, softened a lot so it sounds like air, not static.
            case Sfx.Slash: return Noise("sfx_slash", 0.22f, decay: 13f, volume: 0.55f, smoothing: 0.82f);

            // Bow twang: a pitch dropping fast.
            case Sfx.Shoot: return Tone("sfx_shoot", 420f, 130f, 0.14f, decay: 20f, square: false, volume: 0.45f);

            // Hit: a very short, low, buzzy thud.
            case Sfx.Hit: return Tone("sfx_hit", 190f, 85f, 0.08f, decay: 28f, square: true, volume: 0.4f);

            // Death: a crunchier, longer hiss.
            case Sfx.EnemyDie: return Noise("sfx_die", 0.3f, decay: 11f, volume: 0.5f, smoothing: 0.55f);

            // Pickup blip: a short bleep sliding upwards.
            case Sfx.Gem: return Tone("sfx_gem", 880f, 1320f, 0.09f, decay: 14f, square: true, volume: 0.28f);

            // Coin: the classic two-note "ding".
            case Sfx.Coin: return Arpeggio("sfx_coin", new[] { 1046.5f, 1568f }, 0.06f, volume: 0.3f);

            // Picking berries: a soft, dull pluck.
            case Sfx.Harvest: return Tone("sfx_harvest", 620f, 380f, 0.16f, decay: 16f, square: false, volume: 0.35f);

            // Eating a berry: two quick soft notes, a small "mm, better".
            case Sfx.Eat: return Arpeggio("sfx_eat", new[] { 392f, 587.33f }, 0.07f, volume: 0.25f);

            // Level up: four notes of a happy chord climbing.
            case Sfx.LevelUp: return Arpeggio("sfx_levelup", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.09f, volume: 0.3f);

            // Getting hurt: a low groan sliding down.
            default: return Tone("sfx_hurt", 300f, 110f, 0.3f, decay: 9f, square: false, volume: 0.5f);
        }
    }

    // A pitch that slides from startHz to endHz while fading out.
    // "square" swaps the smooth sine for a harsh retro-console buzz.
    static AudioClip Tone(string name, float startHz, float endHz, float seconds, float decay, bool square, float volume)
    {
        float[] data = new float[Mathf.CeilToInt(SampleRate * seconds)];
        float phase = 0f; // how far through the wave we are

        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / data.Length; // 0 at the start, 1 at the end
            float hz = Mathf.Lerp(startHz, endHz, t);
            phase += 2f * Mathf.PI * hz / SampleRate;

            float wave = square ? Mathf.Sign(Mathf.Sin(phase)) : Mathf.Sin(phase);
            data[i] = wave * Envelope(t, decay) * volume;
        }
        return ToClip(name, data);
    }

    // Random numbers = hiss. Blending each sample with the one before dulls the
    // sharp edges, which turns a harsh "shhh" into a rounder "whoosh".
    static AudioClip Noise(string name, float seconds, float decay, float volume, float smoothing)
    {
        float[] data = new float[Mathf.CeilToInt(SampleRate * seconds)];
        float previous = 0f;

        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / data.Length;
            previous = Mathf.Lerp(Random.Range(-1f, 1f), previous, smoothing);
            data[i] = previous * Envelope(t, decay) * volume;
        }
        return ToClip(name, data);
    }

    // Notes played one after another, each left to ring on while the next starts.
    static AudioClip Arpeggio(string name, float[] notes, float secondsPerNote, float volume)
    {
        int step = Mathf.CeilToInt(SampleRate * secondsPerNote);
        float[] data = new float[step * notes.Length + SampleRate / 3]; // extra room to ring out

        for (int n = 0; n < notes.Length; n++)
        {
            int start = n * step;
            float phase = 0f;
            for (int i = 0; start + i < data.Length; i++)
            {
                phase += 2f * Mathf.PI * notes[n] / SampleRate;
                float fade = Mathf.Exp(-5f * i / SampleRate); // this note dying away
                // "+=" because notes overlap: their waves add together, like a chord.
                data[start + i] += Mathf.Sin(phase) * fade * volume;
            }
        }

        // Adding waves can push past 1, which crackles. Clamp it back.
        for (int i = 0; i < data.Length; i++)
            data[i] = Mathf.Clamp(data[i], -1f, 1f);

        return ToClip(name, data);
    }

    // Volume shape over the life of the sound: fade in fast (a hard start would
    // click), die away by "decay", and fade out at the very end (same reason).
    static float Envelope(float t, float decay)
    {
        float attack = Mathf.Clamp01(t / 0.02f);
        float release = Mathf.Clamp01((1f - t) / 0.05f);
        return attack * release * Mathf.Exp(-decay * t);
    }

    // Hand our list of numbers to Unity as a playable clip. 1 = mono (one speaker channel).
    static AudioClip ToClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
