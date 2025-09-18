using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
   [Header("UI Panels")]
    public GameObject mainButtonsPanel;
    public GameObject creditPanel;
    public GameObject gamerulePanel;

    [Header("Buttons")]
    public Button playButton;
    public Button creditButton;
    public Button gameRuleButton;
    public Button backFromCreditButton;
    public Button backFromGameRuleButton;
    public Button playButtonFromCredit;
    public Button playButtonFromGameRule;
    
    [Header("Game Rule Pagination")]
    public List<GameObject> rulePages;
    public Button nextPageButton;
    public Button prevPageButton;
    private int currentPageIndex = 0;

    void Awake()
    {
        // Pendaftaran listener tidak berubah
        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (playButtonFromCredit != null) playButtonFromCredit.onClick.AddListener(PlayGame);
        if (playButtonFromGameRule != null) playButtonFromGameRule.onClick.AddListener(PlayGame);
        if (creditButton != null) creditButton.onClick.AddListener(ShowCreditPanel);
        if (gameRuleButton != null) gameRuleButton.onClick.AddListener(ShowGameRulePanel);
        if (backFromCreditButton != null) backFromCreditButton.onClick.AddListener(HideCreditPanel);
        if (backFromGameRuleButton != null) backFromGameRuleButton.onClick.AddListener(HideGameRulePanel);

        if (nextPageButton != null) nextPageButton.onClick.AddListener(GoToNextPage);
        if (prevPageButton != null) prevPageButton.onClick.AddListener(GoToPrevPage);
    }

    void Start()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (creditPanel != null) creditPanel.SetActive(false);
        if (gamerulePanel != null) gamerulePanel.SetActive(false);
    }

    public void ShowGameRulePanel()
    {
        if (gamerulePanel != null) gamerulePanel.SetActive(true);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        
        currentPageIndex = 0;
        UpdatePageDisplay();
    }
    
    // --- FUNGSI-FUNGSI DI BAWAH INI TETAP SAMA ---
    public void PlayGame() { SceneManager.LoadScene("Gameplay"); }
    public void ShowCreditPanel() { if (creditPanel != null) creditPanel.SetActive(true); if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false); }
    public void HideCreditPanel() { if (creditPanel != null) creditPanel.SetActive(false); if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true); }
    public void HideGameRulePanel() { if (gamerulePanel != null) gamerulePanel.SetActive(false); if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true); }


    // --- FUNGSI PAGINASI (YANG DIUBAH) ---

    void GoToNextPage()
    {
        // Pindah ke halaman selanjutnya
        currentPageIndex++;
        
        // JIKA sudah melewati halaman terakhir, KEMBALI ke halaman pertama (index 0)
        if (currentPageIndex >= rulePages.Count)
        {
            currentPageIndex = 0;
        }
        
        UpdatePageDisplay();
    }

    void GoToPrevPage()
    {
        // Pindah ke halaman sebelumnya
        currentPageIndex--;

        // JIKA sudah melewati halaman pertama, LOMPAT ke halaman terakhir
        if (currentPageIndex < 0)
        {
            currentPageIndex = rulePages.Count - 1;
        }

        UpdatePageDisplay();
    }

    void UpdatePageDisplay()
    {
        // Pastikan ada halaman yang terdaftar untuk menghindari error
        if (rulePages.Count == 0) return;

        // Loop untuk menyalakan halaman yang aktif dan mematikan yang lain
        for (int i = 0; i < rulePages.Count; i++)
        {
            rulePages[i].SetActive(i == currentPageIndex);
        }

        // BAGIAN UNTUK MENYEMBUNYIKAN TOMBOL DIHAPUS
        // Sekarang tombol akan selalu terlihat.
    }
}
