# Enemies

- Spawn on a ring just past the screen corners, 0.7 per second at the start, rising 0.45/s per minute up to 6/s. Never more than 120 spawned enemies alive.
- Mix: 85 % melee, 10 % fast, 5 % ranged. A **Brute** arrives at 3:00 and every 2:00 after.
- Enemy health grows +8 % per minute (camps use the same multiplier).
- Enemies that fall far behind are teleported back to the edge, so pressure never drops.
- Spawned enemies hunt the player only; they ignore buildings.
- **Melee hits are swings, not contact.** In reach (1.2 m) an enemy stops and swings; the hit lands 0.4 s later, so stepping out of reach dodges it. 0.6 s recovery, then it chases or swings again. One hit = 10 (a Brute's ordinary hit is 8, on top of its slam).
