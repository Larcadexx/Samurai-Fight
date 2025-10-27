using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    private static MusicManager Instance;
    private AudioSource audioSource;

    public AudioClip backgroundMusic;
    [SerializeField] private Slider musicSlider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.volume = 1f; // Set default volume to max
            // DontDestroyOnLoad(gameObject); // Uncomment jika ingin persist antar scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("MusicManager Start() Called");

        if (backgroundMusic != null)
        {
            Debug.Log("Playing background music: " + backgroundMusic.name);
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        if (musicSlider != null)
        {
            // Dengarkan perubahan slider
            musicSlider.onValueChanged.AddListener(delegate { SetVolume(musicSlider.value); });

            // Jika slider belum diatur di Inspector, kita set dari volume saat ini
            if (musicSlider.value == 0)
            {
                musicSlider.value = audioSource.volume;
            }
            else
            {
                // Jika slider sudah diset, kita pakai nilainya
                SetVolume(musicSlider.value);
            }
        }
    }

    public static void SetVolume(float volume)
    {
        if (Instance != null && Instance.audioSource != null)
        {
            Instance.audioSource.volume = volume;
        }
    }

    public void PlayBackgroundMusic(bool resetSong, AudioClip audioClip = null)
    {
        if (audioClip != null)
        {
            audioSource.clip = audioClip;
        }

        if (audioSource.clip != null)
        {
            if (resetSong)
            {
                audioSource.Stop();
            }

            audioSource.Play();
        }
    }

    public void PauseBackgroundMusic()
    {
        audioSource.Pause();
    }
}