using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DimensionSwitchEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    public Image flashImage;
    public float flashDuration = 0.15f;
    public Color flashColor = Color.white;

    [Header("Chromatic Aberration")]
    public float aberrationStrength = 0.05f;
    public float aberrationDuration = 0.4f;

    private Material screenMat;
    private bool isEffectPlaying = false;

    void Start()
    {
        // Make sure flash starts invisible
        if (flashImage != null)
        {
            Color c = flashColor;
            c.a = 0f;
            flashImage.color = c;
        }
    }

    public void PlaySwitchEffect()
    {
        if (!isEffectPlaying)
        {
            StartCoroutine(FlashEffect());
            StartCoroutine(ShakeEffect());
        }
    }

    IEnumerator FlashEffect()
    {
        isEffectPlaying = true;

        // Fade in
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / flashDuration;
            Color c = flashColor;
            c.a = Mathf.Lerp(0f, 0.6f, t);
            flashImage.color = c;
            yield return null;
        }

        // Fade out
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / (flashDuration * 2f);
            Color c = flashColor;
            c.a = Mathf.Lerp(0.6f, 0f, t);
            flashImage.color = c;
            yield return null;
        }

        // Make sure it ends fully transparent
        Color final = flashColor;
        final.a = 0f;
        flashImage.color = final;

        isEffectPlaying = false;
    }

    IEnumerator ShakeEffect()
    {
        // Quick camera shake on switch
        Camera cam = Camera.main;
        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < aberrationDuration)
        {
            float strength = Mathf.Lerp(aberrationStrength, 0f, elapsed / aberrationDuration);
            cam.transform.localPosition = originalPos + Random.insideUnitSphere * strength;
            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.transform.localPosition = originalPos;
    }
}