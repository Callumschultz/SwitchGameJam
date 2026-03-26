using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class PuzzleLevelBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private GameObject buttonPrefab;

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Build Settings")]
    [SerializeField] private bool rebuildOnPlay = true;

    [Header("Enemies (runtime spawn)")]
    [SerializeField] private bool spawnEnemies = true;
    [SerializeField] private int enemyCount = 3;
    [SerializeField] private float enemySpawnYOffset = 1f;
    [SerializeField] private float enemyPatrolRadius = 5f;
    [SerializeField] private float enemyDetectionRange = 8f;

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
        if (floorPrefab == null || doorPrefab == null || buttonPrefab == null)
        {
            Debug.LogError("PuzzleLevelBuilder is missing one or more prefab references.", this);
            return;
        }

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

        float y = player.position.y - 0.5f;
        float baseX = player.position.x;
        float baseZ = player.position.z;

        // Layout is built relative to the player so the puzzles are always near where you start.
        float startX = baseX - 2f;
        float mainFloorLengthX = 40f;
        float corridorWidthZ = 8f;
        float mainFloorCenterX = startX + mainFloorLengthX * 0.5f;
        // Slightly wider than the floor so the player can't "sneak around" the wall in Z.
        float doorZScale = corridorWidthZ + 0.25f;

        // Main floor strip.
        GameObject floor = Instantiate(floorPrefab, new Vector3(mainFloorCenterX, y, baseZ), Quaternion.identity, root.transform);
        floor.name = "PuzzleFloor";
        floor.transform.localScale = new Vector3(mainFloorLengthX, 0.1f, corridorWidthZ);
        SetLayerRecursively(floor, groundLayerIndex);

#if UNITY_EDITOR
        Debug.Log($"PuzzleLevelBuilder: resolved ground layer index = {groundLayerIndex}. Spawned PuzzleFloor layer = {floor.layer}.", this);
#endif

        // Snap the player onto the generated floor so enabling gravity doesn't make them fall through.
        SnapPlayerOntoFloor(floor, player);

        float floorTopY = GetTopY(floor);

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

        // End zone marker near level end.
        GameObject endZone = Instantiate(floorPrefab, new Vector3(startX + 37f, y + 0.03f, baseZ), Quaternion.identity, root.transform);
        endZone.name = "EndZone";
        endZone.transform.localScale = new Vector3(2f, 0.05f, 2f);
        SetLayerRecursively(endZone, groundLayerIndex);

        BuildNavMesh(root);

        if (spawnEnemies)
        {
            SpawnEnemies(enemyCount, startX, baseZ, corridorWidthZ, floorTopY, enemySpawnYOffset, root.transform);
        }
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

    private void SpawnEnemies(int count, float startX, float baseZ, float corridorWidthZ, float floorTopY, float yOffset, Transform parent)
    {
        if (count <= 0) return;

        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");

        // A simple set of spawn points that keeps them on the main floor region.
        float[] xOffsets = new float[] { 2f, 8f, 14f };
        float[] zOffsets = new float[] { 2.5f, -2.5f, 1.2f };

        for (int i = 0; i < count; i++)
        {
            float x = startX + xOffsets[Mathf.Min(i, xOffsets.Length - 1)];
            float z = baseZ + zOffsets[Mathf.Min(i, zOffsets.Length - 1)];

            Vector3 pos = new Vector3(x, floorTopY + yOffset, z);

            // Use a built-in capsule primitive so the enemy has a visible MeshRenderer.
            // (Previously we only added a CapsuleCollider, which results in an invisible enemy.)
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = $"Enemy_{i + 1}";
            enemy.transform.position = pos;
            enemy.transform.SetParent(parent, true);

            if (enemyLayerIndex >= 0)
            {
                enemy.layer = enemyLayerIndex;
            }

            CapsuleCollider capsule = enemy.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = enemy.AddComponent<CapsuleCollider>();
            }
            capsule.radius = 0.3f;
            capsule.height = 1.8f;
            capsule.center = Vector3.up * (capsule.height * 0.5f);

            // Make it a small grey capsule (visible in both Game view and builds).
            Renderer renderer = enemy.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = Color.gray;
            }

            // Keep the enemy stable with rigidbody but prevent rotation flicker.
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = enemy.AddComponent<Rigidbody>();
            }
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = 3.2f;

            EnemyAI ai = enemy.AddComponent<EnemyAI>();

            // Configure AI values from the builder so you can tweak them in the inspector.
            ai.patrolRadius = enemyPatrolRadius;
            ai.detectionRange = enemyDetectionRange;

            // Ensure the collider sits on the floor (avoids tiny penetrations that can mess up patrol grounding).
            AlignColliderBottomToY(enemy, floorTopY, epsilon: 0.005f);

            // Face the player initially to make detection easier.
            if (player != null)
            {
                Vector3 toPlayer = player.position - enemy.transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.0001f)
                {
                    enemy.transform.forward = toPlayer.normalized;
                }
            }
        }
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
