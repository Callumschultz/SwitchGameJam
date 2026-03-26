using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public enum DoorMode
    {
        SingleButton,
        TimedTwoSwitches
    }

    [Header("Mode")]
    [SerializeField] private DoorMode doorMode = DoorMode.SingleButton;

    [Header("Timed Two Switches")]
    [SerializeField] private float twoSwitchWindowSeconds = 0.45f;
    [SerializeField] private float countdownTextHeight = 2.3f;
    [SerializeField] private int requiredButtonCount = 2;

    [Header("Open Motion")]
    // Default chosen for the provided door/floor prefabs.
    // If your door starts intersecting the floor too much or ends too low, tweak this in the Inspector.
    [SerializeField] private float moveDownDistance = 1.2f;
    [SerializeField] private float moveDuration = 0.6f;

    private Vector3 closedPosition;
    private bool isOpening;
    private bool isClosing;

    public bool IsOpen { get; private set; }

    private readonly HashSet<ButtonTrigger> pressedButtons = new HashSet<ButtonTrigger>();
    private Coroutine sequenceCoroutine;
    private Coroutine openCoroutine;
    private Coroutine closeCoroutine;

    private TextMesh countdownText;
    private float firstPressTime = -1f;

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

    public void SetMode(DoorMode mode)
    {
        doorMode = mode;
    }

    public void ConfigureTimedTwoSwitches(float windowSeconds, int requiredButtons = 2)
    {
        twoSwitchWindowSeconds = Mathf.Max(0.05f, windowSeconds);
        requiredButtonCount = Mathf.Max(2, requiredButtons);
    }

    public void OnButtonPressed(ButtonTrigger button)
    {
        if (button == null) return;

        switch (doorMode)
        {
            case DoorMode.SingleButton:
                Open();
                return;

            case DoorMode.TimedTwoSwitches:
                HandleTimedTwoSwitchPress(button);
                return;
        }
    }

    public void Open()
    {
        if (IsOpen || isOpening)
        {
            return;
        }

        isClosing = false;

        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        HideCountdown();
        pressedButtons.Clear();
        firstPressTime = -1f;

        openCoroutine = StartCoroutine(OpenRoutine());
    }

    public void Close()
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;

        // If we're in the middle of opening, cancel that motion.
        if (openCoroutine != null)
        {
            StopCoroutine(openCoroutine);
            openCoroutine = null;
        }
        isOpening = false;

        HideCountdown();
        pressedButtons.Clear();
        firstPressTime = -1f;
        closeCoroutine = StartCoroutine(CloseRoutine());
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
        openCoroutine = null;
    }

    private IEnumerator CloseRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = closedPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float easedT = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPos, targetPos, easedT);
            yield return null;
        }

        transform.position = targetPos;
        IsOpen = false;
        isOpening = false;
        isClosing = false;
        closeCoroutine = null;
    }

    private void HandleTimedTwoSwitchPress(ButtonTrigger button)
    {
        // If the door is already open and there's no active timed sequence, ignore.
        if (IsOpen && sequenceCoroutine == null) return;

        // Only count each button once per attempt.
        if (pressedButtons.Contains(button)) return;
        pressedButtons.Add(button);

        // Start the countdown on the first press.
        if (sequenceCoroutine == null)
        {
            firstPressTime = Time.time;
            sequenceCoroutine = StartCoroutine(TimedTwoSwitchRoutine());
        }

        // If both switches are pressed within the window, open immediately.
        if (pressedButtons.Count >= requiredButtonCount)
        {
            float elapsed = firstPressTime >= 0f ? (Time.time - firstPressTime) : Mathf.Infinity;
            if (elapsed > twoSwitchWindowSeconds)
            {
                return; // Too late; timer should close/reset the door shortly.
            }

            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
                sequenceCoroutine = null;
            }

            HideCountdown();
            Open();
        }
    }

    private IEnumerator TimedTwoSwitchRoutine()
    {
        ShowCountdown();
        float startTime = firstPressTime >= 0f ? firstPressTime : Time.time;

        while (Time.time - startTime < twoSwitchWindowSeconds)
        {
            float remaining = twoSwitchWindowSeconds - (Time.time - startTime);
            SetCountdown(remaining);
            yield return null;
        }

        // Time ran out: close and reset buttons so the player can retry.
        HideCountdown();
        foreach (ButtonTrigger btn in pressedButtons)
        {
            if (btn != null)
            {
                btn.ResetTrigger();
            }
        }

        pressedButtons.Clear();
        Close();
        sequenceCoroutine = null;
        firstPressTime = -1f;
    }

    private void BeginOpenForTimedSequence()
    {
        if (IsOpen || isOpening) return;
        isClosing = false;
        openCoroutine = StartCoroutine(OpenRoutine());
    }

    private void EnsureCountdownText()
    {
        if (countdownText != null) return;

        GameObject go = new GameObject("CountdownTimerText");
        go.transform.SetParent(transform, worldPositionStays: false);
        go.transform.localPosition = new Vector3(0f, countdownTextHeight, 0f);

        countdownText = go.AddComponent<TextMesh>();
        countdownText.alignment = TextAlignment.Center;
        countdownText.anchor = TextAnchor.MiddleCenter;
        countdownText.characterSize = 0.2f;
        countdownText.fontSize = 72;

        // Unity 6 built-in runtime font name differs from older projects.
        // If font loading fails for any reason, we skip setting it so timer logic continues.
        try
        {
            countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            // Leave font as null to avoid crashing the coroutine.
        }
        countdownText.color = Color.white;

        countdownText.text = "";
        countdownText.gameObject.SetActive(false);
    }

    private void ShowCountdown()
    {
        EnsureCountdownText();
        countdownText.gameObject.SetActive(true);
    }

    private void HideCountdown()
    {
        if (countdownText == null) return;
        countdownText.gameObject.SetActive(false);
    }

    private void SetCountdown(float secondsRemaining)
    {
        EnsureCountdownText();
        countdownText.text = Mathf.Max(0f, secondsRemaining).ToString("0.0");
    }
}
