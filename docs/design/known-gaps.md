# Known gaps and ideas

- **Sound licences:** `Resources/Sfx/Slash/ABOUT.txt` guesses the slash sound is from Pixabay; the dagger throw (`Shoot.ogg`) has no source written down at all. Record both before sharing the game.
- The settings window exists only in the run; the main menu and pre-start screen have no way to change volume.
- **Icons are placeholders** drawn in code; items are coloured squares. A small sprite set (coin, heart, sword, berry) would replace them.
- **Lighting:** one directional light and the default skybox; a warmer-light/sky pass was skipped.
- **Audio:** only sword swings and the Assassin's dagger throw (`Shoot`) exist. `Hit` (impacts) is the most-missed sound, then `Gem`, then `EnemyDie` / `PlayerHurt` / `LevelUp` / `Coin`. Real audio files, not synthesized ones — the code needs nothing: drop `Hit.ogg` (or a `Hit/` folder of takes) into `Resources/Sfx/`, then Stop → Play. kenney.nl audio packs are CC0. Once `Gem` has a file: make quick pickups climb in pitch (a few lines in `ExperienceGem`, using `GameAudio.Play`'s pitch). No music yet.
- Enemies only walk (no attack or death animations); Q/R skills reuse the normal swing; the Swordsman has no R skill.
- Particles don't cover skills, the Brute's slam, or daggers in flight.
- `Player/SwordProp` and `Player/BowProp` are switched-off leftovers from before the pack models (cube placeholders). The Assassin's model (`prf_Set Costume02 UTC WTD`) already holds twin daggers and plays the `WTD` clips; nothing bow-shaped is left. The two leftovers can be deleted.
- Performance ideas if it ever stutters: dormant camps (spawn guards only when the hero is near) and letting idle enemy rigidbodies sleep. Pooling enemies like the pickups is possible but **measure first** (F9 before and after): the first measurement said spawning was not the cost, and a pooled enemy must reset health, `Role`, lock-on, hit flash and the alive counter.
