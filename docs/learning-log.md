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
