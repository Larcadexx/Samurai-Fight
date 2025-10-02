// UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Script ini adalah "manajer visual" yang mengurus semua yang tampil di layar.
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

    void Awake() 
    { 
        instance = this; 
        if (scoreManusiaImages.Length > 0)
        {
            defaultScoreSprite = scoreManusiaImages[0].sprite;
        }
    }

    // Mengatur tampilan skor
    public void UpdateScoreUI(Player player)
    {
        // ... (kode tidak berubah) ...
    }

    // Membuat dan menampilkan kartu di tangan pemain
    public void UpdatePlayerHandUI(Player player)
    {
        // ... (kode tidak berubah) ...
    }

    // Mengatur teks sisa kartu di deck
    public void UpdateMainDeckUI(int cardCount)
    {
        if (cardCount > 0)
        {
            kartuDeckText.gameObject.SetActive(true);
            // PERBAIKAN: Menggunakan format teks yang Anda inginkan
            kartuDeckText.text = " " + cardCount;
        }
        else
        {
            kartuDeckText.text = "Deck Habis!";
        }
    }
}