# Architecture — how the systems are built

Technical reference for the code in `game/Assets/_Game/Scripts/`. For what the game *is*, read [game-design.md](game-design.md).

## Folder map (inside `game/`)
| Path | Holds |
|---|---|
| `Assets/_Game/Scripts/` | All game code: `Core`, `Player`, `Weapons`, `Skills`, `Enemies`, `Progression`, `City`, `Inventory`, `UI`, `Camera` |
| `Assets/_Game/Prefabs/` | Enemies and camps (`Enemies/`), city buildings (`City/`), outposts, pickups, `BerryBush`, chests |
| `Assets/_Game/Data/` | ScriptableObjects: `Classes`, `Buildings`, `Outposts` |
| `Assets/_Game/Resources/` | Loaded by name at runtime: `Sfx/`, `Vfx/`, `Items/` |
| `Assets/_Game/Animation/` | Player controllers, `EnemyBody.controller`, upper-body mask, `Clips/` (our copies of attack clips, with events) |
| `Assets/_Game/Settings/` | Post-processing profiles (`PostProcessing.asset`, `LowHealth.asset`) |
| `Assets/_Game/Scenes/` | `MainMenu` (build index 0), `PreStart` (1), `Game` (2) |
| `Assets/Settings/Build Profiles/Windows.asset` | The build profile |
| `Assets/Haons SD series Pack/` | Third-party characters and animations (Asset Store, Unity-chan License). Read only. |
| `Packages/com.unity.springbone/` | Embedded package for hair/cloth physics |

Key packages: Unity 6000.6.2f1, URP 17.7, Input System 1.20, Toon Shader 0.15 (preview), AI Navigation 2.0.

Data assets are created from **Create > Endless Mob > Building / Item / Player Class**.

## The run
- **`EnemySpawner`** owns the pressure. The spawn ring sits just past the screen corners and is re-measured each frame (zoom changes it); it tries up to 8 times to find a truly off-screen spot near the map edge. Every second, enemies more than 2.2× the ring radius away are teleported back to the edge. `HealthMultiplier` (+8 %/min) is also read by camps. Spawned enemies call `Enemy.HuntPlayerOnly()`, stricter than the older `HuntPlayer()` used by wilderness scouts (which still considers buildings).
- **`PlayerHealth.deathEndsRun`** (on) ends the run on death; off restores the old respawn. `WaveHud` kept its name but shows the survival clock and, when the run ends, builds the result panel in code (`UiFactory`, on the clock's root canvas — no scene wiring): dark veil, title, icon columns for time / kills / coins, class strip, and TRY AGAIN / MAIN MENU buttons (clicks work at `timeScale` 0). It fades in with `Time.unscaledDeltaTime` because the game is frozen.
- **Pause** lives in `DebugHUD`: Esc stops time only when it is running normally, so it cannot unfreeze the hero-select panel. This codebase uses `Time.timeScale == 0` to mean "paused", and prompts check it.

## Camps
- `CampManager` / `FortifiedCampManager` handle caps, respawn timers and placement. Placement checks the **camera view** (it used to ask the fog of war, which is now off). `FortifiedCampManager.Tier` = large camps cleared so far.
- `CampCapture` is a `Capture` child on all three camp prefabs (small's is a copy of medium's). The disc is sized from `radius` in `Awake`; `speedWithEnemiesInside` = 0.35. Draining, retakes and counter-attack groups were removed (in git history).
- `Loot.Scatter` drops `rewardCoins` / `rewardGems` on capture (renamed from `chestCoins` with `[FormerlySerializedAs]`).
- `EnemyCamp.worldEnemyBudget` (120) stops guard growth while the world is full. Large camps spawn troops only when the hero is within 40 m; a large camp's `EnemyTower` is disabled the moment capture begins.

## The hero
- **Body vs model:** the Player is a capsule with a `CharacterController` — all gameplay runs on it and its mesh is hidden. `ModelSword` / `ModelBow` are children; `PlayerModel` shows the chosen one and feeds `CharacterController.velocity` into a `Speed` blend tree (0 / 3 / 6 m/s). Root motion is off on purpose.
- **Attack chain:** layer 1 of each animator controller is an upper-body `Attack` layer. `Weapon.NextChainStrike` runs the 100 / 125 / 150 % chain; `PlayerModel.Attack(step)` plays the matching swing; the layer weight fades over `swingTime` (0.75 s).
- **Attack timing — the animation says when a blow lands.** Each clip in `Animation/Clips/` carries an `AttackImpact` event at the fastest frame (sword 0.43 / 0.32 / 0.55 s, bow 0.26 / 0.12 / 0.17 s). The Animator sits on the model, so `AnimationEventRelay` forwards the event to the enabled `Weapon`. `Weapon.BeginImpact(fallback)` remembers a swing, `AnimationImpact()` lands it, `UpdateImpactFallback()` lands it anyway if no event arrives. Weapons implement `OnImpact()` and re-aim there. `Weapon.TraceImpacts` (off) logs every step when timing needs debugging.
- **Skills:** `SkillController` drives `ClassSkill` subclasses (`WhirlwindSkill`, `BouncingBladeSkill`, `RapidFireSkill`). A skill can override `CanActivate()` to refuse a key press without spending its cooldown (Bouncing Blade does when no enemy is in range).
- **Assassin projectiles:** `AutoShooter` throws `Prefabs/Dagger.prefab` (the plain `Projectile` script: flies straight, trigger collider). The old `Projectile.prefab` arrow is still used by the shelved `Tower` and `Fort`. `Prefabs/BouncingBlade.prefab` (`BouncingBlade` script) is different: it has **no collider** — it always knows its target, steers at it with `MoveTowards`, counts a hit when within `hitDistance`, then picks the nearest enemy not in its `alreadyHit` set. Both use the pack's `TwinDagger1` mesh and material by reference (scaled ×2 / ×3, negative scale like the pack's own prefab). `AutoShooter` also throws the same prefab on the chain's final strike (`finalStrikeBladePrefab`, scaled down, fewer targets, less damage — all passed through `BouncingBlade.Launch`), so there is one blade prefab, not two. `BouncingBladeSkill` kept `BarrageSkill`'s `.meta` guid, so the scene component survived the rename.
- **Impact feel:** `HitStop.Freeze` crawls time to **5 %, never 0** (0 means paused), won't disturb a real pause, and only restores time if the value is still its own. `CameraFollow.Shake` uses Perlin noise in screen space on unscaled time with a clean `basePosition` so the follow doesn't chase the shake. Both fire only when a swing **connects** (0.045 s / 0.12 m; double freeze and triple shake on the final strike). The Brute's slam shakes only when it hits the player.
- **`SlashEffect`** builds a crescent mesh once and moves its vertices each frame so the arc sweeps. Whirlwind reuses it at 360°.
- **`SprintDust`** is a looping particle system toggled by `PlayerStamina.IsSprinting`, emitting by **rate over distance** so it stops by itself when the hero stops.

## Progression and saving
- **Stage XP** (`PlayerExperience`, `LevelUpRewards`) is per-run. A run starts at the class level via `PlayerExperience.StartAtLevel` (no events, no popups) then `LevelUpRewards.GrantLevels(level - 1)` for the skipped levels' stats. The class XP curve lives on each `PlayerClassData` (`classFirstLevelXp` 300, `classExtraXpPerLevel` 200).
- **`RunBank`** (Game scene) is the bridge between a run and the profile. On `PlayerClassSelector.ClassChosen` it sets the starting level and applies carried `maxHealthBonus` items. It banks coins, inventory and class XP on **every** way a run ends — defeat, `OnApplicationQuit`, `OnDestroy` — guarded to bank once.
- **`RunSettings.ChosenClass`** (static, not saved) carries the pre-start pick into the Game scene. Only when it is null (Play pressed straight in the Game scene) does `PlayerClassSelector` show its own hero picker.
- **Items:** `ItemBag` (plain C#) holds the slot/stack rules for both the run's `Inventory` (a thin MonoBehaviour that starts as a copy of the stash) and the profile's stash. `ItemData` fields: `usable`, `healAmount`, `revivesOnDeath`, `maxHealthBonus`, `buyPrice`, `sellPrice`, `maxStack`, `color`. `PlayerHealth.TryRevive` handles the Relic. `BerryBush.holdSeconds` = 1.

### Saving
`PlayerProfile` is static (one copy for the whole game, survives scene loads) and holds banked `Coins`, the item `Stash` and class XP by class id. It saves itself after banking and after every shop trade, as JSON:
- Built game: `%USERPROFILE%\AppData\LocalLow\Iampumon\EndlessMobPoc\profile.json`
- Editor: `...\Iampumon\EndlessMobPocDev\profile.json` (`#if UNITY_EDITOR`), so testing never touches real progress.
- **Changing Company or Product Name moves both folders** and orphans existing saves.
- Written to a temp file then swapped in, so a crash mid-write can't corrupt it. `Load()` runs before the first scene (`RuntimeInitializeOnLoadMethod`); an unreadable file is copied to `.broken` and the game starts fresh.
- **Ids, not names.** Items save by `ItemData.id` (Berry 1, Relic 2, Armor 3 — next free 4) and classes by `PlayerClassData.id` (Swordsman 1, Assassin 2 — was the Archer, same id — next free 3), so assets can be renamed freely. **Never change or reuse an id.** Duplicate or unset (0) ids log errors.
- **`SaveVersion` = 2.** Any other version is ignored (fresh start, overwritten on next save) — no migration by design. Bump it whenever the layout changes.
- Delete the file to reset progress. `Player.log` next to it holds a built game's console output.

## Menus and HUD
- Menus are **built in code** with `UiFactory` (center-anchored boxes, text, buttons), so scenes only hold the buttons that open them: `ShopMenu` (PreStart; `IsOpen` so Esc closes it first), `PreStartMenu` (a card per class with level + XP bar; click or 1/2 to pick, Enter to start, Esc back), `HowToPlayMenu` (MainMenu; its text is plain strings — **the one place to update when controls or rules change**). Panels call `SetAsLastSibling()` when opening because UI draws in hierarchy order.
- **`RunHud`** (Game `UI` canvas): red HP bar above the item bar; top-left icon + value rows for time, kills, coins. Icons are drawn in code by `HudIcons` (signed distances, anti-aliased). Values only become strings when they change. Bottom edge upwards: XP bar (y 0–30), stamina (38), inventory (62), HP bar (134); skill slots bottom-left at 40 and 128. `WaveHud` (top-centre) still shows the big survival clock — two clocks, pending a decision.
- **`DebugHUD`** (OnGUI): FPS + live mob count bottom-right, the pause screen, R/M after a run.
- **DEV panel** (`DevTools`, DEV button / F1): coins, XP, level, spawn groups around the hero. Compiled out of release builds (`#if !UNITY_EDITOR && !DEVELOPMENT_BUILD`).
- Main menu title is **ENDLESS MOB**; the old subtitle object is hidden, not deleted.
- If UI text ever turns a strange colour, check the shared `LiberationSans SDF Material` Face Color first (it multiplies every label; it was once blue).

## Presentation
- **Audio:** `GameAudio` creates itself and finds each sound by name in `Resources/Sfx/` — a file, or a **folder of takes** (one picked at random, never the same twice). `Sfx/Slash/` holds 28 sword swings (Vorbis, mono). `Sfx/Shoot.ogg` (0.45 s, mono) is the Assassin's dagger throw, played by `AutoShooter` and `BouncingBladeSkill` — a single take for now; turn it into a `Shoot/` folder of takes if it gets repetitive under Rapid Fire. A missing sound is remembered as missing until the next Play press. Every other sound is silent: the generated placeholders are off (`playGeneratedSounds`). The swings peak around 0.3 s, so `MeleeSlash` starts the sound at the wind-up.
- **Particles:** `Vfx.Play(Vfx.Kind.X, position, scale)` loads `Resources/Vfx/Hit`, `Death`, `Pickup` by enum name and pools them with `UnityEngine.Pool.ObjectPool` (`stopAction = Callback`; `PooledVfx` returns itself). Called from `Enemy.TakeDamage`, `Enemy.Die` and `MagnetPickup`.
- **Enemy visuals:** gameplay is a box (Rigidbody + BoxCollider); a Haons character sits under each as `Model`. `EnemyBody.controller` (stand/walk/run on `Speed`, 0 / 1.6 / 5.5) is shared; `EnemyModel` feeds it the Rigidbody's velocity. Unwanted renderers must be **destroyed, not disabled**, because `Enemy.Update` re-enables every renderer for fog visibility.
- **Post-processing:** URP runs from the quality level's `PC_RPAsset` (`GraphicsSettings.defaultRenderPipeline` reads as none — normal here). The Main Camera needed a `UniversalAdditionalCameraData` before post-processing ran at all. `Global Volume` → `PostProcessing.asset`: Neutral tonemapping, bloom (threshold 1.1, intensity 0.55), colour adjustments (+0.15 exposure, +12 contrast, +10 saturation), vignette 0.25; FXAA on the camera. `Low Health Volume` (priority 1) → `LowHealth.asset` (red vignette, −65 saturation); `LowHealthEffect` only moves the Volume's **weight** from 0 at half health to 1 at zero.

## Performance
- **F9** in play mode records; F9 again writes `Logs/PerfReport.txt` (`PerfProbe`: frame-time percentiles, GC per frame, peaks). Editor and Development Builds only.
- First measurement (158 enemies): the cost was **enemy renderers, not spawning** — each had 21 renderers / 26 materials / 20 shadow casters, 7 of them zero-triangle `GuideDummy` markers (each with a `TextMesh`, which must be removed before its `MeshRenderer`). Now 8–9 renderers, 13–14 materials, no shadow casting.
- Also done: building scans cached for 0.3 s with a staggered start; hot loops **index** `Structure.All` (a `foreach` over an interface allocates an enumerator); HUD text only rebuilt on change.
- A built game runs noticeably smoother than the editor.

## Shelved systems
Switched off in the Game scene, code kept: the `Castle` object (corner archers, `CastleUpgrade`), `WaveManager`, `CastleHealthBar`, `ChestSpawner`, `WildernessScouts`, the DEV start-wave button, the `FogOfWar` object (with it gone, `FogOfWar.IsVisible` answers "visible" everywhere), and building (`BuildManager` disabled and its "[B] Build" hint hidden — its `Awake` still runs, the hint is only managed in `Update`). `FortifiedCamp.leavesOutpostSite` (off) stops outpost foundations. `OffscreenEnemyIndicators` still reads the disabled `WaveManager` and shows nothing. All `City/` scripts (Barrack, Tower, Builder, outposts, income) are intact.
