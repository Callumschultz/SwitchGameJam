using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Settings")]
    [Tooltip("How far ahead on the X axis each new level spawns.")]
    public float levelSpacingX = 120f;

    [Header("UI")]
    public GameObject gameOverScreen;
    public GameObject levelCompleteScreen;

    private int currentLevel = 0;
    private const int TotalLevels = 4;
    private PuzzleLevelBuilder levelBuilder;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        levelBuilder = FindFirstObjectByType<PuzzleLevelBuilder>();
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (levelCompleteScreen != null) levelCompleteScreen.SetActive(false);
    }

    public void CompleteLevel()
    {
        currentLevel++;

        if (currentLevel >= TotalLevels)
        {
            EndGame();
            return;
        }

        StartCoroutine(LoadNextLevel());
    }

    System.Collections.IEnumerator LoadNextLevel()
    {
        // Brief pause so player knows something happened
        if (levelCompleteScreen != null)
        {
            levelCompleteScreen.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            levelCompleteScreen.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Move player forward to the new level position
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            Rigidbody rb = playerObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }

            playerObject.transform.position = new Vector3(
                playerObject.transform.position.x + levelSpacingX,
                playerObject.transform.position.y,
                0f // reset Z so player is centered on next level
            );
        }

        // Rebuild the level at the new position with a new puzzle order
        if (levelBuilder != null)
        {
            levelBuilder.SetLevelIndex(currentLevel);
            levelBuilder.RebuildLevel();
        }
    }

    void EndGame()
    {
        Debug.Log("Game Complete!");

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }

        // Freeze the player
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            Rigidbody rb = playerObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }

    public int GetCurrentLevel() => currentLevel;
}