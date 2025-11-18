using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Header("Tombol Utama")]
    public GameObject playButton;
    public GameObject gameRuleButton;
    public GameObject creditButton;
    public GameObject closeAppButton;
    public GameObject volumeButton;

    [Header("Panel")]
    public GameObject CreditPanel;
    public GameObject GameRulePanel;
    public GameObject VolumePanel;

    [Header("Game Rule")]
    public Image GameRulesImage;       
    public List<Sprite> GameRulesSprites; 

    private int currentPageIndex = 0;
    private bool isVolumePanelVisible = false;

    void Start()
    {
        playButton.SetActive(true);
        creditButton.SetActive(true);
        gameRuleButton.SetActive(true);
        closeAppButton.SetActive(true);
        volumeButton.SetActive(true);
        
        if (CreditPanel != null) CreditPanel.SetActive(false);
        if (GameRulePanel != null) GameRulePanel.SetActive(false);
        if (VolumePanel != null) VolumePanel.SetActive(false);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Gameplay");
    }

    public void ShowCredits()
    {
        ToggleMainButtons(false);
        CreditPanel.SetActive(true);
        if (isVolumePanelVisible)
        {
            isVolumePanelVisible = false;
            VolumePanel.SetActive(false);
        }
    }

    public void HideCredits()
    {
        ToggleMainButtons(true);
        CreditPanel.SetActive(false);
    }

    public void ShowGameRules()
    {
        ToggleMainButtons(false);
        GameRulePanel.SetActive(true);
        
        if (isVolumePanelVisible)
        {
            isVolumePanelVisible = false;
            VolumePanel.SetActive(false);
        }

        currentPageIndex = 0;
        UpdatePageDisplay();
    }

    public void HideGameRules()
    {
        ToggleMainButtons(true);
        GameRulePanel.SetActive(false);
    }

    public void NextPage()
    {
        if (GameRulesSprites.Count == 0) return;

        currentPageIndex++;
        if (currentPageIndex >= GameRulesSprites.Count)
            currentPageIndex = 0;
            
        UpdatePageDisplay();
    }

    public void PrevPage()
    {
        if (GameRulesSprites.Count == 0) return;

        currentPageIndex--;
        if (currentPageIndex < 0)
            currentPageIndex = GameRulesSprites.Count - 1;
            
        UpdatePageDisplay();
    }

    void UpdatePageDisplay()
    {
        if (GameRulesImage != null && GameRulesSprites.Count > 0)
        {
            GameRulesImage.sprite = GameRulesSprites[currentPageIndex];
        }
        else
        {
            Debug.LogWarning("GameRules Image atau GameRules Sprites belum di-assign di Inspector!");
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
        VolumePanel.SetActive(isVolumePanelVisible);
    }

    private void ToggleMainButtons(bool visible)
    {
        playButton.SetActive(visible);
        creditButton.SetActive(visible);
        gameRuleButton.SetActive(visible);
        closeAppButton.SetActive(visible);
        volumeButton.SetActive(visible);
    }
}