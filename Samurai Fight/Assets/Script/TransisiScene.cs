using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class TransisiScene : MonoBehaviour
{
    public static TransisiScene Instance;

    [Header("UI Components")]
    public CanvasGroup fadeCanvasGroup;
    public GameObject fadeCanvasObject;

    [Header("Audio Components")]
    public AudioSource bgmSource;
    
    private float masterVolume = 0.5f; 

    [System.Serializable]
    public struct ScenePlaylist
    {
        public string sceneName;
        public AudioClip bgmMusic;
    }

    [Header("Music List")]
    public List<ScenePlaylist> musicList; 

    [Header("Time Settings")]
    public float fadeDuration = 1.0f;
    public float waitDuration = 1.0f;

    private bool isTransisiActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (bgmSource == null) 
                bgmSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = masterVolume;
        }

        if (Instance == this)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            AudioClip initialMusic = FindMusic(currentScene);

            if (initialMusic != null)
            {
                bgmSource.clip = initialMusic;
                bgmSource.loop = true;
                bgmSource.Play();
                bgmSource.volume = 0f; 
            }

            StartCoroutine(FadeIn());
        }
    }

  
    public bool ToggleMute()
    {
        if (bgmSource != null)
        {
            bgmSource.mute = !bgmSource.mute;
            return bgmSource.mute;
        }
        return false;
    }

    public bool IsMuted()
    {
        if (bgmSource != null)
        {
            return bgmSource.mute;
        }
        return false;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        if (bgmSource != null && !isTransisiActive)
        {
            bgmSource.volume = masterVolume;
        }
    }

    public float GetMasterVolume()
    {
        return masterVolume;
    }

    public void LoadSceneTransisi(string targetSceneName)
    {
        if (!isTransisiActive)
        {
            StartCoroutine(ProcessTransisi(targetSceneName));
        }
    }

    private IEnumerator ProcessTransisi(string targetSceneName)
    {
        isTransisiActive = true;
        Time.timeScale = 1f;  

        AudioClip newMusic = FindMusic(targetSceneName);
        
        bool changeSong = (newMusic != bgmSource.clip);

        yield return StartCoroutine(FadeOut(true)); 

        SceneManager.LoadScene(targetSceneName);

        if (newMusic != null)
        {
            if (changeSong || bgmSource.clip == null)
            {
                bgmSource.clip = newMusic;
                bgmSource.Stop(); 
                bgmSource.time = 0f; 
                bgmSource.Play();
            }
        }
        else
        {
            bgmSource.Stop();
        }

        yield return new WaitForSeconds(waitDuration);

        yield return StartCoroutine(FadeIn());
        
        isTransisiActive = false;
    }

    private AudioClip FindMusic(string sceneName)
    {
        foreach (var item in musicList)
        {
            if (item.sceneName == sceneName)
            {
                return item.bgmMusic;
            }
        }
        return null; 
    }

    private IEnumerator FadeOut(bool muteAudio)
    {
        fadeCanvasObject.SetActive(true);
        float timer = 0f;
        float startVolume = bgmSource.volume;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

            fadeCanvasGroup.alpha = Mathf.Clamp01(progress);

            bgmSource.volume = Mathf.Lerp(startVolume, 0f, progress);

            yield return null;
        }
        
        fadeCanvasGroup.alpha = 1f;
        bgmSource.volume = 0f; 
    }

    private IEnumerator FadeIn()
    {
        float timer = 0f;
        
        bgmSource.volume = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration; 

            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - progress);

            if (bgmSource.clip != null && bgmSource.isPlaying)
            {
                bgmSource.volume = Mathf.Lerp(0f, masterVolume, progress);
            }

            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasObject.SetActive(false);
        
        if (bgmSource.clip != null) bgmSource.volume = masterVolume;
    }
}