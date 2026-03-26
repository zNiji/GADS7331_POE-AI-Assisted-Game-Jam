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

### Requirements & Implementation Buildout (Merged From `requirements-changes.md`)
- Added core tooling stack in `requirements.txt`:
  - Engine: Unity (2022 or later)
  - Language: C#
  - Tools: Cursor AI Editor, ChatGPT
---
### Core Gameplay Requirement Updates
- Added modular 2D player controller with Rigidbody2D movement, jump, and directional flip.
- Added ranged shooting system with prefab bullets and enemy collision damage.
- Added mining loop:
  - Mine with `E` near nodes
  - Node health and drops
  - Pickup-to-inventory collection
- Added basic enemy AI:
  - Patrol between points
  - Detect and chase player
  - Contact damage
  - Health/death behavior
---
### Run/Progression Requirement Updates
- Added run reset/death loop:
  - Run resources cleared on death
  - Player respawn
  - Enemy/resource node reset
- Upgrades refactored to be permanent across runs:
  - Upgrade levels stored separately from run resources
  - Auto-reapply on new run
  - Persistent save/load via `PlayerPrefs` + JSON
---
### Level & Systems Requirement Updates
- Added simple level setup support:
  - Tilemap-ready workflow
  - Spawn point markers for enemies/resources
  - Auto spawner for quick iteration
- Added extraction system:
  - Call extraction by key
  - Delay to success
  - Resources banked only on successful extraction
  - Death before completion loses run resources
---
### UX/Game Feel Requirement Updates
- Added basic game-feel layer:
  - Screen shake on shoot and player damage
  - Particle hooks for mining hits and enemy death
  - SFX hooks for shooting, mining, and pickup
- Added lightweight UI feedback channels for prompts and extraction status.

### Maintenance Note
- This log should be appended whenever new requirements are added, changed, or removed.

---

## 2026-03-26

### Combat: Limited Ammo System (New Mechanic)
- Added limited-ammo gun loop:
  - New `PlayerAmmo` component to track current/max ammo and broadcast `OnAmmoChanged`.
  - `PlayerShooting` now consumes 1 ammo per shot (no ammo = no shot).
- Ammo tuning:
  - Default ammo rules shifted to **start at 80 max ammo**, with progression via upgrades.
- Added ammo pickup pipeline:
  - New `BulletAmmoPickup` pickup that restores **10 bullets** on collect.
  - Ammo pickups can drop from enemies (rarer than before).
  - Ammo pickup SFX support via `GameAudioManager.PlayAmmoPickup`.
- Bullet interaction rule:
  - Player bullets **pass through pickups** (ammo/health/oxygen) so shots aren’t destroyed by triggers.

### UI: Ammo + Upgrade Levels (HUD Improvements)
- HUD now shows ammo in the bottom-right.
- Added a bottom-right “Upgrades” readout listing current levels of permanent upgrades.
  - The label was resized and overflow enabled so longer lists (incl. ammo upgrade) are visible.

### Death/Extraction Upgrade Menu (UX + More Options)
- Death/extraction upgrade menu expanded to support **4 upgrade choices** (Option D added).
- Added “Starting Ammo” as an upgrade choice (ammo capacity progression).
- Iterated menu spacing/anchoring so:
  - “Choose one permanent upgrade…” prompt is not covered.
  - Option label text sits higher within buttons for multi-line readability.

### Progression: Starting Ammo Upgrade
- Added `upgrade.start_ammo` (“Starting Ammo”) as a permanent upgrade.
- Progression rule:
  - Starting max ammo begins at **80** and increases by **+20 per upgrade level**.

### Balance: Enemy Difficulty Scaling (Harder Mid/High Tiers)
- Reworked difficulty scaling in `LevelSetupSpawner`:
  - Increased baseline enemy health/damage multipliers.
  - Added stronger non-linear ramping so far/high-tier enemies become dramatically tougher.
  - Increased shooter enemy frequency and adjusted shooter cadence/damage scaling at higher tiers.

### New Ore: Zenithite (Ultra-Rare, Final-Tier Upgrade Gating)
- Added a new mineable ore: **Zenithite**.
- Spawn rules:
  - **10–20% chance** to appear per level (implemented as ~15% default).
  - **At most 1** Zenithite node can exist in a level at a time.
  - Prefers higher-difficulty (farther) resource spawn points.
- Visuals:
  - Added distinct procedural sprites for Zenithite nodes/items and wired them into `ResourceNode`/`ResourceItem`.
- HUD:
  - Zenithite is included in the resources readout.
- Upgrade gating:
  - The **final upgrade purchase** of each upgrade tier requires **Zenithite x1**.
  - Upgrade UIs (both base menu and death menu) display Zenithite cost when the next purchase is the final level.

### Mining: Rebalanced Node Durability + Upgrade Usefulness
- Mining upgrade feel rebalanced:
  - Early mining upgrades still require multiple hits (Iron ~5–6 hits).
  - Final mining upgrade can one-hit common ore.
  - Rarer ores take longer to mine via higher base HP and existing difficulty scaling multipliers.

### Game Feel: Mining Feedback
- Mining SFX reliability improved:
  - `GameAudioManager.PlayMine` now plays non-spatial (2D) so it is always audible.

### Stability/Flow: Extraction Cancel on Death
- Fixed edge case where extraction could complete on the next run after dying mid-countdown.
  - `ExtractionSystem` now supports explicit cancellation.
  - `GameManager.ResetRun()` forces extraction cancel to guarantee death cancels extraction.

### Startup Hitch: Reduced/Hidden Loading Freeze
- Addressed game-start hitch:
  - Prevented double-spawn by ensuring only one system triggers initial `SpawnAll`.
  - Deferred heavy scene-wide scans by a frame in `GameManager`.
  - Added a lightweight “Loading…” overlay shown during reseed/spawn and hidden afterward.
