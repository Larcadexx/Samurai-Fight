// Salin dan ganti seluruh isi script UIManager.cs Anda dengan kode ini

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
    public GameObject InfoPanel;

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
        if (InfoPanel != null) InfoPanel.SetActive(false);
        if (KonfirmasiTangkisButton != null) KonfirmasiTangkisButton.gameObject.SetActive(false);
    }

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
        HideAllPlayerPanels();
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
        nilaiSerangText.text = $"TANGKISAN ANDA: {total} / {requiredStrength}";
    }

    public void HideAttackStrength()
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(false);
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
    
    // =======================================================================
    // <-- FUNGSI KONTROL UI BARU YANG LEBIH "PINTAR" ADA DI SINI SEMUA -->
    // =======================================================================

    public void RestoreDefaultLayout()
    {
        HideAllPlayerPanels();
        HideAttackStrength();
        KartuManusiaPanel.SetActive(false);
    }
    
    public void TampilkanUI_PilihAksiAwal(bool bisaMelangkah, bool bisaMenyerang)
    {
        RestoreDefaultLayout();
        ShowMessage("Giliran Anda! Pilih aksi: Melangkah atau Serang.", 0f);
        OpsiAwalPanel.SetActive(true);
        InfoPanel.SetActive(true);
        PindahkanPanelKartu(posisiPanelDefault);
        KartuManusiaPanel.SetActive(true);
        MelangkahButton.interactable = bisaMelangkah;
        SerangButton.interactable = bisaMenyerang;
        SetPlayerHandInteractable(false);
    }

    public void TampilkanUI_PilihArahLangkah(bool bisaMaju, bool bisaMundur)
    {
        HideAllPlayerPanels();
        ShowMessage("Pilih arah tujuan Anda.", 0f);
        AksiMelangkahPanel.SetActive(true);
        MajuButton.interactable = bisaMaju;
        MundurButton.interactable = bisaMundur;
    }

    public void TampilkanUI_PilihKartuUntukAksi(string jenisAksi, string detailAksi = "")
    {
        HideAllPlayerPanels();
        string message = "";
        Transform targetPosisi = posisiPanelDefault;

        switch (jenisAksi)
        {
            case "melangkah":
                message = $"Anda melangkah {detailAksi}. Pilih satu kartu untuk menentukan jarak.";
                targetPosisi = posisiPanelKanan;
                break;
            case "serang":
                message = "Pilih kartu yang nilainya sama dengan jarak Anda ke lawan.";
                targetPosisi = posisiPanelBawah;
                break;
            case "perkuat":
                message = "Pilih satu kartu tambahan untuk memperkuat serangan.";
                targetPosisi = posisiPanelKanan;
                break;
            case "sergap":
                message = "Pilih kartu untuk melakukan Sergap.";
                targetPosisi = posisiPanelBawah;
                break;
            case "serangbalik":
                message = "Pilih satu kartu untuk melakukan Serang Balik.";
                targetPosisi = posisiPanelDefault; // Atau posisi lain yang sesuai
                break;
        }

        ShowMessage(message, 0f);
        PindahkanPanelKartu(targetPosisi);
        KartuManusiaPanel.SetActive(true);
        SetPlayerHandInteractable(true);
    }

    public void TampilkanUI_TawarkanPerkuatSerangan()
    {
        PerkuatSeranganPanel.SetActive(true);
        ShowMessage("Serangan awal siap. Ingin perkuat dengan kartu tambahan?", 0f);
    }
    
    public void TampilkanUI_TawarkanSergap()
    {
        ShowMessage("Langkah berhasil! Anda kini dalam jangkauan. Lakukan Sergap?", 0f);
        AksiSergapPanel.SetActive(true);
    }

    public void TampilkanUI_TangkisSerangan(int kekuatan, bool isSerangBalik)
    {
        HideAllPlayerPanels();
        HideAttackStrength();
        string message = isSerangBalik ? $"AI melakukan Serang Balik dengan kekuatan {kekuatan}! Tangkis serangan ini?" : $"Anda diserang dengan kekuatan {kekuatan}! Tangkis serangan ini?";
        ShowMessage(message, 0f);
        AksiTangkisPanel.SetActive(true);
    }

    public void TampilkanUI_PilihKartuTangkis(int kekuatanSerangan)
    {
        AksiTangkisPanel.SetActive(false);
        KonfirmasiTangkisButton.gameObject.SetActive(true);
        ShowMessage($"Pilih kartu dengan total nilai {kekuatanSerangan}, lalu tekan Konfirmasi.", 0f);
        PindahkanPanelKartu(posisiPanelKanan);
        KartuManusiaPanel.SetActive(true);
        SetPlayerHandInteractable(true);
        UpdateSelectedParryTotal(0, kekuatanSerangan);
    }

    public void SembunyikanTombolKonfirmasiTangkis()
    {
        if (KonfirmasiTangkisButton != null) KonfirmasiTangkisButton.gameObject.SetActive(false);
    }
    
    public void SembunyikanPanelKartu()
    {
        if (KartuManusiaPanel != null) KartuManusiaPanel.SetActive(false);
    }
    
    public void TampilkanInfoGiliranAI()
    {
        if(InfoPanel != null) InfoPanel.SetActive(true);
        ShowMessage("Giliran AI...", 0f);
    }

    public void HideAllUIsForRoundEnd()
    {
        HideAllPlayerPanels();
        HideAttackStrength();
        HideMessage();
    }
    
    public void HideAllPlayerPanels()
    {
        OpsiAwalPanel.SetActive(false);
        AksiMelangkahPanel.SetActive(false);
        AksiTangkisPanel.SetActive(false);
        AksiSergapPanel.SetActive(false);
        PerkuatSeranganPanel.SetActive(false);
        if (InfoPanel != null) InfoPanel.SetActive(false);
        KonfirmasiTangkisButton.gameObject.SetActive(false);
        KartuManusiaPanel.SetActive(false);
    }
}