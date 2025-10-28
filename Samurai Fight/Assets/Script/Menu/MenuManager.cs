using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Pages")]
    public List<GameObject> Pages;
    private int currentPageIndex = 0;

    private bool isVolumePanelVisible = false;

    void Start()
    {
        playButton.SetActive(true);
        creditButton.SetActive(true);
        gameRuleButton.SetActive(true);
        closeAppButton.SetActive(true);
        volumeButton.SetActive(true);
        CreditPanel.SetActive(false);
        GameRulePanel.SetActive(false);
        VolumePanel.SetActive(false);
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
        currentPageIndex++;
        if (currentPageIndex >= Pages.Count)
            currentPageIndex = 0;
        UpdatePageDisplay();
    }

    public void PrevPage()
    {
        currentPageIndex--;
        if (currentPageIndex < 0)
            currentPageIndex = Pages.Count - 1;
        UpdatePageDisplay();
    }

    void UpdatePageDisplay()
    {
        for (int i = 0; i < Pages.Count; i++)
            Pages[i].SetActive(i == currentPageIndex);
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