# workshop-unity-endlessmob

**Endless Mob** — a 3D Vampire-Survivors-style game in Unity 6000.6.2f1 (URP, Input System). Product name `EndlessMobPoc`, company `Iampumon`.

The developer is **new to game development**: explain simply, keep changes small, teach the *why* behind each Unity idea.

## Repository layout
- `game/` — the Unity project (open this folder in Unity Hub). Has its own `.gitignore`.
- `docs/` — human documentation, outside the project so Unity doesn't import it:
  - `docs/game-design.md` — rules, controls, tuning numbers, known gaps
  - `docs/architecture.md` — how each system is coded, saving, HUD, performance, shelved code
  - `docs/building.md` — build and share the .exe
  - `docs/learning-log.md` — topics covered with the developer

**Read the relevant doc before touching a system**, and update it when the system changes. Player-facing rules also live in `HowToPlayMenu` (plain strings) — keep both in sync.

## Where things are (paths relative to `game/`)
- `Assets/_Game/Scripts/` — all code: `Core`, `Player`, `Weapons`, `Skills`, `Enemies`, `Progression`, `City`, `Inventory`, `UI`, `Camera`
- `Assets/_Game/Prefabs/`, `Data/` (ScriptableObjects), `Resources/` (`Sfx/`, `Vfx/`, `Items/` — loaded by name), `Animation/`, `Settings/`, `Scenes/` (`MainMenu` 0, `PreStart` 1, `Game` 2)
- `Assets/Haons SD series Pack/` — third-party art. **Read from it; never edit it.**
- `Packages/com.unity.springbone/` — embedded package, committed.

Claude's working directory is the **repo root**: shell, Grep and file-tool paths need the `game/` prefix. Paths passed to Unity APIs inside editor scripts stay project-relative (`Assets/…`).

## Working with Unity
- The **Unity editor must be open**. Drive it with the Unity CLI: `$LOCALAPPDATA/Unity/bin/unity.exe` (package `com.unity.pipeline`, port 7800). `unity status` / `unity command editor_status`.
- Workflow: edit scripts → `unity command recompile`, poll `recompile_status` → wire scenes/prefabs with an `eval_file` script → `console_status` (`console --level error` for stack traces) → review → commit.
- **Check `editor_status` for play mode first.** Saving scenes, creating assets and editing prefabs fail during play; ask the developer to press Stop.
- **Keep token use low:** no screenshots, no playtesting. Compile-check, then the developer playtests and sends screenshots. State plainly what wasn't seen on screen.
- Verify player compilation without a full build: `PlayerBuildInterface.CompilePlayerScripts` for `StandaloneWindows64`.

### eval_file scripts
- Live in the session scratchpad, never the repo. File ends in `.cs` and is a **method body**: no `using`, fully qualify everything (`UnityEditor.AssetDatabase`, `UnityEngine.Object` — plain `Object` is ambiguous).
- No lambdas; local helper methods after the final `return` work.
- Edit any scene, open or not: `SceneManager.GetSceneByPath`; if not loaded, `EditorSceneManager.OpenScene(path, OpenSceneMode.Additive)`, then save and `CloseScene`.
- Private `[SerializeField]`s: `new UnityEditor.SerializedObject(c).FindProperty("name")` + `ApplyModifiedPropertiesWithoutUndo()`. Set TMP text via `m_text` the same way.
- Finish with `MarkSceneDirty` + save + `AssetDatabase.SaveAssets()`; `return` a short log string. Make scripts re-runnable (delete the object/asset first if it exists).
- **Calls over ~5 s time out** but the work finishes — write results to a file and poll.
- `EditorBuildSettings.scenes` reaches disk only after `unity command save_all`.
- Use `FindAnyObjectByType` (with `FindObjectsInactive.Include` for disabled objects), not the deprecated `FindFirstObjectByType`.
- Private statics via reflection are fine for tests (e.g. `PlayerProfile.Load` round-trips); restore the developer's save file afterwards.

### Editing files from the shell
- Backslashes get mangled by Python heredocs (`\n`, `\U`) and `sed` (`\U`/`\L`). **Anything containing backslashes: use the Edit tool.**
- `assert old in s` before any string replacement.

## Unity rules learned the hard way
- `Random` and most Unity APIs throw in field initializers and abandon the object's setup — initialise in `Awake`.
- `Start`, `Update`, `Awake`, `OnEnable`, `Reset` … are Unity messages; never name a helper after one.
- Domain reload is **off**: statics survive between Play presses. Reset them with `[RuntimeInitializeOnLoadMethod]`.
- Unity 6 empties every `ObjectPool` when leaving Play mode after the scene is destroyed — pool callbacks must null-check.
- A disabled component's `Awake` still runs. `== null` is true for destroyed objects.
- Renaming a serialized field needs `[FormerlySerializedAs("old")]` or Inspector values reset.
- `Time.timeScale == 0` means **paused** here; `HitStop` slows to 5 %, never 0.
- Save ids (`ItemData.id`, `PlayerClassData.id`) are permanent — **never change or reuse one**; bump `PlayerProfile.SaveVersion` when the save layout changes.
- Enemy renderers must be **destroyed, not disabled** (`Enemy.Update` re-enables them).

## Code conventions
- Short, beginner-friendly comments explaining *why*, like the existing scripts.
- `[SerializeField]` private fields with `[Tooltip]` for anything worth tuning.
- Shared bases: `Weapon`, `ClassSkill`, `Structure`, `MagnetPickup`. Runtime-loaded things go in `Resources/` so adding one needs no wiring.
- Menus are built in code with `UiFactory`; new panels call `SetAsLastSibling()` when opening.
- Data assets: **Create > Endless Mob > …**.

## Git
- `.git` is at the repo root; the Unity `.gitignore` stays in `game/` (its `/Library/`-style patterns are relative to its own folder).
- Commit everything under `Assets/` **with its `.meta`**. Embedded `Packages/<name>/` are committed; registry packages are one line in `manifest.json`.
- **Review `git status` before `git add -A`** — Unity rewrites `ProjectSettings.asset`, URP prefilter data and leaves build residue. Commit only what belongs.
- No LFS; GitHub rejects files over 100 MB (largest file today ≈ 4 MB).
- Commit after each working change, with a message saying what and why.
