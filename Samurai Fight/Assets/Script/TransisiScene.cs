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
    private float lastVolume = 0.5f; // MENYIMPAN RIWAYAT VOLUME SEBELUM MUTE

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
                // Volume dimulai dari 0 untuk efek fade-in nanti
                bgmSource.volume = 0f; 
            }

            StartCoroutine(FadeIn());
        }
    }

    // --- LOGIC SINKRONISASI VOLUME BARU ---

    public void ToggleMute()
    {
        if (bgmSource != null)
        {
            bgmSource.mute = !bgmSource.mute;

            if (bgmSource.mute)
            {
                // KETIKA MUTE: Simpan volume terakhir, lalu set volume ke 0
                if (masterVolume > 0) lastVolume = masterVolume;
                SetMasterVolume(0); 
            }
            else
            {
                // KETIKA UNMUTE: Kembalikan volume ke posisi terakhir
                // Jika lastVolume error (0), kembalikan ke default 0.5
                float targetVolume = (lastVolume > 0) ? lastVolume : 0.5f;
                SetMasterVolume(targetVolume);
            }
        }
    }

    public bool IsMuted()
    {
        if (bgmSource != null)
        {
            // Dianggap mute jika centang Mute aktif ATAU volume 0
            return bgmSource.mute || masterVolume <= 0;
        }
        return false;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;

        if (bgmSource != null)
        {
            bgmSource.volume = masterVolume;
            
            // Logic Otomatis: Jika Slider di 0, otomatis Mute. Jika > 0, Unmute.
            if (masterVolume <= 0)
            {
                bgmSource.mute = true;
            }
            else
            {
                bgmSource.mute = false;
                // Update memori lastVolume hanya ketika volume sedang aktif (>0)
                lastVolume = masterVolume; 
            }
        }
    }

    public float GetMasterVolume()
    {
        return masterVolume;
    }

    // --- BATAS LOGIC BARU ---

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

            // Fade out audio berdasarkan volume saat ini
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
                // Fade in audio menuju masterVolume yang diset user
                bgmSource.volume = Mathf.Lerp(0f, masterVolume, progress);
            }

            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasObject.SetActive(false);
        
        // Pastikan volume akhir sesuai settingan user
        if (bgmSource.clip != null) bgmSource.volume = masterVolume;
    }

    public void MuteBGMForCutscene(float duration = 0.5f)
    {
        StartCoroutine(FadeBGMVolume(0f, duration));
    }

    public void ResumeBGMAfterCutscene(float duration = 0.5f)
    {
        StopAllCoroutines();
        // Kembalikan ke masterVolume, bukan 1f (agar sesuai slider)
        StartCoroutine(FadeBGMVolume(masterVolume, duration));
    }

    private IEnumerator FadeBGMVolume(float targetVolume, float duration)
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