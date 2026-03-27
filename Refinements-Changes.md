# Refinements & Changes Log
 
Maintained as a running log of scope shifts, design decisions, and notable changes during development.
 
---
 
## Player Controller
 
- Initial movement used `rb.velocity` — updated to `rb.linearVelocity` for Unity 6 compatibility
- Input handling switched from legacy `UnityEngine.Input` to supporting **Both** input modes after new Input System conflict
- Movement axes remapped from `(h, 0, v)` to `(v, 0, -h)` then tuned to `(v, 0, h)` to align with fixed camera facing +X
- Jump force tuned down from default — original value caused player to fly off screen
- Ground check using `Physics.CheckSphere` on a child `GroundCheck` object at player feet
- Rigidbody Collision Detection set to **Continuous** to prevent mid-air collision jank
- Added `is2DMode` flag and `SetDimensionMode()` method to lock Z movement when in 2D perspective
 
## Camera System
 
- Initial camera used `TransformDirection` follow — caused jank and unpredictable rotation
- Replaced with **fixed angle follow** camera — rotation set once in `Start()`, never changes
- 3D mode: fixed `Quaternion.Euler(15f, 90f, 0f)` facing +X, follows player position with offset
- 2D mode: orthographic, side-locked, follows player X and Y only
- Smooth lerp transition between modes via coroutine in `DimensionSwitcher`
- Camera follow logic moved to `LateUpdate()` to prevent jitter
 
## Dimension Switch
 
- Switch triggered by pressing **E**
- Transition lerps camera position and rotation, fades FOV to simulate orthographic switch
- `isSwitching` bool prevents input during transition
- `DimensionSwitchEffect` added: screen flash (UI Image fade) + camera shake on switch
- Flash color exposed in Inspector — recommended cyan/blue for sci-fi feel
 
## Level Generation
 
- `PuzzleLevelBuilder` generates level procedurally on `Start()`
- Initial single floor tile stretched across entire level — caused overlap with doors and buttons
- Refactored to **segmented floor sections** with padding around each door slot
- Invisible bridge puzzle (Puzzle 3) repositioned so gap sits **before** `slotBaseX` — previously bled into next slot's space
- Puzzle 4 button Z positions changed from `corridorWidthZ * 0.5f` to `corridorWidthZ * 0.25f` — buttons were spawning near edge of floor
- `endZonePrefab` field added so end zone uses a dedicated prefab instead of reusing the floor prefab
 
## Puzzle Design
 
- **Puzzle 1 (Z-Offset):** Button offset on Z — requires 3D movement to reach
- **Puzzle 2 (Same-Z):** Button on same Z as player — rewards 2D mode sprint
- **Puzzle 3 (Invisible Bridge):** Narrow platform hidden in 2D side view — requires 3D exploration
- **Puzzle 4 (Timed Two Switches):** Two buttons far apart on Z in 3D, collapsed to X axis in 2D — requires 2D mode to complete in time window
 
## Level Progression
 
- `LevelManager` added — manages 4 levels with different puzzle orderings
- `EndZoneTrigger` on end zone prefab — detects player collision and calls `LevelManager.CompleteLevel()`
- On level complete: player teleports forward on X axis by `levelSpacingX`, level rebuilds at new position
- After level 4: game over screen shown, player Rigidbody set to kinematic to freeze movement
- `FindObjectOfType` replaced with `FindFirstObjectByType` for Unity 6 compatibility
 
## Enemy System
 
- Initial enemy system scrapped and rebuilt to use consistent prefab spawning matching floor/door/button pattern
- Spawn validation added — enemies raycast downward to confirm floor below before spawning, reject door surfaces
- NavMesh Read/Write enabled on floor mesh to fix `RuntimeNavMeshBuilder` warning
- NavMesh baked at runtime via `NavMeshSurface` on the level root object
 
## Audio
 
- `AudioManager` singleton with separate `AudioSource` components for SFX and music
- SFX generated via **ElevenLabs** (dimension switch whoosh, door open, button press)
- Background music generated via **Suno** — loops continuously from game start
- Music uses `Streaming` load type for memory efficiency
 
## Builder Inspector Modularization (Latest)
 
- Extended existing `PuzzleLevelBuilder` on the `Builder` GameObject instead of replacing it
- Added self-documenting inspector sections with `Header` and `Tooltip` attributes
- Added foldable serializable placement entries for floors, doors, and buttons
- Added per-puzzle modules via `PuzzleSectionConfig` list so each puzzle can have its own:
  - grid size
  - cell size
  - origin offset
  - floor/door/button prefab libraries
  - floor/door/button placement lists
- Added `FacingDirection` enum for door orientation (`North`, `South`, `East`, `West`)
- Added door metadata per entry (`startsOpen`, `startsLocked`, `doorId`)
- Added button-to-door linking by door ID in inspector (`targetDoorIds`, first valid link resolves to `ButtonTrigger.controlledDoor`)
- Added `[ContextMenu("Preview Layout")]` to log full planned layout (floors, doors, buttons, links) without entering Play mode
- Kept backward compatibility: if puzzle modules are empty, builder falls back to legacy single-layout fields and existing preset generation path
 
## Project Setup Snapshot (Verified)
 
- Unity version: `6000.3.8f1`
- Render pipeline: **URP** active (`com.unity.render-pipelines.universal` `17.3.0`)
- Input mode: **Both** input handlers enabled (`activeInputHandler: 2`)
- Build scenes: currently only `Assets/Scenes/SampleScene.unity`
- Package registry: default Unity registry only (`https://packages.unity.com`)
 
## Unity Packages In Use
 
- `com.unity.ai.navigation` `2.0.10`
- `com.unity.collab-proxy` `2.11.3`
- `com.unity.ide.rider` `3.0.39`
- `com.unity.ide.visualstudio` `2.0.26`
- `com.unity.inputsystem` `1.18.0`
- `com.unity.multiplayer.center` `1.0.1`
- `com.unity.render-pipelines.universal` `17.3.0`
- `com.unity.test-framework` `1.6.0`
- `com.unity.timeline` `1.8.10`
- `com.unity.ugui` `2.0.0`
- `com.unity.visualscripting` `1.9.9`
- Plus standard built-in Unity module packages
 
## Third-Party Assets / Plugins
 
- Third-party **asset content** present:
  - `SimpleSky`
  - `Free Stylized Skybox`
- No third-party code plugins found in `Assets/Plugins`
- No plugin `.dll` files found under `Assets`
- No custom `.asmdef` files found in project assets
 
## Known Issues / Remaining
- Jump ground check occasionally unreliable when brushing against walls mid-air
- Player high-poly mesh causes editor lag on lower-end hardware (Blender decimation pending)
- Main menu scene not yet implemented
