using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Setup Score")]
    public Image[] scoreManusiaImages;
    public Image[] scoreAiImages;

    [Header("Card Display")]
    public SistemKartu[] cardSlots;
    public GameObject KartuManusiaPanel;

    [Header("Text System")]
    public TextMeshProUGUI nilaiSerangText;
    public TextMeshProUGUI systemText;
    public TextMeshProUGUI KemenanganText;
    public TextMeshProUGUI sisaKartuDeckText;

    [Header("Panels")]
    public GameObject OpsiAwalPanel;
    public GameObject AksiMelangkahPanel;
    public GameObject AksiTangkisPanel;
    public GameObject AksiSergapPanel;
    public GameObject PerkuatSeranganPanel;
    public GameObject KemenanganPanel;

    // Pastikan array ini sudah diisi di Inspector Unity
    [Header("Panel Tetap Ditampilkan")]
    public GameObject[] fokusMode;

    [Header("Action Buttons")]
    public Button MelangkahButton;
    public Button SerangButton;
    public Button MajuButton;
    public Button MundurButton;
    public Button KonfirmasiTangkisButton;

    private Coroutine messageCoroutine;

    void Awake()
    {
        instance = this;
        if (nilaiSerangText != null) nilaiSerangText.gameObject.SetActive(false);
        if (systemText != null) systemText.gameObject.SetActive(false);
        if (KemenanganPanel != null) KemenanganPanel.SetActive(false);
        if (KonfirmasiTangkisButton != null) KonfirmasiTangkisButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Menampilkan semua UI utama untuk giliran pemain (tampilan penuh).
    /// </summary>
    public void ShowFullPlayerUI()
    {
        // Sembunyikan semua panel aksi spesifik
        AksiMelangkahPanel.SetActive(false);
        AksiTangkisPanel.SetActive(false);
        AksiSergapPanel.SetActive(false);
        PerkuatSeranganPanel.SetActive(false);
        KonfirmasiTangkisButton.gameObject.SetActive(false);
        
        // Tampilkan semua elemen yang sebelumnya disembunyikan oleh fokus mode
        foreach (var element in fokusMode)
        {
            if (element != null) element.SetActive(true);
        }

        // Tampilkan panel yang relevan untuk awal giliran
        OpsiAwalPanel.SetActive(true);
        KartuManusiaPanel.SetActive(true); // Langsung tampilkan kartu
        HideAttackStrength();
        HideMessage();
    }

    /// <summary>
    /// Masuk ke Fokus Mode: menyembunyikan UI utama dan menampilkan pesan.
    /// </summary>
    public void EnterFokusMode(string message)
    {
        // Sembunyikan semua panel dan UI utama
        HideAllPlayerPanels();
        foreach (var element in fokusMode)
        {
            if (element != null) element.SetActive(false);
        }
        
        // Selalu tampilkan pesan sistem
        ShowMessage(message, 0f);
    }

    /// <summary>
    /// Mengembalikan layout ke default saat giliran AI atau ronde berakhir.
    /// </summary>
    public void RestoreDefaultLayout()
    {
        HideAllPlayerPanels();
        HideAttackStrength();
        // Tampilkan kembali elemen utama saat giliran AI (jika diinginkan)
        foreach (var element in fokusMode)
        {
            if (element != null) element.SetActive(true);
        }
        // Pastikan panel kartu manusia tidak terlihat saat giliran AI
        KartuManusiaPanel.SetActive(false); 
    }

    // --- Sisa fungsi ---

    public void UpdatePlayerHandUI(Player player)
    {
        foreach (var slot in cardSlots) slot.HideSlot();
        for (int i = 0; i < player.hand.Count; i++)
        {
            if (i < cardSlots.Length) cardSlots[i].Initialize(player.hand[i]);
        }
    }
    
    public void SetPlayerHandInteractable(bool isInteractable)
    {
        foreach (var slot in cardSlots)
        {
            Button button = slot.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = slot.gameObject.activeSelf && isInteractable;
            }
        }
    }
    
    public void ResetScoreUI()
    {
        foreach (Image img in scoreManusiaImages) img.gameObject.SetActive(false);
        foreach (Image img in scoreAiImages) img.gameObject.SetActive(false);
    }
    
    public void ShowKemenanganPanel(bool playerWon)
    {
        EnterFokusMode(""); // Bersihkan layar
        KemenanganPanel.SetActive(true);
        KemenanganText.text = playerWon ? "ANDA MENANG!" : "ANDA KALAH!";
    }

    public void UpdateScoreUI(Player player)
    {
        Image[] targetImages = player.isAI ? scoreAiImages : scoreManusiaImages;
        if (player.score > 0 && player.score <= targetImages.Length)
            targetImages[player.score - 1].gameObject.SetActive(true);
    }

    public void ShowMessage(string message, float duration = 2f)
    {
        if (systemText == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        systemText.text = message;
        systemText.gameObject.SetActive(true);
        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);
            systemText.gameObject.SetActive(false);
        }
    }

    public void HideMessage()
    {
        if (systemText == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        systemText.gameObject.SetActive(false);
    }

    public void ShowAttackStrength(int strength)
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(true);
        nilaiSerangText.text = $"KEKUATAN SERANGAN: {strength}";
    }

    public void UpdateSelectedParryTotal(int total)
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(true);
        nilaiSerangText.text = $"TOTAL TANGKISAN: {total}";
    }

    public void HideAttackStrength()
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(false);
    }

    public void UpdateActionButtons(bool canMove, bool canAttack)
    {
        MelangkahButton.interactable = canMove;
        SerangButton.interactable = canAttack;
    }

    public void UpdateMoveDirectionButtons(bool canMoveForward, bool canMoveBackward)
    {
        MajuButton.interactable = canMoveForward;
        MundurButton.interactable = canMoveBackward;
    }

    public void HideAllPlayerPanels()
    {
        OpsiAwalPanel.SetActive(false);
        AksiMelangkahPanel.SetActive(false);
        AksiTangkisPanel.SetActive(false);
        AksiSergapPanel.SetActive(false);
        PerkuatSeranganPanel.SetActive(false);
        KonfirmasiTangkisButton.gameObject.SetActive(false);
        KartuManusiaPanel.SetActive(false);
    }

    public void UpdateMainDeckUI(int cardCount)
    {
        if (sisaKartuDeckText == null) return;
        sisaKartuDeckText.text = cardCount > 0 ? "Sisa: " + cardCount : "Deck Habis!";
    }
}