using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sound Effects")]
    public AudioClip dimensionSwitch;
    public AudioClip doorOpen;
    public AudioClip buttonPress;

    [Header("Background Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.4f;

    private AudioSource source;
    private AudioSource musicSource;

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

        source = GetComponent<AudioSource>();

        // Second Audio Source specifically for music
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;
    }

    void Start()
    {
        if (backgroundMusic != null)
            musicSource.Play();
    }

    public void PlayDimensionSwitch()
    {
        source.PlayOneShot(dimensionSwitch);
    }

    public void PlayDoorOpen()
    {
        source.PlayOneShot(doorOpen);
    }

    public void PlayButtonPress()
    {
        source.PlayOneShot(buttonPress);
    }

    public void StopMusic() { musicSource.Stop(); }
    public void SetMusicVolume(float volume) { musicSource.volume = volume; }
}