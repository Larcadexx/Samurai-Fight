using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    [Header("Tombol Utama")]
    public GameObject playButton;
    public GameObject gameRulesButton;
    public GameObject creditsButton;
    public GameObject closeAppButton;
    public GameObject volumeButton;

    [Header("Panel")]
    public GameObject creditPanel;
    public GameObject gameRulesPanel;
    public GameObject volumePanel;

    [Header("UI Slider")]
    public Slider volumeSlider; 

    [Header("Game Rule System")]
    public Image gameRulesImage;       
    public List<Sprite> gameRulesSprites;
    public GameObject prevRuleButton; 
    public GameObject nextRuleButton;

    [Header("Credits System")]
    public Image creditImage;             // Komponen Image di dalam creditPanel
    public List<Sprite> creditSprites;    // Daftar gambar untuk credit
    public GameObject prevCreditButton;   // Tombol Back untuk credit
    public GameObject nextCreditButton;   // Tombol Next untuk credit

    private int currentPageIndex = 0;
    private int currentCreditPageIndex = 0; // Index halaman credit
    private bool isVolumePanelVisible = false;

    void Start()
    {
        playButton.SetActive(true);
        creditsButton.SetActive(true);
        gameRulesButton.SetActive(true);
        closeAppButton.SetActive(true);
        volumeButton.SetActive(true);
        
        if (creditPanel != null) creditPanel.SetActive(false);
        if (gameRulesPanel != null) gameRulesPanel.SetActive(false);
        if (volumePanel != null) volumePanel.SetActive(false);

        if (volumeSlider != null)
        {
            if (TransisiScene.Instance != null)
            {
                volumeSlider.value = TransisiScene.Instance.GetMasterVolume();
            }

            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    public void OnVolumeChanged(float value)
    {
        if (TransisiScene.Instance != null)
        {
            TransisiScene.Instance.SetMasterVolume(value);
        }
    }

    public void PlayGame()
    {
        if (TransisiScene.Instance != null)
        {
            TransisiScene.Instance.LoadSceneTransisi("Gameplay");
        }
        else
        {
            SceneManager.LoadScene("Gameplay");
        }
    }

    // --- LOGIKA CREDITS (BARU) ---
    public void ShowCredits()
    {
        ToggleMainButtons(false);
        creditPanel.SetActive(true);
        if (isVolumePanelVisible)
        {
            isVolumePanelVisible = false;
            volumePanel.SetActive(false);
        }

        currentCreditPageIndex = 0; // Reset ke halaman awal
        UpdateCreditPageDisplay();
    }

    public void HideCredits()
    {
        ToggleMainButtons(true);
        creditPanel.SetActive(false);
    }

    public void NextCreditPage()
    {
        if (creditSprites.Count == 0) return;
        if (currentCreditPageIndex < creditSprites.Count - 1)
        {
            currentCreditPageIndex++;
            UpdateCreditPageDisplay();
        }
    }

    public void PrevCreditPage()
    {
        if (creditSprites.Count == 0) return;
        if (currentCreditPageIndex > 0)
        {
            currentCreditPageIndex--;
            UpdateCreditPageDisplay();
        }
    }

    void UpdateCreditPageDisplay()
    {
        if (creditImage != null && creditSprites.Count > 0)
        {
            creditImage.sprite = creditSprites[currentCreditPageIndex];

            if (prevCreditButton != null)
                prevCreditButton.SetActive(currentCreditPageIndex > 0);

            if (nextCreditButton != null)
                nextCreditButton.SetActive(currentCreditPageIndex < creditSprites.Count - 1);
        }
    }
    // ----------------------------

    public void ShowGameRules()
    {
        ToggleMainButtons(false);
        gameRulesPanel.SetActive(true);
        
        if (isVolumePanelVisible)
        {
            isVolumePanelVisible = false;
            volumePanel.SetActive(false);
        }

        currentPageIndex = 0;
        UpdatePageDisplay();
    }

    public void HideGameRules()
    {
        ToggleMainButtons(true);
        gameRulesPanel.SetActive(false);
    }

    public void NextPage()
    {
        if (gameRulesSprites.Count == 0) return;

        if (currentPageIndex < gameRulesSprites.Count - 1)
        {
            currentPageIndex++;
            UpdatePageDisplay();
        }
    }

    public void PrevPage()
    {
        if (gameRulesSprites.Count == 0) return;

        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePageDisplay();
        }
    }

    void UpdatePageDisplay()
    {
        if (gameRulesImage != null && gameRulesSprites.Count > 0)
        {
            gameRulesImage.sprite = gameRulesSprites[currentPageIndex];

            if (prevRuleButton != null)
            {
                prevRuleButton.SetActive(currentPageIndex > 0);
            }

            if (nextRuleButton != null)
            {
                nextRuleButton.SetActive(currentPageIndex < gameRulesSprites.Count - 1);
            }
        }
    }

    public void CloseApp()
    {
        Debug.Log("Menutup aplikasi...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void ToggleVolumePanel()
    {
        isVolumePanelVisible = !isVolumePanelVisible;
        volumePanel.SetActive(isVolumePanelVisible);
    }

    private void ToggleMainButtons(bool visible)
    {
        playButton.SetActive(visible);
        creditsButton.SetActive(visible);
        gameRulesButton.SetActive(visible);
        closeAppButton.SetActive(visible);
        volumeButton.SetActive(visible);
    }
}