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
        // 1. Buat deck berisi 25 kartu
        CreateDeck();
        // 2. Kocok deck
        ShuffleDeck();

        // 3. Bagikan 10 kartu
        manusia.hand.Clear();
        ai.hand.Clear();
        for (int i = 0; i < 5; i++)
        {
            manusia.hand.Add(DrawCardFromDeck());
            ai.hand.Add(DrawCardFromDeck());
        }

        // 4. Update UI
        uiManager.UpdatePlayerHandUI(manusia);
        uiManager.UpdateMainDeckUI(deck.Count);

        // 5. BARU: Catat sisa stok kartu di deck SETELAH dibagikan
        LogDeckStock();

        // 6. Catat kartu yang ada di tangan pemain
        LogPlayerHand(manusia);
        LogPlayerHand(ai);
    }

    // Fungsi ini sekarang hanya membuat deck, tanpa log
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

    // Fungsi baru untuk mencatat sisa stok kartu di deck
    private void LogDeckStock()
    {
        Dictionary<int, int> sisaStok = new Dictionary<int, int>();
        // Hitung sisa kartu di dalam deck
        foreach (Card card in deck)
        {
            if (!sisaStok.ContainsKey(card.value))
            {
                sisaStok[card.value] = 0;
            }
            sisaStok[card.value]++;
        }

        // Buat log dengan format yang Anda inginkan
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