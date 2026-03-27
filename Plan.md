# Development Plan — CRYSTAL SWITCH

## Concept
A 3D puzzle platformer where the player can switch between a full 3D perspective and a locked side-view "2D" perspective. The core mechanic is that switching between modes changes the player's available movement axes (X+Z in 3D, X only in 2D), enabling puzzles that are only solvable in one mode.

---

## Milestones

### Milestone 1 — Core Player & Camera ✅
- [x] Player character imported from Tripo3D, rigged via Mixamo
- [x] Animations: Idle, Walk, Run, Jump imported and configured in Animator
- [x] PlayerController script with Rigidbody movement
- [x] Dimension switch mechanic (3D ↔ 2D mode)
- [x] DimensionSwitcher camera script (fixed angle, smooth transition)

### Milestone 2 — Puzzle Systems ✅
- [x] PuzzleLevelBuilder — procedural level generation
- [x] DoorController — doors that open via buttons
- [x] ButtonTrigger — pressure plate / trigger system
- [x] Timed two-switch door mechanic (Puzzle 4)
- [x] 4 puzzle types designed and implemented

### Milestone 3 — Enemy System ❌
- [x] EnemyAI — patrol and chase states using NavMesh
- [x] PlayerAttack — attack mechanic with OverlapSphere detection
- [x] Enemy despawn on hit
- [x] Enemy spawn validation (floor only, not on doors)

### Milestone 4 — Level Progression ✅
- [x] LevelManager — 4 levels with randomised puzzle order
- [x] EndZoneTrigger — loads next level on collision
- [x] Game over screen on completing level 4

### Milestone 5 — Polish ✅
- [x] DimensionSwitchEffect — screen flash + camera shake on switch
- [x] AudioManager — SFX for dimension switch, door open, button press
- [x] Background music (looping)
- [ ] Main menu scene
- [ ] Post processing (Bloom, Vignette, Color Grading)

---

## AI Tools Used

| Tool | Purpose |
|---|---|
| Claude (Anthropic) | Game design, all C# scripting, system architecture |
| Cursor | In-editor AI coding, script generation, bug fixing |
| Tripo3D | AI-generated 3D character model |
| Mixamo | Automatic character rigging and animation |
| ElevenLabs | AI-generated sound effects |
| Suno | AI-generated background music |
| GitHub Copilot | In-editor code completion |

---

## Task List (Remaining)
- [ ] Main menu with Play button
- [ ] Win screen UI
- [ ] Post processing volume setup
- [ ] Build and export
