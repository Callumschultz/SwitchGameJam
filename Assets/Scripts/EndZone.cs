using UnityEngine;

public class EndZoneTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            LevelManager.Instance.CompleteLevel();
        }
    }
}