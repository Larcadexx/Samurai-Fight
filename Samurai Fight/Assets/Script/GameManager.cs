// Mengimpor library yang dibutuhkan.
using System.Collections.Generic;
using UnityEngine;
using System.Text;

// Ini adalah kelas utama yang menjadi pusat kendali dari seluruh logika permainan.
public class GameManager : MonoBehaviour
{
    // Singleton Pattern
    public static GameManager instance;

    // Referensi ke skrip UI
    public UIManager uiManager;

    // Referensi objek di scene
    [Header("Referensi Objek di Scene")]
    public GameObject pionManusia;
    public GameObject pionAI;
    public Transform[] petakPapan;

    // Data pemain
    private Player manusia;
    private Player ai;

    // Deck kartu
    private List<Card> deck = new List<Card>();

    // Variabel Status Aksi Pemain
    private bool isPlayerMelangkah = false;
    private bool isPlayerMenyerang = false;
    private int arahLangkah = 0; // 1 untuk Maju, -1 untuk Mundur

    void Awake() { instance = this; }

    void Start()
    {
        SetupGame();
        StartRound();
    }

    // Setup awal data pemain
    void SetupGame()
    {
        manusia = new Player("Manusia", 1);
        ai = new Player("AI", 23, true);
    }

    // Memulai ronde baru
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

        uiManager.ShowOpsiAwalPanel(true);
    }

    // --- FUNGSI-FUNGSI UNTUK TOMBOL AKSI ---

    public void OnMelangkahButtonPressed()
    {
        uiManager.ShowOpsiAwalPanel(false);
        uiManager.ShowAksiMelangkahPanel(true);
    }

    public void OnSerangButtonPressed()
    {
        isPlayerMenyerang = true;
        uiManager.ShowOpsiAwalPanel(false);
        Debug.Log("Anda memilih Serang. Pilih kartu yang nilainya sama dengan jarak ke lawan.");
    }

    public void OnArahLangkahPressed(bool isMaju)
    {
        isPlayerMelangkah = true;
        arahLangkah = isMaju ? 1 : -1;
        Debug.Log("Silakan pilih kartu untuk melangkah.");
        uiManager.ShowAksiMelangkahPanel(false);
    }

    // --- FUNGSI UTAMA UNTUK PROSES AKSI KARTU ---

    public void OnCardInHandClicked(Card clickedCard)
    {
        if (isPlayerMelangkah)
        {
            int newPosition = manusia.position + (clickedCard.value * arahLangkah);
            bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);

            if (isValidMove)
            {
                // Perbarui posisi logis dan visual
                manusia.position = newPosition;
                MovePawnVisual(manusia, manusia.position);
                Debug.Log($"Pemain melangkah ke posisi: {manusia.position}");

                // Proses kartu dan reset status
                manusia.hand.Remove(clickedCard);
                isPlayerMelangkah = false;
                arahLangkah = 0;
                uiManager.UpdatePlayerHandUI(manusia);

                // LANGSUNG REFILL DAN AKHIRI GILIRAN (LOGIKA SERGAP DIHAPUS)
                Debug.Log("Langkah berhasil. Giliran berakhir, mengisi ulang kartu.");
                RefillHand(manusia);
                // TODO: Pindah ke giliran AI (End Turn)
            }
            else
            {
                Debug.Log("Langkah tidak valid! Anda tidak bisa melangkah melewati lawan atau keluar papan.");
                isPlayerMelangkah = false;
                uiManager.ShowOpsiAwalPanel(true);
            }
        }
        else if (isPlayerMenyerang)
        {
            int distance = ai.position - manusia.position;
            if (clickedCard.value == distance)
            {
                Debug.Log($"Serangan berhasil! Menggunakan kartu {clickedCard.value} untuk jarak {distance}.");
                manusia.hand.Remove(clickedCard);
                uiManager.UpdatePlayerHandUI(manusia);
                isPlayerMenyerang = false;
                
                // TODO: Logika setelah serangan berhasil (misal, AI menangkis).
                // TODO: Panggil RefillHand() dan End Turn di sini.
            }
            else
            {
                Debug.Log($"Serangan gagal! Jarak adalah {distance}, tapi kartu yang dipilih bernilai {clickedCard.value}.");
                isPlayerMenyerang = false;
                uiManager.ShowOpsiAwalPanel(true);
            }
        }
        else
        {
            Debug.Log("Pilih aksi dulu (Melangkah / Serang).");
        }
    }

    // --- FUNGSI VISUAL DAN REFILL ---

    private void MovePawnVisual(Player player, int targetPosition)
    {
        GameObject pawnToMove = player.isAI ? pionAI : pionManusia;
        Transform targetPetak = petakPapan[targetPosition - 1]; // -1 karena indeks array
        pawnToMove.transform.position = targetPetak.position;
    }

    private void RefillHand(Player player)
    {
        while (player.hand.Count < 5)
        {
            Card newCard = DrawCardFromDeck();
            if (newCard == null)
            {
                Debug.Log("Deck habis! Tidak bisa refill kartu.");
                // TODO: Logika tie-breaker jika dek habis
                break;
            }
            player.hand.Add(newCard);
        }
        
        uiManager.UpdatePlayerHandUI(player);
        uiManager.UpdateMainDeckUI(deck.Count);
        LogPlayerHand(player);
    }
    
    // --- FUNGSI-FUNGSI MANAJEMEN DEK & LOGGING ---

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