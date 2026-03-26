using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Open Motion")]
    // Default chosen for the provided door/floor prefabs.
    // If your door starts intersecting the floor too much or ends too low, tweak this in the Inspector.
    [SerializeField] private float moveDownDistance = 1.2f;
    [SerializeField] private float moveDuration = 0.6f;

    private Vector3 closedPosition;
    private bool isOpening;

    public bool IsOpen { get; private set; }

    private void Start()
    {
        closedPosition = transform.position;

        // Some projects/prefabs may accidentally include a Rigidbody on doors.
        // Make the door deterministic by ensuring it doesn't fall under gravity.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void Open()
    {
        if (IsOpen || isOpening)
        {
            return;
        }

        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        isOpening = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = closedPosition + Vector3.down * moveDownDistance;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            // Smoothstep gives a simple ease-in/ease-out door motion.
            float easedT = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPos, targetPos, easedT);
            yield return null;
        }

        transform.position = targetPos;
        IsOpen = true;
        isOpening = false;
    }
}
