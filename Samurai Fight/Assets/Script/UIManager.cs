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
    // DIHAPUS: CanvasGroup tidak lagi digunakan.
    // public CanvasGroup kartuManusiaCanvasGroup; 

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

    public void ShowFullPlayerUI()
    {
        AksiMelangkahPanel.SetActive(false);
        AksiTangkisPanel.SetActive(false);
        AksiSergapPanel.SetActive(false);
        PerkuatSeranganPanel.SetActive(false);
        KonfirmasiTangkisButton.gameObject.SetActive(false);
        
        foreach (var element in fokusMode)
        {
            if (element != null) element.SetActive(true);
        }

        OpsiAwalPanel.SetActive(true);
        KartuManusiaPanel.SetActive(true);
        HideAttackStrength();
        HideMessage();
    }

    public void EnterFokusMode(string message)
    {
        HideAllPlayerPanels();
        foreach (var element in fokusMode)
        {
            if (element != null) element.SetActive(false);
        }
        
        ShowMessage(message, 0f);
    }

    public void RestoreDefaultLayout()
    {
        HideAllPlayerPanels();
        HideAttackStrength();
        foreach (var element in fokusMode)
        {
            if (element != null) element.SetActive(true);
        }
        KartuManusiaPanel.SetActive(false); 
    }

    public void UpdatePlayerHandUI(Player player)
    {
        foreach (var slot in cardSlots) slot.HideSlot();
        for (int i = 0; i < player.hand.Count; i++)
        {
            if (i < cardSlots.Length) cardSlots[i].Initialize(player.hand[i]);
        }
    }
    
    // UBAH: Fungsi ini disederhanakan untuk langsung memanipulasi tombol.
    public void SetPlayerHandInteractable(bool isInteractable)
    {
        // Langsung iterasi ke setiap slot kartu.
        foreach (var slot in cardSlots)
        {
            // Dapatkan komponen Button dari slot.
            Button button = slot.GetComponent<Button>();
            if (button != null)
            {
                // Atur interaktivitas tombol, hanya jika slotnya aktif.
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
        EnterFokusMode("");
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
    
    public void ShowTangkisanGagalMessage(int nilaiPemain, int nilaiSerangan, float duration = 3f)
    {
        string message = $"Tangkisan Gagal! Total nilai kartu Anda ({nilaiPemain}) tidak cocok dengan serangan ({nilaiSerangan}).";
        ShowMessage(message, duration);
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
    
    public void UpdateSelectedParryTotal(int total, int requiredStrength)
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(true);
        nilaiSerangText.text = $"SERANGAN LAWAN: {requiredStrength}  |  TANGKISAN ANDA: {total}";
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