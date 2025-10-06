// UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    private Sprite defaultScoreSprite; 

    [Header("Asset Gambar Skor")]
    public Sprite scorePenuh;

    [Header("Score Display")]
    public Image[] scoreManusiaImages;
    public Image[] scoreAiImages;

    [Header("Card Display")]
    public GameObject cardPrefab;
    public Transform playerHandPanel;

    [Header("Decks")]
    public TextMeshProUGUI kartuDeckText;

    [Header("Panels")]
    public GameObject OpsiAwalPanel;
    public GameObject AksiMelangkahPanel; // Panel baru ditambahkan di sini

    void Awake() 
    { 
        instance = this; 
        if (scoreManusiaImages.Length > 0)
        {
            defaultScoreSprite = scoreManusiaImages[0].sprite;
        }
    }

    public void ShowOpsiAwalPanel(bool show)
    {
        OpsiAwalPanel.SetActive(show);
    }
    
    // Fungsi baru untuk menampilkan/menyembunyikan panel arah melangkah
    public void ShowAksiMelangkahPanel(bool show)
    {
        AksiMelangkahPanel.SetActive(show);
    }

    public void HideAllPlayerPanels()
    {
        OpsiAwalPanel.SetActive(false);
        AksiMelangkahPanel.SetActive(false); // Ditambahkan di sini
    }

    // Fungsi UpdateScoreUI, UpdatePlayerHandUI, dan UpdateMainDeckUI tidak berubah
    // Pastikan Anda memiliki kode untuk fungsi-fungsi tersebut dari file Anda sebelumnya.
    // Jika tidak, beri tahu saya, saya akan tambahkan kode placeholder.
    public void UpdateScoreUI(Player player)
    {
        // ... (kode Anda sebelumnya) ...
    }
    public void UpdatePlayerHandUI(Player player)
    {
        // Hapus kartu lama
        foreach (Transform child in playerHandPanel)
        {
            Destroy(child.gameObject);
        }
        // Buat kartu baru
        foreach (Card card in player.hand)
        {
            GameObject cardGO = Instantiate(cardPrefab, playerHandPanel);
            cardGO.GetComponent<CardController>().Initialize(card);
        }
    }
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