# Requirements & Changes Log

Tracks evolving requirements, scope updates, and implementation decisions.

---

## 2026-03-24

### Baseline Requirement Captured
- Added core tooling stack in `requirments.txt`:
  - Engine: Unity (2022 or later)
  - Language: C#
  - Tools: Cursor AI Editor, ChatGPT

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

### Run/Progression Requirement Updates
- Added run reset/death loop:
  - Run resources cleared on death
  - Player respawn
  - Enemy/resource node reset
- Upgrades refactored to be permanent across runs:
  - Upgrade levels stored separately from run resources
  - Auto-reapply on new run
  - Persistent save/load via `PlayerPrefs` + JSON

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

### UX/Game Feel Requirement Updates
- Added basic game-feel layer:
  - Screen shake on shoot and player damage
  - Particle hooks for mining hits and enemy death
  - SFX hooks for shooting, mining, and pickup
- Added lightweight UI feedback channels for prompts and extraction status.

---

## Maintenance Note
- This file should be appended whenever new requirements are added, changed, or removed.
