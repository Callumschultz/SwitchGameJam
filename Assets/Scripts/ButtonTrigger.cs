using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ButtonTrigger : MonoBehaviour
{
    [Header("2D Z Collapse (optional)")]
    // For puzzles where "Z collapses" in 2D mode visually AND physically.
    // When enabled, the button snaps its Z position to the player's Z whenever the player is in 2D mode.
    [SerializeField] public bool collapseToPlayerZIn2D = false;

    [Header("Linked Door")]
    public DoorController controlledDoor;

    [Header("Button Press Animation")]
    [SerializeField] private float pressDistance = 0.08f;
    [SerializeField] private float pressDuration = 0.15f;

    [Header("Mode Requirements")]
    [SerializeField] public bool require2DMode = false;

    private Vector3 initialLocalPosition;
    private bool triggered;
    private Coroutine pressRoutine;

    private Transform playerTransform;
    private PlayerController playerController;
    private float originalWorldZ;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
        initialLocalPosition = transform.localPosition;
        originalWorldZ = transform.position.z;
    }

    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        playerController = playerTransform != null ? playerTransform.GetComponentInParent<PlayerController>() : null;
    }

    private void Update()
    {
        if (!collapseToPlayerZIn2D) return;
        if (playerTransform == null || playerController == null) return;

        float desiredZ = playerController.Is2DMode ? playerTransform.position.z : originalWorldZ;
        Vector3 pos = transform.position;
        if (!Mathf.Approximately(pos.z, desiredZ))
        {
            pos.z = desiredZ;
            transform.position = pos;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        AudioManager.Instance.PlayButtonPress();
        
        if (triggered || !other.CompareTag("Player"))
        {
            return;
        }

        if (require2DMode)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                player = other.GetComponent<PlayerController>();
            }
            if (player == null || !player.Is2DMode)
            {
                return; // Only solvable in 2D mode.
            }
        }

        triggered = true;

        if (controlledDoor != null)
        {
            controlledDoor.OnButtonPressed(this);
        }
        else
        {
            Debug.LogWarning($"{name} has no linked door assigned.", this);
        }

        pressRoutine = StartCoroutine(PressAnimationRoutine());
    }

    private IEnumerator PressAnimationRoutine()
    {
        Vector3 pressedPosition = initialLocalPosition + Vector3.down * pressDistance;
        float elapsed = 0f;

        while (elapsed < pressDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pressDuration);
            transform.localPosition = Vector3.Lerp(initialLocalPosition, pressedPosition, t);
            yield return null;
        }

        transform.localPosition = pressedPosition;
    }

    public void ResetTrigger()
    {
        triggered = false;

        if (pressRoutine != null)
        {
            StopCoroutine(pressRoutine);
            pressRoutine = null;
        }

        transform.localPosition = initialLocalPosition;
    }
}
