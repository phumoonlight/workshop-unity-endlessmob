# Building and sharing the game

## Build
1. Open `game/` in Unity Hub.
2. **File → Build Profiles → Windows** (the profile asset is committed). Leave **Development Build off** for a build you share.
3. Build **outside the repo**, or into `game/Builds/` (gitignored). Never into a folder git would pick up.
4. If Unity asks about a "Missing Project ID", answer **Yes** — the game uses no Unity online services.

The output is `EndlessMobPoc.exe` (product name **EndlessMobPoc**, company **Iampumon**, set in Player Settings). Scripting backend is Mono.

## Share
- Zip the build folder **except** `EndlessMobPoc_BackUpThisFolder_ButDontShipItWithYourGame`. That folder is Burst's debug data — keep it next to that build's zip so crash reports can be decoded, but never ship it.
- Windows SmartScreen warns on an unsigned game. Players click "More info → Run anyway".
- There is no installer by choice. Inno Setup is the option if one is ever wanted.

## Where a built game writes
- Save file: `%USERPROFILE%\AppData\LocalLow\Iampumon\EndlessMobPoc\profile.json` (delete it to reset progress).
- Console output: `Player.log` in the same folder.
- The editor uses `...\Iampumon\EndlessMobPocDev\` instead, so testing never touches real progress.

## Release vs development builds
- In release builds, the F9 performance probe and the F1 DEV panel are compiled out.
- A Development Build keeps both, plus the profiler connection.

## Checks without a full build
- `PlayerBuildInterface.CompilePlayerScripts` for `StandaloneWindows64` verifies player compilation in seconds (last run: 34 assemblies, no errors).
- Past build blocker, now fixed: `Packages/com.unity.springbone/Editor/*.asmdef` had `includePlatforms: []` (= every platform), which compiled editor tools into the game. It is now `["Editor"]`.
