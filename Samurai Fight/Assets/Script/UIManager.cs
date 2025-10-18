using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Setup Score")]
    public Image[] scoreManusiaImages;
    public Image[] scoreAiImages;

    [Header("Card Display")]
    public SistemKartu[] cardSlots;
    public GameObject KartuManusiaPanel;

    [Header("Posisi Panel Kartu")]
    public Transform posisiPanelDefault;
    public Transform posisiPanelKanan;
    public Transform posisiPanelBawah;

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
        if (AksiMelangkahPanel != null) AksiMelangkahPanel.SetActive(false);
        if (AksiTangkisPanel != null) AksiTangkisPanel.SetActive(false);
        if (AksiSergapPanel != null) AksiSergapPanel.SetActive(false);
        if (PerkuatSeranganPanel != null) PerkuatSeranganPanel.SetActive(false);
        if (KonfirmasiTangkisButton != null) KonfirmasiTangkisButton.gameObject.SetActive(false);

        if (fokusMode != null)
        {
            foreach (var element in fokusMode)
                if (element != null) element.SetActive(true);
        }

        if (OpsiAwalPanel != null) OpsiAwalPanel.SetActive(true);
        if (KartuManusiaPanel != null) KartuManusiaPanel.SetActive(true);

        HideAttackStrength();
        HideMessage();
    }

    public void EnterFokusMode(string message)
    {
        HideAllPlayerPanels();
        if (fokusMode != null)
        {
            foreach (var element in fokusMode)
                if (element != null) element.SetActive(false);
        }
        ShowMessage(message, 0f);
    }

    public void RestoreDefaultLayout()
    {
        HideAllPlayerPanels();
        HideAttackStrength();
        if (fokusMode != null)
        {
            foreach (var element in fokusMode)
                if (element != null) element.SetActive(true);
        }
        if (KartuManusiaPanel != null) KartuManusiaPanel.SetActive(false);
    }

    public void UpdatePlayerHandUI(Player player)
    {
        if (cardSlots == null) return;
        foreach (var slot in cardSlots) slot.HideSlot();
        for (int i = 0; i < player.hand.Count; i++)
        {
            if (i < cardSlots.Length)
                cardSlots[i].Initialize(player.hand[i]);
        }
    }

    public void SetPlayerHandInteractable(bool isInteractable)
    {
        if (cardSlots == null) return;
        foreach (var slot in cardSlots)
        {
            if (slot == null) continue;
            var btn = slot.GetComponent<Button>();
            if (btn != null)
                btn.interactable = slot.gameObject.activeSelf && isInteractable;
        }
    }

    public void ResetScoreUI()
    {
        if (scoreManusiaImages != null)
        {
            foreach (Image img in scoreManusiaImages) if (img != null) img.gameObject.SetActive(false);
        }
        if (scoreAiImages != null)
        {
            foreach (Image img in scoreAiImages) if (img != null) img.gameObject.SetActive(false);
        }
    }

    public void ShowKemenanganPanel(bool playerWon)
    {
        EnterFokusMode("");
        if (KemenanganPanel != null) KemenanganPanel.SetActive(true);
        if (KemenanganText != null) KemenanganText.text = playerWon ? "ANDA MENANG!" : "ANDA KALAH!";
    }

    public void UpdateScoreUI(Player player)
    {
        Image[] targetImages = player.isAI ? scoreAiImages : scoreManusiaImages;
        if (targetImages == null) return;
        if (player.score > 0 && player.score <= targetImages.Length)
            if (targetImages[player.score - 1] != null)
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
        if (systemText == null) yield break;
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
        nilaiSerangText.text = $"TANGKISAN ANDA: {total}";
    }

    public void HideAttackStrength()
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(false);
    }

    public void UpdateActionButtons(bool canMove, bool canAttack)
    {
        if (MelangkahButton != null) MelangkahButton.interactable = canMove;
        if (SerangButton != null) SerangButton.interactable = canAttack;
    }

    public void UpdateMoveDirectionButtons(bool canMoveForward, bool canMoveBackward)
    {
        if (MajuButton != null) MajuButton.interactable = canMoveForward;
        if (MundurButton != null) MundurButton.interactable = canMoveBackward;
    }

    public void HideAllPlayerPanels()
    {
        if (OpsiAwalPanel != null) OpsiAwalPanel.SetActive(false);
        if (AksiMelangkahPanel != null) AksiMelangkahPanel.SetActive(false);
        if (AksiTangkisPanel != null) AksiTangkisPanel.SetActive(false);
        if (AksiSergapPanel != null) AksiSergapPanel.SetActive(false);
        if (PerkuatSeranganPanel != null) PerkuatSeranganPanel.SetActive(false);
        if (KonfirmasiTangkisButton != null) KonfirmasiTangkisButton.gameObject.SetActive(false);
        if (KartuManusiaPanel != null) KartuManusiaPanel.SetActive(false);
    }

    public void UpdateMainDeckUI(int cardCount)
    {
        if (sisaKartuDeckText == null) return;
        sisaKartuDeckText.text = cardCount > 0 ? "Sisa: " + cardCount : "Deck Habis!";
    }

    public void PindahkanPanelKartu(Transform targetPosisi)
    {
        if (KartuManusiaPanel != null && targetPosisi != null)
        {
            KartuManusiaPanel.transform.position = targetPosisi.position;
        }
    }
}
