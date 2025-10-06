// GameManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public UIManager uiManager;

    private Player manusia;
    private Player ai;

    private List<Card> deck = new List<Card>();

    // Variabel baru untuk aksi melangkah
    private bool isPlayerMelangkah = false;
    private int arahLangkah = 0; // 1 untuk Maju, -1 untuk Mundur

    void Awake() { instance = this; }

    void Start()
    {
        SetupGame();
        StartRound();
    }

    void SetupGame()
    {
        manusia = new Player("Manusia", 1);
        ai = new Player("AI", 23, true);
    }

    public void StartRound()
    {
        CreateDeck();
        ShuffleDeck();

        manusia.hand.Clear();
        ai.hand.Clear();
        for (int i = 0; i < 5; i++)
        {
            manusia.hand.Add(DrawCardFromDeck());
            ai.hand.Add(DrawCardFromDeck());
        }

        uiManager.UpdatePlayerHandUI(manusia);
        uiManager.UpdateMainDeckUI(deck.Count);
        LogDeckStock();
        LogPlayerHand(manusia);
        LogPlayerHand(ai);
        
        // TODO: Ganti ke giliran AI sesuai aturan
        // Untuk sekarang, kita langsung ke giliran pemain untuk testing
        uiManager.ShowOpsiAwalPanel(true);
    }

    // --- FUNGSI BARU UNTUK AKSI PEMAIN ---

    // 1. Dipanggil saat tombol "Melangkah" di OpsiAwalPanel ditekan.
    public void OnMelangkahButtonPressed()
    {
        uiManager.ShowOpsiAwalPanel(false);
        uiManager.ShowAksiMelangkahPanel(true);
    }

    // 2. Dipanggil saat tombol "Maju" atau "Mundur" ditekan.
    public void OnArahLangkahPressed(bool isMaju)
    {
        isPlayerMelangkah = true;
        arahLangkah = isMaju ? 1 : -1;

        Debug.Log("Silakan pilih kartu untuk melangkah.");
        uiManager.ShowAksiMelangkahPanel(false);
    }

    // 3. Dipanggil dari CardController saat sebuah kartu di tangan diklik.
    public void OnCardInHandClicked(Card clickedCard)
    {
        if (isPlayerMelangkah)
        {
            int newPosition = manusia.position + (clickedCard.value * arahLangkah);
            
            bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);

            if (isValidMove)
            {
                manusia.position = newPosition;
                Debug.Log($"Pemain melangkah ke posisi: {manusia.position}");

                manusia.hand.Remove(clickedCard);

                isPlayerMelangkah = false;
                arahLangkah = 0;
                
                uiManager.UpdatePlayerHandUI(manusia);

                // TODO: Pindah ke fase "Sergap" (menampilkan AksiSergapPanel)
            }
            else
            {
                Debug.Log("Langkah tidak valid! Anda tidak bisa melangkah melewati lawan atau keluar papan.");
                isPlayerMelangkah = false;
                uiManager.ShowOpsiAwalPanel(true);
            }
        }
        else
        {
            Debug.Log("Pilih aksi dulu (Melangkah / Serang).");
        }
    }

    // --- FUNGSI MANAJEMEN DEK & LOGGING (Tidak Berubah) ---

    private void CreateDeck()
    {
        deck.Clear();
        for (int value = 1; value <= 5; value++)
        {
            for (int count = 0; count < 5; count++)
            {
                deck.Add(new Card(value));
            }
        }
    }

    private void LogDeckStock()
    {
        Dictionary<int, int> sisaStok = new Dictionary<int, int>();
        foreach (Card card in deck)
        {
            if (!sisaStok.ContainsKey(card.value)) { sisaStok[card.value] = 0; }
            sisaStok[card.value]++;
        }

        StringBuilder logStok = new StringBuilder("Sisa Stok Kartu di deck : ");
        for (int i = 1; i <= 5; i++)
        {
            int jumlah = sisaStok.ContainsKey(i) ? sisaStok[i] : 0;
            logStok.Append($"{i}={jumlah} ");
        }
        Debug.Log(logStok.ToString());
    }

    private void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Card temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    private Card DrawCardFromDeck()
    {
        if (deck.Count == 0) return null;
        Card drawnCard = deck[0];
        deck.RemoveAt(0);
        return drawnCard;
    }

    private void LogPlayerHand(Player player)
    {
        StringBuilder logTangan = new StringBuilder($"{player.playerName} : ");
        foreach (Card card in player.hand)
        {
            logTangan.Append(card.value + " ");
        }
        Debug.Log(logTangan.ToString());
    }
}