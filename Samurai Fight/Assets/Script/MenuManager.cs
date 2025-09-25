using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Header("Tombol Utama")]
    public GameObject playButton;
    public GameObject gameRuleButton;
    public GameObject creditButton;


    [Header("Panel")]
    public GameObject CreditPanel;
    public GameObject GameRulePanel;

    [Header("Pages")]
    public List<GameObject> Pages;
    private int currentPageIndex = 0;

    void Start()
    {
        playButton.SetActive(true);
        creditButton.SetActive(true);
        gameRuleButton.SetActive(true);

        CreditPanel.SetActive(false);
        GameRulePanel.SetActive(false);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Gameplay");
    }

    public void ShowCredits()
    {
        playButton.SetActive(false);
        creditButton.SetActive(false);
        gameRuleButton.SetActive(false);

        CreditPanel.SetActive(true);
    }

    public void HideCredits()
    {
        playButton.SetActive(true);
        creditButton.SetActive(true);
        gameRuleButton.SetActive(true);

        CreditPanel.SetActive(false);
    }

    public void ShowGameRules()
    {
        playButton.SetActive(false);
        creditButton.SetActive(false);
        gameRuleButton.SetActive(false);
        
        GameRulePanel.SetActive(true);

        currentPageIndex = 0;
        UpdatePageDisplay();
    }
<<<<<<< Updated upstream
    
    // --- FUNGSI-FUNGSI DI BAWAH INI TETAP SAMA ---
    public void PlayGame() { SceneManager.LoadScene("Game"); }
    public void ShowCreditPanel() { if (creditPanel != null) creditPanel.SetActive(true); if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false); }
    public void HideCreditPanel() { if (creditPanel != null) creditPanel.SetActive(false); if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true); }
    public void HideGameRulePanel() { if (gamerulePanel != null) gamerulePanel.SetActive(false); if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true); }
=======
>>>>>>> Stashed changes

    public void HideGameRules()
    {
        playButton.SetActive(true);
        creditButton.SetActive(true);
        gameRuleButton.SetActive(true);

        GameRulePanel.SetActive(false);
    }

    public void NextPage()
    {
        currentPageIndex++;
        if (currentPageIndex >= Pages.Count)
        {
            currentPageIndex = 0;
        }
        UpdatePageDisplay();
    }

    public void PrevPage()
    {
        currentPageIndex--;
        if (currentPageIndex < 0)
        {
            currentPageIndex = Pages.Count - 1;
        }
        UpdatePageDisplay();
    }

    void UpdatePageDisplay()
    {
        for (int i = 0; i < Pages.Count; i++)
        {
            Pages[i].SetActive(i == currentPageIndex);
        }
    }
}