# Endless Mob — game design

A 3D Vampire-Survivors-style action game. Survive as long as you can while enemies pour in from off-screen, clear camps for loot, and grow your hero between runs.

## The loop
1. **Main menu** — PLAY, HOW TO PLAY, QUIT.
2. **Pre-start** — pick a hero (Swordsman or Assassin), see each class's level and your coins, visit the SHOP, then START.
3. **The run** — enemies spawn endlessly and rush you; the longer you last, the faster and tougher they get. Dying ends the run.
4. **After the run** — the run's coins, items and XP are banked. The result panel has two buttons: **TRY AGAIN** retries with the same hero, **MAIN MENU** returns to the main menu.

It used to be a **castle-defense game with waves**. That mode is shelved, not deleted — see [Shelved mode](#shelved-mode).

## Controls
| Key | Action |
|---|---|
| WASD | Move (sprint drains stamina) |
| Auto | Attack — a 3-hit chain at 100 / 125 / 150 % damage |
| Q | Class skill, unlocks at stage level 10 (Whirlwind / Bouncing Blade) |
| R | Second skill, unlocks at stage level 5 (Assassin's Rapid Fire; Swordsman has none yet) |
| E (hold 1 s) | Pick berries from a bush |
| 1–0 | Eat a Berry from that inventory slot (refused at full health) |
| Esc | Pause (M from the pause quits to the menu, keeping progress) |
| F1 | DEV panel (editor and Development Builds only) |
| F9 | Start / stop a performance recording (editor and Development Builds only) |

**Assassin skills** (the class was the Archer until 2026-09; same save id, so its progress carried over):
- **Bouncing Blade (Q)** — throws one dagger at the nearest enemy within 20 m. After each hit it jumps to the nearest enemy within 10 m that it has not hit yet, up to **20 targets**, 15 damage each (× ATK). With no enemy in range the key does nothing and the cooldown is not spent. Cooldown 5 s.
- **Final strike** — the third hit of the attack chain also throws a **small bouncing blade**: 60 % size, up to **5 targets**, half the Q blade's damage (× the 150 % strike × ATK). It replaced the old "one extra arrow". Rapid Fire makes final strikes come three times as often, so it multiplies these too.
- **Rapid Fire (R)** — for 6 s: +200 % attack speed (three times as fast) and daggers fly 2.2× faster. Was +100 % / 1.8×. Cooldown 15 s.

`HowToPlayMenu` is the in-game copy of these rules — update it whenever controls change.

## Enemies
- Spawn on a ring just past the screen corners, 0.7 per second at the start, rising 0.45/s per minute up to 6/s. Never more than 120 spawned enemies alive.
- Mix: 85 % melee, 10 % fast, 5 % ranged. A **Brute** arrives at 3:00 and every 2:00 after.
- Enemy health grows +8 % per minute (camps use the same multiplier).
- Enemies that fall far behind are teleported back to the edge, so pressure never drops.
- Spawned enemies hunt the player only; they ignore buildings.

## Camps
- The map starts with 4 small / 2 medium / 1 large camp. Caps: 8 / 4 / 2. While below its cap a size respawns every 60 s / 180 s / 300 s.
- Large camps are placed on a map side that has none, at least 45 m from the hero and off-screen. Each new large camp is one tier tougher than the last one cleared.
- Medium and large camps gain +1 guard per minute (paused while the world is full). Large camps only wake up when the hero is within 40 m.
- **Capture:** kill the guards, then stand in the circle. The gauge only rises — full speed alone, ×0.35 with enemies inside, frozen while you are outside.

| Size | Radius | Capture time | Coins | Gems |
|---|---|---|---|---|
| Small | 4 m | 2.5 s | 8 | 5 |
| Medium | 6 m | 5 s | 25 | 12 |
| Large | 9 m | 10 s | 60 | 30 |

Loot is scattered on the spot (no chest). A large camp's tower switches off the moment capture begins.

## Progression
- **Stage level** (in-run): XP gems and camps give stage XP; levels are cheap (5, 10, 15 … XP). Each gives +1 % attack and +1 % max HP, and skills unlock by stage level. Gone when the run ends.
- **Class level** (kept): all stage XP from a run is poured into that class's XP (300 XP for the first level, +200 per level after — first guesses). Your next run with that class **starts at its class level**, with all the stat bonuses of the skipped levels.
- **Coins and items** are banked on every kind of run end — defeat, quitting, closing the window. The wallet starts every run at 0.
- Progress saves itself; there is no save button. See [architecture.md](architecture.md#saving) for the file location and how to reset.

## Items and shop
| Item | Buy / Sell | Effect |
|---|---|---|
| Berry | 10 / 5 | Heals when eaten (1–0 keys). Picked from bushes. |
| Relic | 100 / 50 | Instead of dying, one is consumed and you rise at 50 % HP with 2 s of invulnerability. |
| Armor | 500 / 250 | +25 max HP for each one carried. |

The shop (pre-start screen) checks for stash room **before** taking coins. Coins currently buy shop items only — a stronger coin sink would help.

## Shelved mode
The castle-defense mode is switched off but its code and scene objects remain: the Castle with corner archers and upgrades, wave manager, castle health bar, chest spawner, wilderness scouts, fog of war, building (Barracks, Towers, Builder huts, outposts). See [architecture.md](architecture.md#shelved-systems) for what is disabled where.

## Known gaps and ideas
- **Two survival clocks** show (top-centre and top-left) — decide which to keep.
- **Icons are placeholders** drawn in code; items are coloured squares. A small sprite set (coin, heart, sword, berry) would replace them.
- **Balance is untested:** spawn curve, health growth, Brute timing, class XP curve, prices, Relic/Armor strength, capture speed with enemies inside.
- **Lighting:** one directional light and the default skybox; a warmer-light/sky pass was skipped.
- **Audio:** only sword swings exist. `Hit` (impacts) is the most-missed sound. Real audio files, not synthesized ones.
- Enemies only walk (no attack or death animations); Q/R skills reuse the normal swing; the Swordsman has no R skill.
- Particles don't cover skills, the Brute's slam, or daggers in flight.
- The Assassin still **holds a bow and plays the bow animations** while throwing daggers. The Haons pack has twin-dagger props and a `WeaponMaster Twin dagger(WTD)` animation set that could replace them.
- Performance ideas if it ever stutters: dormant camps (spawn guards only when the hero is near) and letting idle enemy rigidbodies sleep.
