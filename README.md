# CRYSTAL SWITCH

A 3D puzzle platformer built for a game jam where the theme was **Switch**.

---

## Concept

SWITCH is a puzzle game where the player can toggle between a full **3D perspective** and a locked **side-view "2D" perspective** at any time by pressing **E**. The twist is that switching modes also changes how the player moves — in 3D you can move on both the X and Z axes, but in 2D you are locked to the X axis only.

Every puzzle in the game is designed around this mechanic. Some puzzles are only solvable in 3D (requiring Z-axis movement to reach a button or platform). Others are only solvable in 2D (where the Z lock collapses distances and makes previously unreachable buttons accessible).

---

## How to Run

1. Clone or download the repository
2. Open the project in **Unity 6**
3. Open the main scene located at `Assets/Scenes/SampleScene.unity`
4. Press **Play** in the Unity Editor

### Build
A pre-built version can be found in the `/Build` folder (if included). Run `SWITCH.exe` on Windows.

---

## Controls

| Key | Action |
|---|---|
| W / S | Move forward / backward |
| A / D | Move left / right |
| Space | Jump |
| Left Shift | Run |
| E | Switch between 3D and 2D mode |
| Left Click | Attack |

---

## Puzzle Types

1. **Z-Offset Button** — Button is offset on the Z axis, only reachable in 3D mode
2. **Same-Z Button** — Button is on the same Z plane, sprint between it and the door in 2D mode
3. **Invisible Bridge** — A narrow platform hidden in 2D side view, only accessible in 3D
4. **Timed Two Switches** — Two buttons far apart on Z in 3D, but side-by-side in 2D — press both within the time window

---

## Credits

**Development:** [Callum Schultz]

### AI Tools Used
- **Claude (Anthropic)** — Game design, C# scripting, system architecture
- **Cursor** — In-editor AI coding assistant
- **Tripo3D** — AI-generated 3D character model
- **Mixamo** — Automatic character rigging and animation
- **Adobe Firefly** — AI-generated sound effects
- **Suno** — AI-generated background music


---

## Notes on AI Tool Usage

This project was built as part of a game jam with the goal of using as many AI tools as possible throughout the development pipeline. AI was used at every stage — from generating the player character mesh and all other assets (Tripo3D), to rigging and animating it (Mixamo), to writing all game scripts (Claude + Cursor), to generating all audio assets (Adobe Firefly + Suno). Human input was focused on design decisions, integration, and playtesting.
