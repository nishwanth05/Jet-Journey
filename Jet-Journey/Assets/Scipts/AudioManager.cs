using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source")]
    public AudioSource musicSource;

    [Header("Music Clips")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip gameEndMusic;

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f;
    public float targetVolume = 0.6f;

    string currentTrackName = "";
    Coroutine fadeRoutine;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        //FadeToMusic(mainMenuMusic);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Menu"))
        {
            FadeToMusic(mainMenuMusic);
        }
        else if (scene.name.Contains("Game") || scene.name.Contains("Level"))
        {
            FadeToMusic(gameplayMusic);
        }
        else if (scene.name.Contains("End") || scene.name.Contains("Result"))
        {
            FadeToMusic(gameEndMusic);
        }
    }

    public void FadeToMusic(AudioClip newClip)
    {
        if (newClip == null) return;
        if (currentTrackName == newClip.name) return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeMusicRoutine(newClip));
    }

    IEnumerator FadeMusicRoutine(AudioClip newClip)
    {
        // Fade OUT
        float startVolume = musicSource.volume;

        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        musicSource.Stop();

        // Switch clip
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();
        currentTrackName = newClip.name;

        // Fade IN
        musicSource.volume = 0f;

        while (musicSource.volume < targetVolume)
        {
            musicSource.volume += targetVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void StopMusic()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        musicSource.Stop();
        currentTrackName = "";
    }
}