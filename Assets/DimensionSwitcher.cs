using UnityEngine;
using System.Collections;

public class DimensionSwitcher : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public PlayerController playerController;

    [Header("3D Camera Settings")]
    public Vector3 follow3DOffset = new Vector3(0f, 3f, -6f);
    public float followSpeed = 5f;

    [Header("2D Camera Settings")]
    public Vector3 cam2DOffset = new Vector3(0f, 5f, -15f);
    public float orthographicSize = 6f;

    [Header("Transition")]
    public float switchSpeed = 3f;

    private bool is2D = false;
    private bool isSwitching = false;
    private Transform player;

    void Start()
    {
        cam = Camera.main;

        if (playerController == null)
        {
            Debug.LogError("DimensionSwitcher: Player Controller is not assigned in the Inspector!");
            return;
        }

        player = playerController.transform;
        cam.transform.rotation = Quaternion.Euler(15f, 90f, 0f);
    }

    void LateUpdate()
    {
        if (player == null || isSwitching) return;

        if (!is2D)
        {
            Vector3 targetPos = new Vector3(
                player.position.x + follow3DOffset.z,
                player.position.y + follow3DOffset.y,
                player.position.z
            );
            cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, followSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 targetPos = new Vector3(
                player.position.x,
                player.position.y + cam2DOffset.y,
                player.position.z + cam2DOffset.z
            );
            cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, followSpeed * Time.deltaTime);
            cam.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isSwitching)
        {
            StartCoroutine(SwitchDimension());
        }
    }

    IEnumerator SwitchDimension()
    {
        isSwitching = true;
        is2D = !is2D;

        playerController.SetDimensionMode(is2D);

        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        float startFOV = cam.fieldOfView;

        Vector3 targetPos;
        Quaternion targetRot;

        if (is2D)
        {
            targetPos = new Vector3(
                player.position.x,
                player.position.y + cam2DOffset.y,
                player.position.z + cam2DOffset.z
            );
            targetRot = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            targetPos = new Vector3(
                player.position.x - follow3DOffset.z,
                player.position.y + follow3DOffset.y,
                player.position.z
            );
            targetRot = Quaternion.Euler(15f, 90f, 0f);
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * switchSpeed;

            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            cam.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);

            if (is2D)
            {
                cam.fieldOfView = Mathf.Lerp(startFOV, 1f, t);
                if (t >= 1f) cam.orthographic = true;
            }
            else
            {
                cam.orthographic = false;
                cam.fieldOfView = Mathf.Lerp(1f, 60f, t);
            }

            yield return null;
        }

        if (is2D)
        {
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }
        else
        {
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.transform.rotation = Quaternion.Euler(15f, 90f, 0f);
        }

        isSwitching = false;
    }
}