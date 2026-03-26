using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ButtonTrigger : MonoBehaviour
{
    [Header("Linked Door")]
    public DoorController controlledDoor;

    [Header("Button Press Animation")]
    [SerializeField] private float pressDistance = 0.08f;
    [SerializeField] private float pressDuration = 0.15f;

    [Header("Mode Requirements")]
    [SerializeField] public bool require2DMode = false;

    private Vector3 initialLocalPosition;
    private bool triggered;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
        initialLocalPosition = transform.localPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
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
            controlledDoor.Open();
        }
        else
        {
            Debug.LogWarning($"{name} has no linked door assigned.", this);
        }

        StartCoroutine(PressAnimationRoutine());
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
}
