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
    
    // Logic Volume
    private float masterVolume = 0.5f; 
    private float lastVolume = 0.5f; 

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
            PlayMusicForCurrentScene();
            
            StartCoroutine(AnimateFade(1f, 0f, 0f, masterVolume, true));
        }
    }

    private void PlayMusicForCurrentScene()
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
    }

    public void ToggleMute()
    {
        if (bgmSource == null) return;

        bgmSource.mute = !bgmSource.mute;

        if (bgmSource.mute)
        {
            if (masterVolume > 0) lastVolume = masterVolume;
            SetMasterVolume(0); 
        }
        else
        {
            float targetVolume = (lastVolume > 0) ? lastVolume : 0.5f;
            SetMasterVolume(targetVolume);
        }
    }

    public bool IsMuted()
    {
        return (bgmSource != null && bgmSource.mute) || masterVolume <= 0;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;

        if (bgmSource != null)
        {
            bgmSource.volume = masterVolume;
            
            if (masterVolume <= 0)
            {
                bgmSource.mute = true;
            }
            else
            {
                bgmSource.mute = false;
                lastVolume = masterVolume; 
            }
        }
    }

    public float GetMasterVolume() => masterVolume;

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
        float currentVol = bgmSource.volume;

        yield return StartCoroutine(AnimateFade(0f, 1f, currentVol, 0f, false));

        SceneManager.LoadScene(targetSceneName);

        if (newMusic != null)
        {
            if (changeSong || bgmSource.clip == null || !bgmSource.isPlaying)
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

        yield return StartCoroutine(AnimateFade(1f, 0f, 0f, masterVolume, true));
        
        isTransisiActive = false;
    }

    private IEnumerator AnimateFade(float startAlpha, float endAlpha, float startVol, float endVol, bool deactivateCanvasOnEnd)
    {
        if (fadeCanvasObject != null) fadeCanvasObject.SetActive(true);
        
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeDuration);

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            }

            if (bgmSource != null)
            {
                bgmSource.volume = Mathf.Lerp(startVol, endVol, progress);
            }

            yield return null;
        }

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = endAlpha;
        if (bgmSource != null) bgmSource.volume = endVol;

        if (deactivateCanvasOnEnd && fadeCanvasObject != null) 
        {
            fadeCanvasObject.SetActive(false);
        }
    }

    private AudioClip FindMusic(string sceneName)
    {
        foreach (var item in musicList)
        {
            if (item.sceneName == sceneName) return item.bgmMusic;
        }
        return null; 
    }

    public void MuteBGMForCutscene(float duration = 0.5f)
    {
        StartCoroutine(FadeBGMOnly(0f, duration));
    }

    public void ResumeBGMAfterCutscene(float duration = 0.5f)
    {
        StopAllCoroutines();
        if (!isTransisiActive) 
        {
            StartCoroutine(FadeBGMOnly(masterVolume, duration));
        }
    }

    private IEnumerator FadeBGMOnly(float targetVolume, float duration)
    {
        if (bgmSource == null) yield break;

        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
    }
}