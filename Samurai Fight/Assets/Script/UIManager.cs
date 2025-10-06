// UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Script ini bertugas mengelola semua elemen User Interface (UI) di game.
public class UIManager : MonoBehaviour
{
    // Singleton instance: cara mudah untuk mengakses script ini dari script lain.
    public static UIManager instance;
    // Menyimpan sprite default untuk skor (opsional).
    private Sprite defaultScoreSprite; 

    [Header("Asset Gambar Skor")]
    public Sprite scorePenuh;

    [Header("Score Display")]
    // Array untuk menampung gambar-gambar skor pemain.
    public Image[] scoreManusiaImages;
    public Image[] scoreAiImages;

    [Header("Card Display")]
    // Prefab (template) dari objek kartu yang akan ditampilkan.
    public GameObject cardPrefab;
    // Panel UI tempat kartu-kartu pemain akan diletakkan.
    public Transform playerHandPanel;

    [Header("Decks")]
    // Teks untuk menampilkan jumlah sisa kartu di dek.
    public TextMeshProUGUI kartuDeckText;

    [Header("Panels")]
    // Referensi ke panel-panel UI untuk berbagai aksi.
    public GameObject OpsiAwalPanel;
    public GameObject AksiMelangkahPanel;

    // Awake dipanggil sebelum Start, bagus untuk inisialisasi awal.
    void Awake() 
    { 
        // Mengatur singleton instance.
        instance = this; 
        if (scoreManusiaImages.Length > 0)
        {
            defaultScoreSprite = scoreManusiaImages[0].sprite;
        }
    }

    // Fungsi untuk menampilkan atau menyembunyikan panel opsi awal.
    public void ShowOpsiAwalPanel(bool show)
    {
        OpsiAwalPanel.SetActive(show);
    }
    
    // Fungsi untuk menampilkan atau menyembunyikan panel pilihan arah melangkah.
    public void ShowAksiMelangkahPanel(bool show)
    {
        AksiMelangkahPanel.SetActive(show);
    }

    // Fungsi untuk menyembunyikan semua panel aksi pemain.
    public void HideAllPlayerPanels()
    {
        OpsiAwalPanel.SetActive(false);
        AksiMelangkahPanel.SetActive(false);
    }

    // Fungsi untuk mengupdate tampilan skor di UI.
    public void UpdateScoreUI(Player player)
    {
        // ... (kode Anda sebelumnya) ...
    }
    
    // Fungsi untuk mengupdate tampilan kartu di tangan pemain.
    public void UpdatePlayerHandUI(Player player)
    {
        // Menghapus semua objek kartu lama yang ada di panel.
        foreach (Transform child in playerHandPanel)
        {
            Destroy(child.gameObject);
        }
        // Membuat objek kartu baru untuk setiap kartu di tangan pemain.
        foreach (Card card in player.hand)
        {
            // Membuat instance baru dari prefab kartu.
            GameObject cardGO = Instantiate(cardPrefab, playerHandPanel);
            // Mengirim data kartu ke CardController agar nilainya tampil.
            cardGO.GetComponent<CardController>().Initialize(card);
        }
    }
    
    // Fungsi untuk mengupdate teks jumlah sisa kartu dek.
    public void UpdateMainDeckUI(int cardCount)
    {
        if (cardCount > 0)
        {
            kartuDeckText.gameObject.SetActive(true);
            kartuDeckText.text = " " + cardCount;
        }
        else
        {
            kartuDeckText.text = "Deck Habis!";
        }
    }
}