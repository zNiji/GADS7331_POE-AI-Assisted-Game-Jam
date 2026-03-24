# Refinements & Changes Log

Tracks scope shifts, design decisions, and implementation changes over time.

---

## 2026-03-24

### Pre-Development Brainstorm Baseline
- The milestone roadmap and task checklist in `plan.md` are documented as the initial brainstorming baseline created before development started.
- Future scope decisions should be compared against this baseline to track what changed during implementation.

### Initial Structure Decision
- Established a modular Unity project layout under `Assets`:
  - `Player`, `Enemies`, `Resources`, `UI`, `Systems`
  - Supporting folders: `Core`, `Scenes`, `Tiles`
- Chosen approach: keep gameplay systems separated by domain to reduce coupling and speed up iteration during game jam development.

### Physics/Controller Direction
- Standardized on `Rigidbody2D` + `Collider2D` for player/enemy interactions.
- Avoided transform-based movement in favor of physics-safe velocity control.
- Updated player controller to include:
  - Smooth horizontal movement (accel/decel)
  - Ground-checked jumping
  - Sprite flip based on facing direction

### Combat Scope Shift
- Started with melee-capable player combat (`PlayerCombat`) for quick baseline testing.
- Expanded scope to include ranged shooting system:
  - Added `PlayerShooting`
  - Added modular `Bullet` behavior (forward motion, timed self-destruction, collision damage)
  - Added dedicated `EnemyHealth` component implementing `IDamageable`
- Design decision: keep melee and shooting as separate components so either/both can be used per character.

### UI/Theme Direction
- Theme direction set to: pixel-art astronaut on alien planet collecting resources.
- HUD shifted from generic inventory/health text to thematic survival HUD:
  - Suit integrity
  - Oxygen tracking
  - Resource counters
  - Biome label
  - Prompt text
  - Pause panel support
- Added `PlayerStats` to drive suit/oxygen state and broadcast UI update events.

### Planning & Process
- Added `plan.md` with milestone roadmap and task checklist.
- Decision: maintain this log continuously for all future refinements, including:
  - Scope additions/removals
  - System rewrites
  - Major tuning decisions
  - UX/UI changes
