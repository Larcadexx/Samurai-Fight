using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransisiScene : MonoBehaviour
{
    public static TransisiScene Instance;

    [Header("Komponen UI")]
    public CanvasGroup fadeCanvasGroup;
    public GameObject fadeCanvasObject;

    [Header("Komponen Audio")]
    public AudioSource bgmSource;
    
    private float masterVolume = 0.5f; 

    [System.Serializable]
    public struct PlaylistScene
    {
        public string namaScene;
        public AudioClip musikBGM;
    }

    [Header("Daftar Lagu")]
    public List<PlaylistScene> daftarMusik; 

    [Header("Pengaturan Waktu")]
    public float durasiFade = 1.0f;
    public float durasiTunggu = 1.0f;

    private bool sedangTransisi = false;

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
            AudioClip musicAwal = CariMusik(currentScene);

            if (musicAwal != null)
            {
                bgmSource.clip = musicAwal;
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
        if (bgmSource != null && !sedangTransisi)
        {
            bgmSource.volume = masterVolume;
        }
    }

    public float GetMasterVolume()
    {
        return masterVolume;
    }

    public void PindahKeScene(string namaScene)
    {
        if (!sedangTransisi)
        {
            StartCoroutine(ProsesTransisi(namaScene));
        }
    }

    private IEnumerator ProsesTransisi(string namaSceneTujuan)
    {
        sedangTransisi = true;
        Time.timeScale = 1f;  

        AudioClip musikBaru = CariMusik(namaSceneTujuan);
        bool gantiLagu = (musikBaru != bgmSource.clip);

        yield return StartCoroutine(FadeOut(true)); 

        SceneManager.LoadScene(namaSceneTujuan);

        if (musikBaru != null)
        {
            bgmSource.clip = musikBaru;
            bgmSource.Stop(); 
            bgmSource.time = 0f; 
            bgmSource.Play();
        }
        else
        {
            bgmSource.Stop();
        }

        yield return new WaitForSeconds(durasiTunggu);

        yield return StartCoroutine(FadeIn());
        
        sedangTransisi = false;
    }

    private AudioClip CariMusik(string namaScene)
    {
        foreach (var item in daftarMusik)
        {
            if (item.namaScene == namaScene)
            {
                return item.musikBGM;
            }
        }
        return null; 
    }

    private IEnumerator FadeOut(bool matikanAudio)
    {
        fadeCanvasObject.SetActive(true);
        float timer = 0f;
        float startVolume = bgmSource.volume;

        while (timer < durasiFade)
        {
            timer += Time.deltaTime;
            float progress = timer / durasiFade;

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

        while (timer < durasiFade)
        {
            timer += Time.deltaTime;
            float progress = timer / durasiFade; 

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