# workshop-unity-endlessmob

A 3D Vampire-Survivors-style action game in **Unity 6.6 (6000.6.2f1)** with URP and the Input System. The developer is **new to game development**: explain simply, keep changes small, and teach the *why*.

The product is named **EndlessMobPoc** by **Iampumon** (Player Settings), so the build is `EndlessMobPoc.exe`. The Unity project lives in `game/` — nothing inside the project depends on the folder name. The `Create` menu for data assets is **Create > Endless Mob > …**.

## Repository layout
- `game/` — **the Unity project** (open this folder in Unity Hub). It has its own `.gitignore`.
- `docs/` — documentation for humans (outside the Unity project, so Unity doesn't import it)
- `CLAUDE.md`, `README.md` — repo root

**Every project path below (`Assets/…`, `Packages/…`, `ProjectSettings/…`, `Logs/…`) is relative to `game/`.** From the repo root, prefix it with `game/`.

## The game
Survive as long as you can. Enemies spawn endlessly just off-screen and rush the hero, faster and tougher the longer the run lasts; dying ends the run. Pick a hero (Swordsman or Archer) with a 3-hit auto-attack chain and Q/R skills. Clear enemy camps — wipe the guards, then stand in the capture circle — for coins and XP gems. Hold E at bushes to pick berries. Between runs, coins, items and class XP are kept: a shop sells items, and each class has a level that the next run starts at.

It used to be **castle defense with waves**. That mode is **shelved, not deleted** (see "Shelved systems").

**Scene flow:** `MainMenu` (PLAY, HOW TO PLAY, QUIT) → PLAY → `PreStart` (pick hero, see class levels and coins, SHOP, START / BACK) → START → `Game`. After a run: **R** retries with the same hero, **M** returns to the main menu. **Esc** pauses a run; **M** from the pause quits to the menu, keeping everything.

## Project layout
- `Assets/_Game/Scripts/` — all game code: `Core`, `Player`, `Weapons`, `Skills`, `Enemies`, `Progression`, `City`, `Inventory`, `UI`, `Camera`
- `Assets/_Game/Prefabs/` — enemies (and camps under `Enemies/`), city buildings, outposts, pickups, `BerryBush`
- `Assets/_Game/Data/` — ScriptableObject data: `Classes`, `Buildings`, `Outposts`
- `Assets/_Game/Resources/` — loaded at runtime: `Sfx/` and `Vfx/` (by name), `Items/` (every item the save file can mention, by id)
- `Assets/_Game/Animation/` — player controllers, `EnemyBody.controller`, upper-body mask, and `Clips/` (our copies of the attack clips, with events)
- `Assets/_Game/Settings/` — post-processing profiles (`PostProcessing.asset`, `LowHealth.asset`)
- `Assets/_Game/Scenes/` — `MainMenu` (build index 0), `PreStart` (1), `Game` (2)
- `Assets/Settings/Build Profiles/Windows.asset` — the build profile
- `Assets/Haons SD series Pack/` — third-party characters and animations (Asset Store, Unity-chan License). **Read from it; never edit it.**
- `Packages/com.unity.springbone/` — embedded package (hair/cloth physics), committed; its editor `.asmdef` was fixed (see Building)
- Documentation for humans belongs in the repo-root `docs/` folder (never inside `Assets` — Unity imports everything there)

## Systems

### The run
- **`EnemySpawner`** owns the pressure. Enemies appear on a ring just past the screen corners (re-measured each frame, since zoom changes it; up to 8 tries to find a truly off-screen spot near the map edge) at 0.7/s, rising 0.45/s per minute to 6/s, never more than 120 of its own alive. Mix: 85% melee / 10% fast / 5% ranged, plus a Brute at 3:00 and every 2:00 after. Enemy health +8% per minute (`HealthMultiplier`, also read by camps). Every second, enemies left more than 2.2× the ring away are teleported back to the edge. Spawned enemies call `Enemy.HuntPlayerOnly()` — they ignore buildings entirely (stricter than the older `HuntPlayer()` used by wilderness scouts).
- **Death ends the run** (`PlayerHealth.deathEndsRun`; off = the old respawn). `WaveHud` kept its name but shows the survival clock and the end-of-run summary.
- **Pause** (`DebugHUD`): Esc stops time only when it's running normally, so it can't unfreeze the hero-select panel.

### Camps
- **Caps and respawn:** start with 4 small / 2 medium / 1 large; caps 8 / 4 / 2; while below its cap a size gets a new camp every 60 s / 180 s / 300 s. The timer only runs while below the cap. Large camps go on a map side with no large camp, 45 m+ from the hero and off-screen; each new one is a tier tougher (`FortifiedCampManager.Tier` = large camps cleared). Placement checks the **camera view** (it used to ask the fog, which is off — see Shelved).
- **Capture** (`CampCapture`, a `Capture` child on all three camp prefabs; small's is a copy of medium's): after the guards die, a circle appears. The gauge **only rises** — full speed with the hero alone, ×0.35 (`speedWithEnemiesInside`) with enemies inside too, frozen while the hero is outside. Radius / time: small 4 m / 2.5 s, medium 6 m / 5 s, large 9 m / 10 s. No draining, retake or counter-attack groups (removed; in git history). The disc is sized from `radius` in `Awake`. A large camp's `EnemyTower` is disabled the moment capture begins.
- **Loot:** a captured camp scatters coins and gems on the spot (`Loot.Scatter`) — no chest. Small 8 coins / 5 gems, medium 25 / 12, large 60 / 30 (`rewardCoins`/`rewardGems`, renamed from `chestCoins` with `[FormerlySerializedAs]`).
- Guards: medium/large camps grow +1 guard a minute; `EnemyCamp.worldEnemyBudget` (120) stops growth while the world is full. Large camps spawn troops only when the hero comes within 40 m.

### The hero
- **Visuals:** the Player is still a capsule with a `CharacterController` (all gameplay runs on it; its mesh is hidden). `ModelSword` / `ModelBow` are children; `PlayerModel` shows the chosen one and feeds `CharacterController.velocity` into a `Speed` blend tree (0/3/6 m/s). Root motion is off on purpose.
- **Attacks:** layer 1 of each controller is an upper-body `Attack` layer. `Weapon.NextChainStrike` runs the 100/125/150% chain; `PlayerModel.Attack(step)` plays the matching swing; the layer weight fades per swing (`swingTime` 0.75 s).
- **Attack timing:** the **animation** says when a blow lands. Each attack clip in `Animation/Clips/` carries an `AttackImpact` event at the fastest frame (sword 0.43 / 0.32 / 0.55 s, bow 0.26 / 0.12 / 0.17 s). The Animator lives on the model, so `AnimationEventRelay` forwards the event to the enabled `Weapon`. `Weapon.BeginImpact(fallback)` remembers a swing, `AnimationImpact()` lands it, `UpdateImpactFallback()` lands it anyway if no event comes. Weapons fill in `OnImpact()` and re-aim there. `Weapon.TraceImpacts` (off) logs every step when timing needs debugging.
- **Skills:** Q unlocks at stage LV 10 (Whirlwind / Barrage), R at LV 5 (Archer's Rapid Fire; Swordsman has no R yet).
- **Impact feel:** `HitStop.Freeze` crawls time to **5%, never 0** (this codebase uses `timeScale == 0` to mean paused, and prompts check it); it won't disturb a real pause and only restores time if the value is still its own. `CameraFollow.Shake` uses Perlin noise in screen space on unscaled time, with a clean `basePosition` so the follow doesn't chase the shake. Both fire only when a swing **connects** (0.045 s / 0.12 m, double freeze and triple shake on the final strike); the Brute's slam shakes only when it hits the player.
- **Slash effect:** `SlashEffect` builds a crescent mesh once and moves its vertices each frame so the arc sweeps. Whirlwind reuses it at 360°.
- **Sprint dust:** a looping particle system (`SprintDust` child) switched on/off from `PlayerStamina.IsSprinting`, emitting by **rate over distance**, so it stops by itself when the hero stops moving.

### Progression and saving
- **Stage vs class level:** in-run XP and levels are *stage* XP/levels — cheap (5, 10, 15… XP), +1% ATK and +1% max HP each, skills unlock by stage level, gone at run end. The whole run's stage XP is poured into that class's **class XP** (curve on each `PlayerClassData`: `classFirstLevelXp` 300, `classExtraXpPerLevel` 200 — first guesses). A run **starts at the class level**: `PlayerExperience.StartAtLevel` (no events, no popups) then `LevelUpRewards.GrantLevels(level - 1)` for the skipped levels' stats.
- **`RunBank`** (Game scene) is the bridge. On `PlayerClassSelector.ClassChosen` it sets the starting level and applies carried `maxHealthBonus` items. It **banks** coins, inventory and class XP on **every** way a run ends — defeat, `OnApplicationQuit` (closing the window or Stop in the editor), `OnDestroy` (leaving the scene) — guarded to bank once. The wallet starts every run at 0.
- **`PlayerProfile`** (static — one copy for the whole game, survives scene loads) holds banked `Coins`, the item `Stash` (an `ItemBag`) and class XP by class id. It **saves itself automatically** (no button or menu): after banking and after every shop trade, as JSON:
  - built game: `%USERPROFILE%\AppData\LocalLow\Iampumon\EndlessMobPoc\profile.json`
  - editor: `...\Iampumon\EndlessMobPocDev\profile.json` (`#if UNITY_EDITOR`) — so testing never touches real progress
  - **Changing Company or Product Name moves both folders** and orphans existing saves.
  - Written via a temp file swapped in (a crash mid-write can't corrupt it). `Load()` runs before the first scene (`RuntimeInitializeOnLoadMethod`); an unreadable file is copied to `.broken` and the game starts fresh.
  - **Ids, not names:** items and classes are saved by `ItemData.id` (Berry 1, Relic 2, Armor 3 — **next free: 4**) and `PlayerClassData.id` (Swordsman 1, Archer 2 — **next free: 3**), so assets and display names can be renamed freely. **Never change or reuse an id.** A duplicate or unset (0) item id logs an error on load; an unset class id logs when XP is added.
  - **`SaveVersion` = 2.** Any other version is ignored (fresh start, overwritten on next save) — no migration by design. Bump it whenever the layout changes.
  - Delete the file to reset progress. `Player.log` next to it holds a built game's console output.
- **`RunSettings.ChosenClass`** (static, not saved) carries the PreStart pick into the Game scene. `PlayerClassSelector` starts at once with it; only when it's null (Play pressed straight in the Game scene in the editor) does its own hero picker appear.
- **Items:** `ItemBag` (plain C#) holds the slot/stack rules for both the run's `Inventory` (a thin MonoBehaviour that starts as a copy of the stash) and the stash. `ItemData`: `usable`, `healAmount`, `revivesOnDeath`, `maxHealthBonus`, `buyPrice`, `sellPrice`, `maxStack`, `color`. Berry 10/5 (1–0 keys eat it; refused at full health), Relic 100/50 (instead of dying, one is used and you rise at 50% HP with 2 s of invulnerability — `PlayerHealth.TryRevive`), Armor 500/250 (+25 max HP per one carried). Berry picking is a 1 s hold of E (`BerryBush.holdSeconds`).

### Menus and HUD
- **Built in code** with `UiFactory` (center-anchored boxes, text, buttons), so only the buttons that open them are placed in scenes:
  - `ShopMenu` (PreStart) — rows per item, stash row, checks for stash room **before** taking coins; `IsOpen` so Esc closes it first.
  - `PreStartMenu` — a card per class (name, description, class level + XP bar); click or 1/2 to pick, Enter to start, Esc back; live coin count.
  - `HowToPlayMenu` (MainMenu) — its text is plain strings in that script: **the one place to update when controls or rules change.**
  - Panels call `SetAsLastSibling()` when opening, because UI draws in hierarchy order and other code-built UI may come later.
- **`RunHud`** (Game `UI` canvas): red HP bar above the item bar; top-left icon + value rows for time, kills, coins. Icons are drawn in code by `HudIcons` (signed distances, anti-aliased) until there is art. Values only become strings when they change. Bottom edge upwards: XP bar (full width, y 0–30), stamina (38), inventory (62), HP bar (134); skill slots bottom-left at 40 and 128. The big survival clock (`WaveHud`, top-centre) also still shows — **two clocks**, pending the developer's call.
- **`DebugHUD`** (OnGUI): FPS + live mob count bottom-right, the pause screen, R/M after a run.
- **DEV panel** (DEV button / F1): coins, XP, level, spawn groups around the hero. **Compiled out of release builds** (`#if !UNITY_EDITOR && !DEVELOPMENT_BUILD`); works in the editor and Development Builds.
- Main menu title is **ENDLESS MOB**; the old subtitle object is hidden, not deleted.
- UI text had turned blue because the shared `LiberationSans SDF Material` had a blue Face Color (it multiplies every label). Fixed to white — check it first if text colour ever looks wrong.

### Presentation
- **Audio:** `GameAudio` creates itself and finds each sound by name in `Resources/Sfx/` — a file or a **folder of takes** (one picked at random, never the same twice). `Sfx/Slash/` holds 28 sword swings (Vorbis, mono); **every other sound is silent**, since the maths placeholders are off (`playGeneratedSounds`). The swings swell to a peak around 0.3 s, so `MeleeSlash` starts the sound at the wind-up. The developer wants **real audio files**, not synthesized ones.
- **Particles:** `Vfx.Play(Vfx.Kind.X, position, scale)` finds prefabs in `Resources/Vfx/` by name; pooled with `UnityEngine.Pool.ObjectPool` (`stopAction = Callback`, `PooledVfx` returns itself). Hit sparks and pickup sparkles (additive `VfxSpark`), death puff (`VfxSmoke`), from `Enemy.TakeDamage`, `Enemy.Die` and `MagnetPickup`.
- **Enemy visuals:** gameplay is still a box (Rigidbody + BoxCollider); the mesh was deleted and a Haons character sits under each as `Model`. `EnemyBody.controller` (stand/walk/run on `Speed`, 0/1.6/5.5) is shared; `EnemyModel` feeds it the Rigidbody's velocity. Renderers must be **destroyed, not disabled**, because `Enemy.Update` re-enables every renderer for fog visibility.
- **Post-processing:** URP runs from the quality level's `PC_RPAsset` (`GraphicsSettings.defaultRenderPipeline` reads as none — normal here). The Main Camera needed a `UniversalAdditionalCameraData` before post-processing ran at all. `Global Volume` → `PostProcessing.asset`: Neutral tonemapping, bloom (threshold 1.1, 0.55), colour adjustments (+0.15 exposure, +12 contrast, +10 saturation), vignette 0.25; FXAA on the camera. `Low Health Volume` (priority 1) → `LowHealth.asset` (red vignette, −65 saturation); `LowHealthEffect` only moves the Volume's **weight** from 0 at half health to 1 at zero.

### Performance
- **F9** in play mode records, F9 again writes `Logs/PerfReport.txt` (`PerfProbe`: frame-time percentiles, GC per frame, peaks; editor and Development Builds only).
- First measurement (158 enemies): the cost was **enemy renderers, not spawning** — each had 21 renderers / 26 materials / 20 shadow casters, 7 of them zero-triangle `GuideDummy` markers (each with a `TextMesh`, which must be removed before its `MeshRenderer`). Now 8–9 renderers, 13–14 materials, no shadow casting. Also: building scans cached 0.3 s with a staggered start; hot loops **index** `Structure.All` (an interface `foreach` allocates an enumerator); text only rebuilt on change.
- Ideas not yet done (only if it stutters): **dormant camps** (spawn guards only when the hero is near, like large camps), and idle enemies setting `linearVelocity = zero` every step, which keeps their rigidbodies from ever sleeping.
- A built game runs noticeably smoother than the editor.

### Shelved systems (switched off, code kept)
In the Game scene: the `Castle` object (with corner archers and `CastleUpgrade`), `WaveManager`, the castle health bar, `ChestSpawner`, `WildernessScouts`, the DEV start-wave button, the `FogOfWar` object (with it gone, `FogOfWar.IsVisible` answers "visible" everywhere), and building (`BuildManager` disabled, its "[B] Build" hint hidden — its `Awake` still runs but the hint is only managed in `Update`). `FortifiedCamp.leavesOutpostSite` (off) stops outpost foundations. `CampCapture` no longer has counter-attacks at all. `OffscreenEnemyIndicators` still reads the disabled `WaveManager` and shows nothing. `City/` scripts (Barrack, Tower, Builder, outposts) are intact.

## How Claude works on this project
- The **Unity editor must be open**. Control it with the Unity CLI: `$LOCALAPPDATA/Unity/bin/unity.exe` (package `com.unity.pipeline`, port 7800). Check with `unity status` / `unity command editor_status`.
- Workflow: edit scripts → `unity command recompile`, poll `recompile_status` → wire scenes/prefabs with an `eval_file` setup script → `console_status` (and `console --level error` for messages and stack traces) → review → commit.
- **Claude's working directory is the repo root**, not the Unity project. Shell, Grep and file-tool paths need the `game/` prefix (`game/Assets/_Game/Scripts/…`). Paths passed to Unity APIs inside `eval_file` scripts (`AssetDatabase`, scene paths) stay project-relative (`Assets/…`).
- **Check `editor_status` for play mode first.** Saving scenes, creating assets and editing prefabs throw "cannot be used during play mode"; the developer often has the game running. Ask them to press Stop.
- Setup scripts live in the session scratchpad (throwaway, not in the repo). Edit a scene whether or not it's open with `SceneManager.GetSceneByPath(path)`; if not loaded, `EditorSceneManager.OpenScene(path, OpenSceneMode.Additive)`, then save and `CloseScene` it.
- **Keep token use low:** no screenshots or playtesting. Compile-check, then the developer playtests and sends screenshots. State plainly what wasn't seen on screen.
- The developer is a beginner learning by topic. Covered so far: particles, animation events, hit-stop and shake, profiler and pooling, post-processing, save/load, building an .exe. Explain each change and the Unity idea behind it.

### eval_file gotchas
- The file must end in **`.cs`**, and is a **method body**: no `using`; fully qualify everything (`UnityEditor.AssetDatabase`, `System.Text.StringBuilder`).
- `Object` is ambiguous — write `UnityEngine.Object`. Scene saving is `UnityEditor.SceneManagement.EditorSceneManager`.
- Avoid lambdas. Local helper methods placed after the final `return` work.
- Private `[SerializeField]`s: `new UnityEditor.SerializedObject(c).FindProperty("name")` then `ApplyModifiedPropertiesWithoutUndo()`. For TMP text, set `m_text` that way too — a generic `GetComponentInChildren<TMP_Text>()` once failed to find a `TextMeshProUGUI`.
- Finish with `MarkSceneDirty` + save + `AssetDatabase.SaveAssets()`, and `return` a short log string.
- Make scripts re-runnable: delete the object/asset first if it exists.
- **Calls over ~5 s time out** (e.g. `PlayerBuildInterface.CompilePlayerScripts`); the work still finishes, so have the script write its result to a file and poll for it.
- `EditorBuildSettings.scenes` changes only reach disk after `unity command save_all`.
- `FindFirstObjectByType` is deprecated — use `FindAnyObjectByType` (pass `FindObjectsInactive.Include` to find switched-off objects).
- Private statics via reflection (`BindingFlags.NonPublic | BindingFlags.Static`) are handy for testing, e.g. calling `PlayerProfile.Load` for a save round-trip. Restore the developer's save file afterwards.

### Editing files from the shell
- Python string replacements in heredocs mangle backslashes: `\n` becomes a real newline in C# strings, `\U` in Windows paths breaks. `sed` treats `\U`/`\L` as case commands. **For anything containing backslashes, use the Edit tool.**
- Always `assert old in s` before replacing, so a pattern that silently fails to match is caught.
- **Review `git status` before `git add -A`.** Unity rewrites settings files during builds and play (URP prefilter data, `ProjectSettings.asset`, build leftovers); commit only what belongs.

### Unity lessons from this project's bugs
- **`Random` (and most Unity APIs) can't run in a field initializer** — it throws in the constructor and abandons the rest of the object's setup. Initialise in `Awake`.
- **Method names `Start`, `Update`, `Awake`, `OnEnable`, `Reset`… are Unity messages** — naming a helper `Start()` makes Unity call it on its own.
- **This project keeps static fields between Play presses** (domain reload is off). Reset statics with `[RuntimeInitializeOnLoadMethod(SubsystemRegistration or BeforeSceneLoad)]`.
- Unity 6 empties every `ObjectPool` when leaving Play mode **after** destroying the scene, so pool destroy callbacks must null-check.
- A disabled component's `Awake` still runs; only `Update`-style messages stop.
- `==` null is true for destroyed Unity objects — the way to ask "is it still alive?".
- Renaming a serialized field resets its Inspector values unless you add `[FormerlySerializedAs("old")]`.

## Code conventions
- Short, beginner-friendly comments explaining *why*, like the existing scripts.
- `[SerializeField]` private fields with `[Tooltip]` for anything worth tuning in the Inspector.
- Shared base classes: `Weapon`, `ClassSkill`, `Structure` (castle + buildings), `MagnetPickup` (gems + coins).
- Things found by name at runtime go in `Resources/` (sounds, effects, items), so adding one needs no wiring.
- Commit after each working change, with a message explaining what and why.

## Git
- The repository root is **one level above the Unity project** (`.git` at the root, project in `game/`). Hosted on GitHub; no LFS (largest file is ~4 MB — GitHub rejects files over 100 MB, so add LFS before committing anything that big).
- The Unity `.gitignore` lives in `game/`, because its `/Library/`-style patterns are relative to the folder the file is in. Don't move it back to the root.
- Everything under `Assets/` is committed, including Asset Store imports, and **never without its `.meta`** (GUIDs are what references point at).
- `Packages/<name>/` folders are embedded packages and are committed; registry packages are one line in `manifest.json`.
- Gitignored (in `game/`): `Library/`, `Temp/`, `Logs/` (PerfReport lives there), `UserSettings/`, `Builds/`, `ProfilerCaptures/`, generated `*.csproj`/`*.slnx`, and `Assets/Resources/PerformanceTestRun*` (left behind by failed builds).

## Building
- **File → Build Profiles → Windows** (profile asset committed), Development Build **off** for sharing. Build **outside** the repo, or into `game/Builds/` (gitignored). Never into a folder that git would pick up.
- Share by zipping the build folder **except** `EndlessMobPoc_BackUpThisFolder_ButDontShipItWithYourGame` (Burst's debug data — keep it with that build's zip for decoding crash reports, never ship it). Windows SmartScreen will warn on an unsigned game: "More info → Run anyway". No installer by choice (Inno Setup is the option if one is wanted).
- The "Missing Project ID" prompt is fine to answer **Yes**: the game uses no Unity online services.
- Fixed build blocker: `Packages/com.unity.springbone/Editor/*.asmdef` had `includePlatforms: []` (= every platform), so its editor tools were compiled into the game. Now `["Editor"]`.
- Verify player compilation without a full build: `PlayerBuildInterface.CompilePlayerScripts` for `StandaloneWindows64` (last run: 34 assemblies, no errors).
- In release builds `PerfProbe` and the DEV panel are excluded; `Player.log` in the save folder is the console.
- Scripting backend is Mono (IL2CPP only for Android).

## Open items and likely next asks
- **Two survival clocks** (top-centre `WaveHud` and top-left `RunHud`) — ask which to keep.
- **Icons are placeholders** drawn in code; items still show as coloured squares. Offered: a TMP sprite asset of small icons (coin, heart, sword, berry) for text and item slots. TMP Settings point at an EmojiOne default sprite asset that was never imported.
- **Unused packages** (Ads Mediation, In-App Purchasing, Vivox) could be removed to shrink the build.
- **Balance is untested:** spawn curve, health growth, Brute timing, class XP curve, prices, relic/armor strength, capture speed with enemies inside.
- **Lighting:** one directional light and the default skybox; the warmer-light/sky pass was skipped.
- **Audio:** only sword swings exist; `Hit` (impacts) is the most-missed sound. The developer finds and imports packs; Claude wires them.
- Enemies only walk (no attack or death animations); Q/R skills reuse the normal swing; the Swordsman has no R skill.
- Particles don't cover skills, the Brute's slam or arrows in flight.
- Coins now buy shop items only; a stronger coin sink or more items would help.
