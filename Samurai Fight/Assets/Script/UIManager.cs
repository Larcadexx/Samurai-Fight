using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Ini adalah Singleton yang mengelola SEMUA elemen UI
public class UIManager : MonoBehaviour
{
    public static UIManager instance; // Singleton instance

    [Header("Setup Score")]
    public Image[] scoreManusiaImages; // Array Image untuk skor manusia (misal: 5 gambar)
    public Image[] scoreAiImages;      // Array Image untuk skor AI

    [Header("Card Display")]
    public SistemKartu[] cardSlots;   // Array dari semua skrip SistemKartu di UI
    public GameObject KartuManusiaPanel; // Panel yang menampung semua cardSlots

    [Header("Text System")]
    public TextMeshProUGUI nilaiSerangText;  // Teks untuk "KEKUATAN SERANGAN: X"
    public TextMeshProUGUI systemText;       // Teks untuk pesan di tengah layar
    public TextMeshProUGUI KemenanganText;   // Teks "ANDA MENANG!" / "ANDA KALAH!"
    public TextMeshProUGUI sisaKartuDeckText; // Teks untuk sisa kartu di deck

    [Header("Panels")]
    // Referensi ke semua panel yang bisa di-togle (muncul/hilang)
    public GameObject OpsiAwalPanel;         // Panel "Melangkah" / "Serang"
    public GameObject AksiMelangkahPanel;    // Panel "Maju" / "Mundur"
    public GameObject AksiTangkisPanel;      // Panel "Tangkis Ya" / "Tangkis Tidak"
    public GameObject AksiSergapPanel;       // Panel "Sergap Ya" / "Sergap Tidak"
    public GameObject PerkuatSeranganPanel;  // Panel "Perkuat Ya" / "Perkuat Tidak"
    public GameObject KemenanganPanel;       // Panel yang muncul saat game berakhir

    [Header("Panel Tetap Ditampilkan")]
    // Elemen UI yang selalu ada (seperti skor, sisa deck)
    public GameObject[] fokusMode; 

    [Header("Action Buttons")]
    // Referensi ke tombol-tombol utama untuk di-enable/disable
    public Button MelangkahButton;
    public Button SerangButton;
    public Button MajuButton;
    public Button MundurButton;
    public Button KonfirmasiTangkisButton; // Tombol "Konfirmasi" saat memilih kartu tangkis

    private Coroutine messageCoroutine; // Variabel untuk menyimpan coroutine pesan

    void Awake()
    {
        instance = this; // Set Singleton
        
        // Inisialisasi UI saat game dimulai
        if (nilaiSerangText != null) nilaiSerangText.gameObject.SetActive(false);
        if (systemText != null) systemText.gameObject.SetActive(false);
        if (KemenanganPanel != null) KemenanganPanel.SetActive(false);
        if (KonfirmasiTangkisButton != null) KonfirmasiTangkisButton.gameObject.SetActive(false);
    }

    // Menampilkan UI lengkap untuk giliran pemain
    public void ShowFullPlayerUI()
    {
        // Sembunyikan semua panel aksi
        AksiMelangkahPanel.SetActive(false);
        AksiTangkisPanel.SetActive(false);
        AksiSergapPanel.SetActive(false);
        PerkuatSeranganPanel.SetActive(false);
        KonfirmasiTangkisButton.gameObject.SetActive(false);
        
        // Tampilkan elemen UI tetap
        foreach (var element in fokusMode)
        {
            if (element != null) element.SetActive(true);
        }

        // Tampilkan panel yang relevan untuk giliran pemain
        OpsiAwalPanel.SetActive(true);
        KartuManusiaPanel.SetActive(true);
        HideAttackStrength(); // Sembunyikan teks kekuatan serangan
        HideMessage(); // Sembunyikan pesan sistem
    }

    // Masuk ke mode fokus (misal: saat AI bergerak), sembunyikan semua UI
    public void EnterFokusMode(string message)
    {
        HideAllPlayerPanels(); // Sembunyikan semua panel
        // Sembunyikan elemen UI tetap
        foreach (var element in fokusMode)
        {
            if (element != null) element.SetActive(false);
        }
        
        // Tampilkan pesan (misal: "Giliran AI...") tanpa durasi (0f)
        ShowMessage(message, 0f); 
    }

    // Mengembalikan layout ke default (misal: akhir giliran)
    public void RestoreDefaultLayout()
    {
        HideAllPlayerPanels();
        HideAttackStrength();
        // Tampilkan lagi elemen UI tetap
        foreach (var element in fokusMode)
        {
            if (element != null) element.SetActive(true);
        }
        KartuManusiaPanel.SetActive(false); // Sembunyikan panel kartu
    }

    // Mengupdate tampilan kartu di tangan pemain
    public void UpdatePlayerHandUI(Player player)
    {
        // Pertama, sembunyikan semua slot
        foreach (var slot in cardSlots) slot.HideSlot();
        
        // Lalu, isi slot satu per satu sesuai kartu di tangan pemain
        for (int i = 0; i < player.hand.Count; i++)
        {
            // Pastikan tidak error jika pemain punya > kartu daripada slot
            if (i < cardSlots.Length) cardSlots[i].Initialize(player.hand[i]);
        }
    }
    
    // Mengatur apakah kartu di tangan bisa diklik atau tidak
    public void SetPlayerHandInteractable(bool isInteractable)
    {
        // Loop ke setiap slot kartu
        foreach (var slot in cardSlots)
        {
            // Ambil komponen Button dari slot
            Button button = slot.GetComponent<Button>();
            if (button != null)
            {
                // Atur interaktivitas tombol
                // Kartu hanya bisa interaktif JIKA slotnya aktif DAN isInteractable true
                button.interactable = slot.gameObject.activeSelf && isInteractable;
            }
        }
    }
    
    // Mereset tampilan skor (menyembunyikan semua gambar skor)
    public void ResetScoreUI()
    {
        foreach (Image img in scoreManusiaImages) img.gameObject.SetActive(false);
        foreach (Image img in scoreAiImages) img.gameObject.SetActive(false);
    }
    
    // Menampilkan panel kemenangan
    public void ShowKemenanganPanel(bool playerWon)
    {
        EnterFokusMode(""); // Masuk mode fokus (sembunyikan UI lain)
        KemenanganPanel.SetActive(true); // Tampilkan panel kemenangan
        KemenanganText.text = playerWon ? "ANDA MENANG!" : "ANDA KALAH!"; // Set teks
    }

    // Mengupdate UI skor
    public void UpdateScoreUI(Player player)
    {
        // Tentukan array gambar mana yang mau diupdate (AI atau Manusia)
        Image[] targetImages = player.isAI ? scoreAiImages : scoreManusiaImages;
        
        // Tampilkan gambar skor sesuai skor pemain
        // (player.score - 1) karena array index mulai dari 0
        if (player.score > 0 && player.score <= targetImages.Length)
            targetImages[player.score - 1].gameObject.SetActive(true);
    }

    // Menampilkan pesan sistem di tengah layar
    public void ShowMessage(string message, float duration = 2f)
    {
        if (systemText == null) return;
        
        // Jika sudah ada pesan, hentikan coroutine lama
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        
        // Mulai coroutine baru untuk menampilkan pesan
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    // Coroutine untuk mengelola durasi pesan
    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        systemText.text = message; // Set teks pesan
        systemText.gameObject.SetActive(true); // Tampilkan teks
        
        // Jika durasi > 0, tunggu selama durasi tersebut
        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);
            systemText.gameObject.SetActive(false); // Sembunyikan teks
        }
        // Jika durasi 0, teks akan tetap tampil sampai HideMessage() dipanggil
    }
    
    // Pesan khusus saat tangkisan gagal
    public void ShowTangkisanGagalMessage(int nilaiPemain, int nilaiSerangan, float duration = 3f)
    {
        string message = $"Tangkisan Gagal! Total nilai kartu Anda ({nilaiPemain}) tidak cocok dengan serangan ({nilaiSerangan}).";
        ShowMessage(message, duration);
    }

    // Menyembunyikan pesan sistem secara paksa
    public void HideMessage()
    {
        if (systemText == null) return;
        
        // Hentikan coroutine yang sedang berjalan (jika ada)
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        
        systemText.gameObject.SetActive(false); // Sembunyikan teks
    }

    // Menampilkan teks "KEKUATAN SERANGAN: X"
    public void ShowAttackStrength(int strength)
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(true);
        nilaiSerangText.text = $"KEKUATAN SERANGAN: {strength}";
    }
    
    // Mengupdate teks saat pemain memilih kartu tangkisan
    public void UpdateSelectedParryTotal(int total, int requiredStrength)
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(true);
        nilaiSerangText.text = $"SERANGAN LAWAN: {requiredStrength}  |  TANGKISAN ANDA: {total}";
    }

    // Menyembunyikan teks kekuatan serangan
    public void HideAttackStrength()
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(false);
    }

    // Mengatur apakah tombol "Melangkah" dan "Serang" bisa diklik
    public void UpdateActionButtons(bool canMove, bool canAttack)
    {
        MelangkahButton.interactable = canMove;
        SerangButton.interactable = canAttack;
    }

    // Mengatur apakah tombol "Maju" dan "Mundur" bisa diklik
    public void UpdateMoveDirectionButtons(bool canMoveForward, bool canMoveBackward)
    {
        MajuButton.interactable = canMoveForward;
        MundurButton.interactable = canMoveBackward;
    }

    // Fungsi utilitas untuk menyembunyikan SEMUA panel aksi pemain
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

    // Mengupdate teks sisa kartu di deck
    public void UpdateMainDeckUI(int cardCount)
    {
        if (sisaKartuDeckText == null) return;
        sisaKartuDeckText.text = cardCount > 0 ? "Sisa: " + cardCount : "Deck Habis!";
    }
}