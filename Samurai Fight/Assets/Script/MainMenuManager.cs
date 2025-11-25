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

    [Header("Game Rule")]
    public Image gameRulesImage;       
    public List<Sprite> gameRulesSprites; 

    private int currentPageIndex = 0;
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
            TransisiScene.Instance.PindahKeScene("Gameplay");
        }
        else
        {
            SceneManager.LoadScene("Gameplay");
        }
    }

    public void ShowCredits()
    {
        ToggleMainButtons(false);
        creditPanel.SetActive(true);
        if (isVolumePanelVisible)
        {
            isVolumePanelVisible = false;
            volumePanel.SetActive(false);
        }
    }

    public void HideCredits()
    {
        ToggleMainButtons(true);
        creditPanel.SetActive(false);
    }

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

        currentPageIndex++;
        if (currentPageIndex >= gameRulesSprites.Count)
            currentPageIndex = 0;
            
        UpdatePageDisplay();
    }

    public void PrevPage()
    {
        if (gameRulesSprites.Count == 0) return;

        currentPageIndex--;
        if (currentPageIndex < 0)
            currentPageIndex = gameRulesSprites.Count - 1;
            
        UpdatePageDisplay();
    }

    void UpdatePageDisplay()
    {
        if (gameRulesImage != null && gameRulesSprites.Count > 0)
        {
            gameRulesImage.sprite = gameRulesSprites[currentPageIndex];
        }
        else
        {
            Debug.LogWarning("gameRulesImage atau gameRulesSprites belum di-assign di Inspector!");
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
