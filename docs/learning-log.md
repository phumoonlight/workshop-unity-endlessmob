# Learning log

Unity topics covered so far, one per session, with the idea behind each. Add a line when a new topic is done.

| Topic | Where it lives | The idea |
|---|---|---|
| Particles | `SprintDust`, `Resources/Vfx/` | Particle systems are cheap when pooled. "Rate over distance" emission stops by itself when the object stops moving. |
| Animation events | `Animation/Clips/`, `AnimationEventRelay` | Let the animation say when a hit lands instead of a timer — put an event on the frame, and forward it to the script that cares. |
| Hit-stop and screen shake | `HitStop`, `CameraFollow.Shake` | Tiny pauses and a wobble sell impact. Slow time to 5 % rather than 0 so a "paused" check stays meaningful. |
| Profiler and pooling | `PerfProbe`, `Vfx` | Measure before optimising: the cost was renderers, not spawning. Reuse objects with `ObjectPool` instead of Instantiate/Destroy. |
| Post-processing | `Assets/_Game/Settings/` | URP Volumes blend profiles by weight; a second Volume with a higher priority can fade in an effect (low-health red). |
| Save / load | `PlayerProfile` | Save ids, not names. Write to a temp file and swap. Version the file and start fresh on mismatch. |
| Building an .exe | [building.md](building.md) | Build profiles, what to ship, what to keep. |
| Homing projectiles | `BouncingBlade` | Two ways to hit: fly straight and wait for a trigger collider (`Projectile`), or know the target and steer at it with `MoveTowards` (no collider needed, can't miss or overshoot). A `HashSet` remembers who was already hit. |
| Settings and PlayerPrefs | `SoundSettings`, `SettingsMenu` | Preferences go in PlayerPrefs, progress in the save file. A uGUI `Slider` draws nothing itself: it moves the anchors of a fill and a handle you give it. Two scripts reading the same key in one frame: let one act in `LateUpdate`. |
| Renaming a script | `BouncingBladeSkill` | Unity links components to scripts by the **guid in the `.meta`**, not the file name. Move the `.cs` and its `.meta` together and scenes keep working. |
| Physics layers | `Layers`, enemy prefabs | A layer is a label on a GameObject; a **layer mask** tells a physics query which labels to look at, so it skips the rest before your code runs. A layer is a number (6); a mask is 32 on/off bits (`1 << 6` = 64) — passing the number where a mask is wanted is the classic bug. Next step not done yet: the **collision matrix** (Project Settings > Physics) decides which layers bump into each other at all. |
| Pooling things with state | `PickupPool`, `MagnetPickup.OnEnable` | A pool is just a stack of switched-off spares. The hard part is the **reset**: `Awake` runs once per object, `OnEnable` runs every time it is switched on, so whatever a use changed (`flySpeed` grew, `isFlying` was set) goes back in `OnEnable`. Decide who owns the spares: effects live across scenes, pickups die with their run. |
| Enums instead of yes/no switches | `Enemy.Role` | Like a traffic light: one variable that is exactly one of a named list (`Attacker` / `Guard` / `Hunter`). Two `bool`s allow four combinations and some mean nothing; an enum makes the nonsense impossible to write. A `switch` on it picks the behaviour. This is the first half of a **state machine** — the second half (not done yet) is a state that *changes over time*: Idle → Chase → Attack. |
| Pitch as information | `GameAudio.Play`, `Weapon.chainPitch` | Random pitch only stops a sound feeling robotic; a *chosen* pitch tells the player something. Higher = lighter, lower = heavier, so the combo's strikes go 1 → 1.1 → 0.85. The same trick makes gem pickups climb in pitch when collected quickly (not done: `Gem` has no sound file yet). |
| Directional light | `Game.unity` > Directional Light | The scene's one sun: only its rotation matters, never its position. Light travels along the object's blue Z arrow; Rotation X tips it (sun height), Rotation Y turns it (which side). A low sun spreads its light thin on flat ground (50 deg lands 77 %, 25 deg lands 42 %), so an evening look needs more Intensity to pay it back. Set to 25 deg, orange, intensity 1.8. Not done yet: ambient light, skybox, fog. |
| Frame Debugger | Window > Analysis > Frame Debugger | A frame is one painting done stroke by stroke; each stroke is a **draw call**. Enable freezes one frame and the slider replays the strokes in order. Answers: what is expensive (long list), why something is missing (no stroke), why something is on top (later strokes cover earlier ones). The Profiler says a frame is slow; the Frame Debugger says which strokes. Opened and explained; the developer did not step through a frame yet. |
| Ambient light | Lighting window > Environment > Environment Lighting | The sun lights one side of things; ambient is the colour of the **other** side, the shadow side. It fakes light bouncing off walls: a flat glow from everywhere, no shadows. Source = Skybox samples it from the sky picture; Source = Color gives one swatch. The developer switched to Color and picked green by hand. Not done yet: skybox, fog. |
| Skybox and fog | Lighting window > Environment | The **skybox** is wallpaper on the inside of the room: the far background, a material not a light. The top-down camera never sees it and ambient no longer samples it, so it stays default. **Fog** is a gradient from the object's colour to one fog colour, with distance from the camera as the position; Linear mode gives Start (clear) and End (all fog). The far side of the arena fogs first because the camera is tilted. Set by the developer: green (0,130,40), Linear, 20 to 60. Generate Lighting was pressed once by mistake: it **bakes** a photo of the light for things that never move, useless on a flat floor with a moving mob; the files it wrote were deleted. |
| Collision matrix | Project Settings > Physics > Settings > Layer Collision Matrix | The guest list for physics: a triangle of checkboxes, one per pair of layers. Unticked pairs never touch and Unity skips checking them (like `pointer-events: none` on the web). Everything is ticked here and stays that way: enemies spread out **because** they bump into each other (`Enemy.FixedUpdate` sets velocity so physics pushes them apart), so Enemy vs Enemy must stay on, and the other layers hold nothing. Ask "does anything rely on that bump?" before unticking. |

## What to learn next
Agreed on 2026-09-22. The developer asked for **simpler explanations**: an everyday picture first (a traffic light for enums), one idea at a time, short.

Hands-on, no code to break — good while recent changes are still unplayed:

All hands-on topics are done. Needs code:
- **Animator Override Controller** (next up): same animator, different clips — puts the Assassin on the pack's twin-dagger animations.
- **State machine, second half**: a state that changes over time (Idle → Chase → Attack), which is what enemy attack animations need. `Enemy.Role` was the first half.

Skipped by the developer: **Git branches / tags** (they know git from web dev; only Unity and game dev are new). Also skipped for now: how survivors games handle thousands of enemies (steering, spatial grids, DOTS), unit tests for `ItemBag` / `PlayerProfile`, and the game-design topic (level-up choices, balance spreadsheet).

## Lessons from this project's bugs
- **`Random` and most Unity APIs can't run in a field initializer.** It throws in the constructor and the rest of the object's setup is abandoned. Initialise in `Awake`.
- **Method names `Start`, `Update`, `Awake`, `OnEnable`, `Reset` … are Unity messages.** Naming a helper `Start()` makes Unity call it on its own.
- **Static fields survive between Play presses** here, because domain reload is off. Reset statics with `[RuntimeInitializeOnLoadMethod]`.
- **Unity 6 empties every `ObjectPool` when leaving Play mode, after destroying the scene.** Pool destroy callbacks must null-check.
- **A disabled component's `Awake` still runs.** Only `Update`-style messages stop.
- **`== null` is true for destroyed Unity objects** — that is how you ask "is it still alive?".
- **Renaming a serialized field resets its Inspector values** unless you add `[FormerlySerializedAs("old")]`.
- **A blue Face Color on a shared TMP material tints every label** that uses it.
- **An `.asmdef` with `includePlatforms: []` means every platform**, so editor-only code ends up in the build.
- **Anything changed during Play mode is thrown away on Stop**, including Lighting-window settings. Tune in Play to find the value, then type it again after Stop and Ctrl+S.
