# Controls

| Key | Action |
|---|---|
| WASD | Move (sprint drains stamina) |
| Auto | Attack — a 3-hit chain at 100 / 125 / 150 % damage |
| Q | Class skill, unlocks at stage level 10 (Whirlwind / Bouncing Blade) |
| R | Second skill, unlocks at stage level 5 (Assassin's Rapid Fire; Swordsman has none yet) |
| E (hold 1 s) | Pick berries from a bush |
| 1–0 | Eat a Berry from that inventory slot (refused at full health) |
| Esc | Pause (M from the pause quits to the menu, keeping progress) |
| Gear button (top-right) | Settings window — pauses the run. Sound only for now: master volume, sword swing, dagger throw. Esc or Close shuts it; volumes are remembered between sessions. |
| F1 | DEV panel (editor and Development Builds only) |
| F9 | Start / stop a performance recording (editor and Development Builds only) |

**Assassin skills** (the class was the Archer until 2026-09; same save id, so its progress carried over):
- **Bouncing Blade (Q)** — throws one dagger at the nearest enemy within 20 m. After each hit it jumps to the nearest enemy within 10 m that it has not hit yet, up to **20 targets**, 15 damage each (× ATK). With no enemy in range the key does nothing and the cooldown is not spent. Cooldown 5 s.
- **Final strike** — the third hit of the attack chain also throws a **small bouncing blade**: 60 % size, up to **5 targets**, half the Q blade's damage (× the 150 % strike × ATK). It replaced the old "one extra arrow". Rapid Fire makes final strikes come three times as often, so it multiplies these too.
- **Rapid Fire (R)** — for 6 s: +200 % attack speed (three times as fast) and daggers fly 2.2× faster. Was +100 % / 1.8×. Cooldown 15 s.

`HowToPlayMenu` is the in-game copy of these rules — update it whenever controls change.
