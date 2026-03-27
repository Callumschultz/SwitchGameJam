using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class PuzzleLevelBuilder : MonoBehaviour
{
    /*
     * Puzzle Builder Inspector Guide
     * - Floor Configuration: Grid sizing, floor prefab library, and floor placements.
     * - Door Configuration: Door prefab library and door placements with facing/state.
     * - Button Configuration: Button prefab library and button placements with door links.
     * 
     * Notes:
     * - "Use Inspector Layout" switches level generation from the built-in preset path
     *   to the inspector-driven placement lists below.
     * - Legacy prefab fields remain for backward compatibility with existing scene data.
     */
    public enum FacingDirection
    {
        North,
        South,
        East,
        West
    }

    [Serializable]
    public class FloorPlacementEntry
    {
        [Tooltip("Grid cell for this floor tile/section (X = width axis, Y = height axis).")]
        public Vector2Int gridPosition;

        [Tooltip("Index into the Floor Prefabs list. Ignored when Override Prefab is assigned.")]
        public int prefabIndex;

        [Tooltip("Optional direct prefab override for this entry.")]
        public GameObject overridePrefab;

        [Tooltip("Optional non-uniform local scale applied after spawning.")]
        public Vector3 localScale = Vector3.one;
    }

    [Serializable]
    public class DoorPlacementEntry
    {
        [Tooltip("Designer ID used for linking button targets.")]
        public string doorId = "Door_01";

        [Tooltip("Grid cell for this door.")]
        public Vector2Int gridPosition;

        [Tooltip("Index into the Door Prefabs list. Ignored when Override Prefab is assigned.")]
        public int prefabIndex;

        [Tooltip("Optional direct prefab override for this entry.")]
        public GameObject overridePrefab;

        [Tooltip("Door facing direction in the grid.")]
        public FacingDirection facing = FacingDirection.North;

        [Tooltip("If enabled, the door starts open.")]
        public bool startsOpen;

        [Tooltip("If enabled, the door starts locked (design metadata).")]
        public bool startsLocked;
    }

    [Serializable]
    public class ButtonPlacementEntry
    {
        [Tooltip("Grid cell for this button.")]
        public Vector2Int gridPosition;

        [Tooltip("Index into the Button Prefabs list. Ignored when Override Prefab is assigned.")]
        public int prefabIndex;

        [Tooltip("Optional direct prefab override for this entry.")]
        public GameObject overridePrefab;

        [Tooltip("Door IDs this button controls. IDs must match Door Placement entries.")]
        public List<string> targetDoorIds = new List<string>();

        [Tooltip("If enabled, this button can only be used in 2D mode.")]
        public bool require2DMode;
    }

    [Serializable]
    public class PuzzleSectionConfig
    {
        [Tooltip("Label used in logs and object naming.")]
        public string puzzleName = "Puzzle";

        [Tooltip("Enable/disable this puzzle module without deleting its data.")]
        public bool enabled = true;

        [Header("Floor Configuration")]
        [Tooltip("Puzzle floor dimensions in grid cells (X = width, Y = height).")]
        public Vector2Int gridSize = new Vector2Int(12, 4);

        [Tooltip("World-space size of each grid cell (X maps to world X, Y maps to world Z).")]
        public Vector2 cellSize = new Vector2(2f, 2f);

        [Tooltip("World-space offset applied to this puzzle's grid origin from player position.")]
        public Vector2 gridOriginOffset = new Vector2(-2f, -2f);

        [Tooltip("Prefab library for floor placements in this puzzle.")]
        public List<GameObject> floorPrefabs = new List<GameObject>();

        [Tooltip("Per-entry floor placements for this puzzle.")]
        public List<FloorPlacementEntry> floorPlacements = new List<FloorPlacementEntry>();

        [Header("Door Configuration")]
        [Tooltip("Prefab library for door placements in this puzzle.")]
        public List<GameObject> doorPrefabs = new List<GameObject>();

        [Tooltip("Per-entry door placements for this puzzle.")]
        public List<DoorPlacementEntry> doorPlacements = new List<DoorPlacementEntry>();

        [Header("Button Configuration")]
        [Tooltip("Prefab library for button placements in this puzzle.")]
        public List<GameObject> buttonPrefabs = new List<GameObject>();

        [Tooltip("Per-entry button placements for this puzzle.")]
        public List<ButtonPlacementEntry> buttonPlacements = new List<ButtonPlacementEntry>();
    }

    [Header("Layout Mode")]
    [Tooltip("When enabled, this builder uses the inspector placement lists instead of the built-in preset layout.")]
    [SerializeField] private bool useInspectorLayout;

    [Header("Puzzle Modules")]
    [Tooltip("Each puzzle has its own floor/door/button config and independent sizing.")]
    [SerializeField] private List<PuzzleSectionConfig> puzzleSections = new List<PuzzleSectionConfig>();

    [Header("Legacy Single Layout (Fallback)")]
    [Tooltip("Puzzle floor dimensions in grid cells (X = width, Y = height).")]
    [SerializeField] private Vector2Int gridSize = new Vector2Int(12, 4);

    [Tooltip("World-space size of each grid cell (X maps to world X, Y maps to world Z).")]
    [SerializeField] private Vector2 cellSize = new Vector2(2f, 2f);

    [Tooltip("World-space offset applied to the generated grid origin from the player position.")]
    [SerializeField] private Vector2 gridOriginOffset = new Vector2(-2f, -2f);

    [Tooltip("Prefab library for floor placements.")]
    [SerializeField] private List<GameObject> floorPrefabs = new List<GameObject>();

    [Tooltip("Per-entry floor placements.")]
    [SerializeField] private List<FloorPlacementEntry> floorPlacements = new List<FloorPlacementEntry>();

    [Header("Door Configuration")]
    [Tooltip("Prefab library for door placements.")]
    [SerializeField] private List<GameObject> doorPrefabs = new List<GameObject>();

    [Tooltip("Per-entry door placements.")]
    [SerializeField] private List<DoorPlacementEntry> doorPlacements = new List<DoorPlacementEntry>();

    [Header("Button Configuration")]
    [Tooltip("Prefab library for button placements.")]
    [SerializeField] private List<GameObject> buttonPrefabs = new List<GameObject>();

    [Tooltip("Per-entry button placements.")]
    [SerializeField] private List<ButtonPlacementEntry> buttonPlacements = new List<ButtonPlacementEntry>();

    [Header("Prefabs")]
    [Tooltip("Legacy default floor prefab used by the built-in preset layout and as fallback.")]
    [SerializeField] private GameObject floorPrefab;
    [Tooltip("Legacy default door prefab used by the built-in preset layout and as fallback.")]
    [SerializeField] private GameObject doorPrefab;
    [Tooltip("Legacy default button prefab used by the built-in preset layout and as fallback.")]
    [SerializeField] private GameObject buttonPrefab;

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Build Settings")]
    [SerializeField] private bool rebuildOnPlay = true;

    private const string RootName = "PuzzleRuntimeLayout";

    private void Start()
    {
        if (!rebuildOnPlay)
        {
            return;
        }

        BuildLevel();
    }

    private void BuildLevel()
    {
        // Always use the tagged Player at runtime.
        // (This prevents issues if the inspector reference is accidentally wrong.)
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        if (player == null)
        {
            Debug.LogError("PuzzleLevelBuilder could not find a Player transform (tagged 'Player').", this);
            return;
        }

        // Ensure spawned surfaces are on the same layer the player uses for ground checks.
        int groundLayerIndex = ResolvePlayerGroundLayerIndex();

        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null)
        {
            Destroy(oldRoot);
        }

        GameObject root = new GameObject(RootName);

        if (useInspectorLayout)
        {
            BuildInspectorLayout(root, player.position, groundLayerIndex);
            BuildNavMesh(root);
            return;
        }

        if (floorPrefab == null || doorPrefab == null || buttonPrefab == null)
        {
            Debug.LogError("PuzzleLevelBuilder is missing one or more legacy prefab references.", this);
            Destroy(root);
            return;
        }

        float y = player.position.y - 0.5f;
        float baseX = player.position.x;
        float baseZ = player.position.z;

        // Layout is built relative to the player so the puzzles are always near where you start.
        float startX = baseX - 2f;
        float mainFloorLengthX = 74f;
        float corridorWidthZ = 24f;

        // Puzzle 3 (Invisible Bridge) uses a full-width floor gap over which a narrow bridge floats.
        float invisibleGapStartX = startX + 34f;
        float invisibleGapEndX = startX + 44f;
        float invisibleBridgeLengthX = invisibleGapEndX - invisibleGapStartX;
        float invisibleBridgeWidthZ = 1.2f;
        float invisibleBridgeZCenter = baseZ + (corridorWidthZ * 0.5f) - (invisibleBridgeWidthZ * 0.5f) - 0.6f;

        float floor1LengthX = invisibleGapStartX - startX;
        float floor2StartX = invisibleGapEndX;
        float floor2LengthX = mainFloorLengthX - (floor2StartX - startX);

        // Slightly wider than the floor so the player can't "sneak around" the wall in Z.
        float doorZScale = corridorWidthZ + 0.25f;

        // Floor 1 (covers everything up to the invisible bridge gap).
        GameObject floor1 = Instantiate(
            floorPrefab,
            new Vector3(startX + floor1LengthX * 0.5f, y, baseZ),
            Quaternion.identity,
            root.transform);
        floor1.name = "PuzzleFloor";
        floor1.transform.localScale = new Vector3(floor1LengthX, 0.1f, corridorWidthZ);
        SetLayerRecursively(floor1, groundLayerIndex);

#if UNITY_EDITOR
        Debug.Log($"PuzzleLevelBuilder: resolved ground layer index = {groundLayerIndex}. Spawned PuzzleFloor layer = {floor1.layer}.", this);
#endif

        // Snap the player onto the generated floor so enabling gravity doesn't make them fall through.
        SnapPlayerOntoFloor(floor1, player);

        float floorTopY = GetTopY(floor1);

        // Floating narrow bridge (only exists inside the invisible bridge gap).
        GameObject invisibleBridgeFloor = Instantiate(
            floorPrefab,
            new Vector3(invisibleGapStartX + invisibleBridgeLengthX * 0.5f, y, invisibleBridgeZCenter),
            Quaternion.identity,
            root.transform);
        invisibleBridgeFloor.name = "InvisibleBridgeFloor";
        invisibleBridgeFloor.transform.localScale = new Vector3(invisibleBridgeLengthX, 0.1f, invisibleBridgeWidthZ);
        SetLayerRecursively(invisibleBridgeFloor, groundLayerIndex);

        // Floor 2 (covers everything after the invisible bridge gap).
        GameObject floor2 = Instantiate(
            floorPrefab,
            new Vector3(floor2StartX + floor2LengthX * 0.5f, y, baseZ),
            Quaternion.identity,
            root.transform);
        floor2.name = "PuzzleFloor_Continue";
        floor2.transform.localScale = new Vector3(floor2LengthX, 0.1f, corridorWidthZ);
        SetLayerRecursively(floor2, groundLayerIndex);

        // Ensure the player has the attack component so clicking immediately works.
        if (player.GetComponentInChildren<PlayerAttack>() == null)
        {
            player.gameObject.AddComponent<PlayerAttack>();
        }

        // Door 1 blocks forward X path. Button is offset on Z to force 3D movement.
        DoorController door1 = CreateDoor(new Vector3(startX + 8f, y + 1f, baseZ), root.transform, "Door_Puzzle1", doorZScale, floorTopY);
        SetLayerRecursively(door1.gameObject, groundLayerIndex);
        CreateButton(new Vector3(startX + 4f, y + 0.15f, baseZ + 3f), root.transform, "Button_Puzzle1", door1, floorTopY);

        // Door 2 blocks later path. Button stays on same Z to reward 2D lock movement.
        DoorController door2 = CreateDoor(new Vector3(startX + 20f, y + 1f, baseZ), root.transform, "Door_Puzzle2", doorZScale, floorTopY);
        SetLayerRecursively(door2.gameObject, groundLayerIndex);
        CreateButton(new Vector3(startX + 16f, y + 0.15f, baseZ), root.transform, "Button_Puzzle2", door2, floorTopY);

        // Puzzle 3: Door can only be opened in 2D mode.
        DoorController door3 = CreateDoor(new Vector3(startX + 30f, y + 1f, baseZ), root.transform, "Door_Puzzle3", doorZScale, floorTopY);
        SetLayerRecursively(door3.gameObject, groundLayerIndex);
        ButtonTrigger button3 = CreateButtonWithTrigger(
            new Vector3(startX + 26f, y + 0.15f, baseZ),
            root.transform,
            "Button_Puzzle3",
            door3,
            floorTopY);
        button3.require2DMode = true;

        // Puzzle 3 (Invisible Bridge): door opens via a button on the floating narrow bridge.
        float invisibleDoorX = invisibleGapEndX + 3f;
        DoorController invisibleDoor = CreateDoor(
            new Vector3(invisibleDoorX, y + 1f, baseZ),
            root.transform,
            "Door_PuzzleInvisibleBridge",
            doorZScale,
            floorTopY);
        SetLayerRecursively(invisibleDoor.gameObject, groundLayerIndex);

        CreateButton(
            new Vector3(invisibleGapEndX - 2.2f, y + 0.15f, invisibleBridgeZCenter),
            root.transform,
            "Button_PuzzleInvisibleBridge",
            invisibleDoor,
            floorTopY);

        // Puzzle 4 (Two Switches): timed two-button sequence opens the exit door.
        float twoSwitchDoorX = invisibleDoorX + 12f;
        DoorController twoSwitchDoor = CreateDoor(
            new Vector3(twoSwitchDoorX, y + 1f, baseZ),
            root.transform,
            "Door_PuzzleTwoSwitches",
            doorZScale,
            floorTopY);
        twoSwitchDoor.SetMode(DoorController.DoorMode.TimedTwoSwitches);
        // Give 2D a fair window; 3D will be blocked by require2DMode on the buttons.
        twoSwitchDoor.ConfigureTimedTwoSwitches(windowSeconds: 1.4f, requiredButtons: 2);
        SetLayerRecursively(twoSwitchDoor.gameObject, groundLayerIndex);

        float buttonAX = twoSwitchDoorX - 8f;
        float buttonBX = twoSwitchDoorX - 5f;
        // In 3D mode these buttons sit far apart on Z.
        // In 2D mode `ButtonTrigger.collapseToPlayerZIn2D` snaps them close together.
        float buttonA_Z = baseZ - (corridorWidthZ * 0.5f) + 0.8f;
        float buttonB_Z = baseZ + (corridorWidthZ * 0.5f) - 0.8f;

        ButtonTrigger buttonA = CreateButtonWithTrigger(
            new Vector3(buttonAX, y + 0.15f, buttonA_Z),
            root.transform,
            "Button_PuzzleTwoSwitches_A",
            twoSwitchDoor,
            floorTopY);
        buttonA.require2DMode = true;
        buttonA.collapseToPlayerZIn2D = true;

        ButtonTrigger buttonB = CreateButtonWithTrigger(
            new Vector3(buttonBX, y + 0.15f, buttonB_Z),
            root.transform,
            "Button_PuzzleTwoSwitches_B",
            twoSwitchDoor,
            floorTopY);
        buttonB.require2DMode = true;
        buttonB.collapseToPlayerZIn2D = true;

        // End zone marker near level end.
        GameObject endZone = Instantiate(
            floorPrefab,
            new Vector3(twoSwitchDoorX + 7f, y + 0.03f, baseZ),
            Quaternion.identity,
            root.transform);
        endZone.name = "EndZone";
        endZone.transform.localScale = new Vector3(2f, 0.05f, 2f);
        SetLayerRecursively(endZone, groundLayerIndex);

        BuildNavMesh(root);
    }

    [ContextMenu("Preview Layout")]
    private void PreviewLayout()
    {
        Debug.Log($"PuzzleLevelBuilder Preview | UseInspectorLayout={useInspectorLayout} | ModuleCount={puzzleSections.Count}", this);

        if (!useInspectorLayout)
        {
            Debug.Log("Preview Layout: currently using built-in preset layout path (set 'Use Inspector Layout' to true for list-driven layout).", this);
            return;
        }

        if (puzzleSections != null && puzzleSections.Count > 0)
        {
            for (int moduleIndex = 0; moduleIndex < puzzleSections.Count; moduleIndex++)
            {
                PreviewSection(puzzleSections[moduleIndex], moduleIndex, useLegacyNames: false);
            }
            return;
        }

        PreviewLegacySingleLayout();
    }

    private void BuildInspectorLayout(GameObject root, Vector3 playerPosition, int groundLayerIndex)
    {
        if (puzzleSections != null && puzzleSections.Count > 0)
        {
            for (int i = 0; i < puzzleSections.Count; i++)
            {
                BuildInspectorSection(
                    puzzleSections[i],
                    moduleIndex: i,
                    sectionLabel: $"Module_{i}",
                    root: root,
                    playerPosition: playerPosition,
                    groundLayerIndex: groundLayerIndex,
                    useLegacyFallbackFields: true);
            }
            return;
        }

        PuzzleSectionConfig legacySection = new PuzzleSectionConfig
        {
            puzzleName = "LegacyLayout",
            enabled = true,
            gridSize = gridSize,
            cellSize = cellSize,
            gridOriginOffset = gridOriginOffset,
            floorPrefabs = floorPrefabs,
            floorPlacements = floorPlacements,
            doorPrefabs = doorPrefabs,
            doorPlacements = doorPlacements,
            buttonPrefabs = buttonPrefabs,
            buttonPlacements = buttonPlacements
        };

        BuildInspectorSection(
            legacySection,
            moduleIndex: 0,
            sectionLabel: "LegacyLayout",
            root: root,
            playerPosition: playerPosition,
            groundLayerIndex: groundLayerIndex,
            useLegacyFallbackFields: true);
    }

    private void BuildInspectorSection(
        PuzzleSectionConfig section,
        int moduleIndex,
        string sectionLabel,
        GameObject root,
        Vector3 playerPosition,
        int groundLayerIndex,
        bool useLegacyFallbackFields)
    {
        if (section == null || !section.enabled)
        {
            return;
        }

        if (section.gridSize.x <= 0 || section.gridSize.y <= 0)
        {
            Debug.LogError($"PuzzleLevelBuilder: {sectionLabel} grid size must be greater than zero in both dimensions.", this);
            return;
        }

        string safeSectionName = string.IsNullOrWhiteSpace(section.puzzleName) ? sectionLabel : section.puzzleName.Trim();
        float y = playerPosition.y - 0.5f;
        Vector3 origin = new Vector3(playerPosition.x + section.gridOriginOffset.x, y, playerPosition.z + section.gridOriginOffset.y);
        Dictionary<string, DoorController> doorsById = new Dictionary<string, DoorController>(StringComparer.OrdinalIgnoreCase);

        foreach (FloorPlacementEntry entry in section.floorPlacements)
        {
            GameObject prefab = ResolvePrefab(entry.overridePrefab, entry.prefabIndex, section.floorPrefabs, useLegacyFallbackFields ? floorPrefab : null);
            if (prefab == null)
            {
                Debug.LogWarning($"PuzzleLevelBuilder: Missing floor prefab for {safeSectionName} at grid {entry.gridPosition}.", this);
                continue;
            }

            GameObject floorObject = Instantiate(prefab, GridToWorld(entry.gridPosition, origin, section.cellSize), Quaternion.identity, root.transform);
            floorObject.name = $"Floor_{safeSectionName}_{entry.gridPosition.x}_{entry.gridPosition.y}";
            floorObject.transform.localScale = entry.localScale;
            SetLayerRecursively(floorObject, groundLayerIndex);
        }

        foreach (DoorPlacementEntry entry in section.doorPlacements)
        {
            GameObject prefab = ResolvePrefab(entry.overridePrefab, entry.prefabIndex, section.doorPrefabs, useLegacyFallbackFields ? doorPrefab : null);
            if (prefab == null)
            {
                Debug.LogWarning($"PuzzleLevelBuilder: Missing door prefab for {safeSectionName} at grid {entry.gridPosition}.", this);
                continue;
            }

            GameObject doorObject = Instantiate(prefab, GridToWorld(entry.gridPosition, origin, section.cellSize) + Vector3.up, FacingToRotation(entry.facing), root.transform);
            doorObject.name = string.IsNullOrWhiteSpace(entry.doorId)
                ? $"Door_{safeSectionName}_{entry.gridPosition.x}_{entry.gridPosition.y}"
                : $"{safeSectionName}_{entry.doorId}";
            SetLayerRecursively(doorObject, groundLayerIndex);

            DoorController controller = doorObject.GetComponent<DoorController>();
            if (controller == null)
            {
                controller = doorObject.AddComponent<DoorController>();
            }

            if (!string.IsNullOrWhiteSpace(entry.doorId))
            {
                string scopedDoorId = BuildScopedDoorId(moduleIndex, entry.doorId);
                doorsById[scopedDoorId] = controller;
            }

            if (entry.startsOpen)
            {
                StartCoroutine(OpenDoorAfterStart(controller));
            }

            if (entry.startsLocked)
            {
                Debug.Log($"PuzzleLevelBuilder: Door '{doorObject.name}' starts locked (metadata only).", this);
            }
        }

        foreach (ButtonPlacementEntry entry in section.buttonPlacements)
        {
            GameObject prefab = ResolvePrefab(entry.overridePrefab, entry.prefabIndex, section.buttonPrefabs, useLegacyFallbackFields ? buttonPrefab : null);
            if (prefab == null)
            {
                Debug.LogWarning($"PuzzleLevelBuilder: Missing button prefab for {safeSectionName} at grid {entry.gridPosition}.", this);
                continue;
            }

            GameObject buttonObject = Instantiate(prefab, GridToWorld(entry.gridPosition, origin, section.cellSize), Quaternion.identity, root.transform);
            buttonObject.name = $"Button_{safeSectionName}_{entry.gridPosition.x}_{entry.gridPosition.y}";
            SetLayerRecursively(buttonObject, groundLayerIndex);

            ButtonTrigger trigger = buttonObject.GetComponent<ButtonTrigger>();
            if (trigger == null)
            {
                trigger = buttonObject.AddComponent<ButtonTrigger>();
            }
            trigger.require2DMode = entry.require2DMode;

            if (entry.targetDoorIds != null && entry.targetDoorIds.Count > 0)
            {
                DoorController firstDoor = ResolveFirstLinkedDoor(entry.targetDoorIds, doorsById, moduleIndex);
                trigger.controlledDoor = firstDoor;
            }
        }
    }

    private IEnumerator OpenDoorAfterStart(DoorController controller)
    {
        yield return null;
        if (controller != null)
        {
            controller.Open();
        }
    }

    private void PreviewSection(PuzzleSectionConfig section, int moduleIndex, bool useLegacyNames)
    {
        if (section == null)
        {
            return;
        }

        string sectionName = useLegacyNames || string.IsNullOrWhiteSpace(section.puzzleName)
            ? $"Module_{moduleIndex}"
            : section.puzzleName.Trim();

        Debug.Log($"[{sectionName}] Enabled={section.enabled} Grid={section.gridSize} CellSize={section.cellSize} OriginOffset={section.gridOriginOffset}", this);

        for (int i = 0; i < section.floorPlacements.Count; i++)
        {
            FloorPlacementEntry entry = section.floorPlacements[i];
            string prefabName = entry.overridePrefab != null
                ? entry.overridePrefab.name
                : ResolvePrefabName(entry.prefabIndex, section.floorPrefabs, floorPrefab);
            Debug.Log($"[{sectionName}] Floor[{i}] Grid={entry.gridPosition} Prefab={prefabName} Scale={entry.localScale}", this);
        }

        for (int i = 0; i < section.doorPlacements.Count; i++)
        {
            DoorPlacementEntry entry = section.doorPlacements[i];
            string prefabName = entry.overridePrefab != null
                ? entry.overridePrefab.name
                : ResolvePrefabName(entry.prefabIndex, section.doorPrefabs, doorPrefab);
            Debug.Log($"[{sectionName}] Door[{i}] ID={entry.doorId} Grid={entry.gridPosition} Facing={entry.facing} Open={entry.startsOpen} Locked={entry.startsLocked} Prefab={prefabName}", this);
        }

        for (int i = 0; i < section.buttonPlacements.Count; i++)
        {
            ButtonPlacementEntry entry = section.buttonPlacements[i];
            string prefabName = entry.overridePrefab != null
                ? entry.overridePrefab.name
                : ResolvePrefabName(entry.prefabIndex, section.buttonPrefabs, buttonPrefab);
            string links = entry.targetDoorIds == null || entry.targetDoorIds.Count == 0 ? "<none>" : string.Join(", ", entry.targetDoorIds);
            Debug.Log($"[{sectionName}] Button[{i}] Grid={entry.gridPosition} Require2D={entry.require2DMode} Links={links} Prefab={prefabName}", this);
        }
    }

    private void PreviewLegacySingleLayout()
    {
        PuzzleSectionConfig legacySection = new PuzzleSectionConfig
        {
            puzzleName = "LegacyLayout",
            enabled = true,
            gridSize = gridSize,
            cellSize = cellSize,
            gridOriginOffset = gridOriginOffset,
            floorPrefabs = floorPrefabs,
            floorPlacements = floorPlacements,
            doorPrefabs = doorPrefabs,
            doorPlacements = doorPlacements,
            buttonPrefabs = buttonPrefabs,
            buttonPlacements = buttonPlacements
        };

        PreviewSection(legacySection, moduleIndex: 0, useLegacyNames: true);
    }

    private Vector3 GridToWorld(Vector2Int gridPosition, Vector3 origin, Vector2 sectionCellSize)
    {
        return new Vector3(
            origin.x + gridPosition.x * sectionCellSize.x,
            origin.y,
            origin.z + gridPosition.y * sectionCellSize.y);
    }

    private static Quaternion FacingToRotation(FacingDirection facing)
    {
        switch (facing)
        {
            case FacingDirection.East: return Quaternion.Euler(0f, 90f, 0f);
            case FacingDirection.South: return Quaternion.Euler(0f, 180f, 0f);
            case FacingDirection.West: return Quaternion.Euler(0f, 270f, 0f);
            default: return Quaternion.identity;
        }
    }

    private DoorController ResolveFirstLinkedDoor(List<string> targetDoorIds, Dictionary<string, DoorController> doorsById, int moduleIndex)
    {
        for (int i = 0; i < targetDoorIds.Count; i++)
        {
            string id = targetDoorIds[i];
            if (string.IsNullOrWhiteSpace(id)) continue;
            string scopedId = BuildScopedDoorId(moduleIndex, id);
            if (doorsById.TryGetValue(scopedId, out DoorController door))
            {
                return door;
            }
        }

        return null;
    }

    private static string BuildScopedDoorId(int moduleIndex, string rawDoorId)
    {
        return $"{moduleIndex}:{rawDoorId.Trim()}";
    }

    private GameObject ResolvePrefab(GameObject overridePrefab, int prefabIndex, List<GameObject> library, GameObject fallback)
    {
        if (overridePrefab != null) return overridePrefab;
        if (library != null && prefabIndex >= 0 && prefabIndex < library.Count && library[prefabIndex] != null)
        {
            return library[prefabIndex];
        }
        return fallback;
    }

    private string ResolvePrefabName(int prefabIndex, List<GameObject> library, GameObject fallback)
    {
        GameObject prefab = ResolvePrefab(null, prefabIndex, library, fallback);
        return prefab != null ? prefab.name : "<missing>";
    }

    private int ResolvePlayerGroundLayerIndex()
    {
        // PlayerController.groundLayer is a LayerMask (bitfield). Choose the first layer bit set.
        // If multiple layers are used, this picks one of them (enough for grounding).
        PlayerController pc = player != null ? player.GetComponentInParent<PlayerController>() : null;
        if (pc == null)
        {
            return 0; // Default layer fallback.
        }

        int mask = pc.groundLayer.value;
        for (int layer = 0; layer < 32; layer++)
        {
            if ((mask & (1 << layer)) != 0)
            {
                return layer;
            }
        }

        return 0;
    }

    private void SetLayerRecursively(GameObject obj, int layerIndex)
    {
        if (obj == null) return;
        obj.layer = layerIndex;
        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = layerIndex;
        }
    }

    private void SnapPlayerOntoFloor(GameObject floor, Transform playerTransform)
    {
        Collider playerCollider = playerTransform.GetComponentInChildren<Collider>();
        Collider floorCollider = floor.GetComponentInChildren<Collider>();

        if (playerCollider == null)
        {
            Debug.LogWarning("PuzzleLevelBuilder: Player has no Collider, cannot snap onto floor.", this);
            return;
        }

        if (floorCollider == null)
        {
            Debug.LogWarning("PuzzleLevelBuilder: Floor has no Collider, cannot snap onto floor.", this);
            return;
        }

        // Use collider bounds to align physics properly.
        float floorTopY = floorCollider.bounds.max.y + 0.01f;
        float playerBottomY = playerCollider.bounds.min.y;
        float delta = floorTopY - playerBottomY;

        playerTransform.position += Vector3.up * delta;

        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Avoid the first physics step pushing the player down again.
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
        }
    }

    private float GetTopY(GameObject obj)
    {
        Collider col = obj.GetComponentInChildren<Collider>();
        if (col == null) return obj.transform.position.y;
        return col.bounds.max.y;
    }

    private void AlignColliderBottomToY(GameObject obj, float targetTopY, float epsilon)
    {
        Collider col = obj.GetComponentInChildren<Collider>();
        if (col == null) return;

        float currentMinY = col.bounds.min.y;
        float desiredMinY = targetTopY + epsilon; // keep it just above the floor surface
        float delta = desiredMinY - currentMinY;

        obj.transform.position += Vector3.up * delta;
    }

    private void BuildNavMesh(GameObject rootRoot)
    {
        if (rootRoot == null) return;

        // Runtime navmesh build so enemies can work immediately without manual editor baking.
        // (In the editor you can still bake NavMeshSurface normally.)
        NavMeshSurface surface = rootRoot.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = rootRoot.AddComponent<NavMeshSurface>();
        }

        surface.BuildNavMesh();
    }

    private DoorController CreateDoor(Vector3 position, Transform parent, string name, float doorZScale, float floorTopY)
    {
        GameObject doorObject = Instantiate(doorPrefab, position, Quaternion.identity, parent);
        doorObject.name = name;
        // Stretch the door across the whole corridor so it can't be bypassed in Z.
        doorObject.transform.localScale = new Vector3(1.5f, 2f, doorZScale);

        // Prevent the door collider from starting intersecting the floor.
        AlignColliderBottomToY(doorObject, floorTopY, epsilon: 0.002f);

        DoorController controller = doorObject.GetComponent<DoorController>();
        if (controller == null)
        {
            controller = doorObject.AddComponent<DoorController>();
        }

        return controller;
    }

    private void CreateButton(Vector3 position, Transform parent, string name, DoorController targetDoor, float floorTopY)
    {
        GameObject buttonObject = Instantiate(buttonPrefab, position, Quaternion.identity, parent);
        buttonObject.name = name;
        buttonObject.transform.localScale = new Vector3(0.8f, 0.25f, 0.8f);

        // Put button sitting on floor.
        AlignColliderBottomToY(buttonObject, floorTopY, epsilon: 0.0015f);

        ButtonTrigger trigger = buttonObject.GetComponent<ButtonTrigger>();
        if (trigger == null)
        {
            trigger = buttonObject.AddComponent<ButtonTrigger>();
        }

        trigger.controlledDoor = targetDoor;
    }

    private ButtonTrigger CreateButtonWithTrigger(Vector3 position, Transform parent, string name, DoorController targetDoor, float floorTopY)
    {
        GameObject buttonObject = Instantiate(buttonPrefab, position, Quaternion.identity, parent);
        buttonObject.name = name;
        buttonObject.transform.localScale = new Vector3(0.8f, 0.25f, 0.8f);

        // Put button sitting on floor.
        AlignColliderBottomToY(buttonObject, floorTopY, epsilon: 0.0015f);

        ButtonTrigger trigger = buttonObject.GetComponent<ButtonTrigger>();
        if (trigger == null)
        {
            trigger = buttonObject.AddComponent<ButtonTrigger>();
        }

        trigger.controlledDoor = targetDoor;
        return trigger;
    }
}
