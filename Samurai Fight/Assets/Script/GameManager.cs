using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public UIManager uiManager;

    [Header("Referensi Objek di Scene")]
    public GameObject pionManusia;
    public GameObject pionAI;
    public Transform[] petakPapan;

    private Player manusia;
    private Player ai;
    private List<Card> deck = new List<Card>();

    private Player activePlayer;
    private enum AttackPhase { None, AwaitingTangkis, AwaitingSerangBalik, AwaitingTangkisSerangBalik }
    private AttackPhase currentAttackPhase = AttackPhase.None;

    private Player attacker;
    private Player defender;
    private int attackValue;

    private bool isPlayerMelangkah = false;
    private bool isPlayerMenyerang = false;
    private int arahLangkah = 0;

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
        uiManager.ResetScoreUI();
    }

    public void StartRound()
    {
        currentAttackPhase = AttackPhase.None;
        isPlayerMelangkah = false;
        isPlayerMenyerang = false;
        uiManager.HideAllPlayerPanels();

        manusia.position = 1;
        ai.position = 23;
        MovePawnVisual(manusia, manusia.position);
        MovePawnVisual(ai, ai.position);

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
        
        Debug.Log("--- RONDE BARU DIMULAI ---");
        activePlayer = ai;
        StartTurn();
    }

    private void StartTurn()
    {
        Debug.Log($"Sekarang giliran: {activePlayer.playerName}");
        if (activePlayer.isAI)
        {
            Invoke("ExecuteAITurn", 1.5f);
        }
        else
        {
            CheckAvailablePlayerActions();
            uiManager.ShowOpsiAwalPanel(true);
            uiManager.SetPlayerHandInteractable(false);
        }
    }
    
    private void EndTurn()
    {
        Debug.Log($"Giliran {activePlayer.playerName} berakhir.");
        RefillHand(activePlayer);
        
        // Cek apakah refill memicu tie breaker, jika ya, EndTurn berhenti di sini
        if (deck.Count == 0 && (manusia.hand.Count < 5 || ai.hand.Count < 5)) return;

        activePlayer = (activePlayer == manusia) ? ai : manusia;
        StartTurn();
    }

    private void CheckAvailablePlayerActions()
    {
        bool canMove = false;
        foreach (var card in manusia.hand)
        {
            if (manusia.position + card.value < ai.position) canMove = true;
            if (manusia.position - card.value >= 1) canMove = true;
            if (canMove) break;
        }

        int distance = ai.position - manusia.position;
        bool canAttack = manusia.hand.Any(card => card.value == distance);
        uiManager.UpdateActionButtons(canMove, canAttack);

        if (!canMove && !canAttack)
        {
            Debug.Log("Pemain terpojok! Memulai Tie Breaker...");
            ExecuteTieBreaker();
        }
    }
    
    private void ExecuteAITurn()
    {
        if (ai.hand.Count == 0) 
        {
            Debug.Log("AI tidak punya kartu, mengakhiri giliran.");
            EndTurn(); 
            return; 
        }

        var possibleActions = new List<string>();
        int distance = manusia.position - ai.position;

        if (ai.hand.Any(card => card.value == distance)) { possibleActions.Add("Serang"); }
        if (ai.hand.Any(card => ai.position - card.value > manusia.position)) { possibleActions.Add("Maju"); }
        if (ai.hand.Any(card => ai.position + card.value <= 23)) { possibleActions.Add("Mundur"); }

        if (possibleActions.Count == 0)
        {
            Debug.Log("AI terpojok! Memulai Tie Breaker...");
            ExecuteTieBreaker();
            return;
        }

        string chosenAction = possibleActions[Random.Range(0, possibleActions.Count)];
        Debug.Log($"AI memiliki opsi [{string.Join(", ", possibleActions)}] dan memilih: {chosenAction}");

        if (chosenAction == "Serang")
        {
            Card attackCard = ai.hand.First(card => card.value == distance);
            Debug.Log($"AI menyerang dengan kartu {attackCard.value}");
            ai.hand.Remove(attackCard);
            InitiateAttack(ai, manusia, attackCard.value);
        }
        else if (chosenAction == "Maju")
        {
            var validCards = ai.hand.Where(card => ai.position - card.value > manusia.position).ToList();
            Card chosenCard = validCards[Random.Range(0, validCards.Count)];
            
            ai.position -= chosenCard.value;
            MovePawnVisual(ai, ai.position);
            Debug.Log($"AI melangkah maju ke {ai.position} dengan kartu {chosenCard.value}");
            ai.hand.Remove(chosenCard);
            EndTurn();
        }
        else if (chosenAction == "Mundur")
        {
            var validCards = ai.hand.Where(card => ai.position + card.value <= 23).ToList();
            Card chosenCard = validCards[Random.Range(0, validCards.Count)];

            ai.position += chosenCard.value;
            MovePawnVisual(ai, ai.position);
            Debug.Log($"AI melangkah mundur ke {ai.position} dengan kartu {chosenCard.value}");
            ai.hand.Remove(chosenCard);
            EndTurn();
        }
    }
    
    public void OnMelangkahButtonPressed()
    {
        if (activePlayer != manusia) return;
        
        uiManager.ShowOpsiAwalPanel(false);
        uiManager.ShowAksiMelangkahPanel(true);
        CheckAvailableMoveDirections();
    }

    private void CheckAvailableMoveDirections()
    {
        bool canMoveForward = false;
        bool canMoveBackward = false;

        foreach (var card in manusia.hand)
        {
            if (manusia.position + card.value < ai.position) canMoveForward = true;
            if (manusia.position - card.value >= 1) canMoveBackward = true;
        }
        uiManager.UpdateMoveDirectionButtons(canMoveForward, canMoveBackward);
    }

    public void OnArahLangkahPressed(bool isMaju)
    {
        if (activePlayer != manusia) return;

        isPlayerMelangkah = true;
        arahLangkah = isMaju ? 1 : -1;
        
        Debug.Log("Arah dipilih. Silakan pilih kartu untuk melangkah.");
        uiManager.ShowAksiMelangkahPanel(false);
        uiManager.SetPlayerHandInteractable(true);
    }

    public void OnSerangButtonPressed()
    {
        if (activePlayer != manusia) return;
        isPlayerMenyerang = true;
        uiManager.ShowOpsiAwalPanel(false);
        Debug.Log("Anda memilih Serang...");
        uiManager.SetPlayerHandInteractable(true);
    }

    public void OnCardInHandClicked(Card clickedCard)
    {
        if (activePlayer != manusia || (!isPlayerMenyerang && !isPlayerMelangkah)) return;

        uiManager.SetPlayerHandInteractable(false);

        if (isPlayerMenyerang)
        {
            int distance = ai.position - manusia.position;
            if (clickedCard.value == distance)
            {
                isPlayerMenyerang = false;
                manusia.hand.Remove(clickedCard);
                uiManager.UpdatePlayerHandUI(manusia);
                InitiateAttack(manusia, ai, clickedCard.value);
            }
            else
            {
                isPlayerMenyerang = false;
                CheckAvailablePlayerActions();
                uiManager.ShowOpsiAwalPanel(true);
            }
        }
        else if (isPlayerMelangkah)
        {
            int newPosition = manusia.position + (clickedCard.value * arahLangkah);
            bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);

            if (isValidMove)
            {
                manusia.position = newPosition;
                MovePawnVisual(manusia, manusia.position);
                manusia.hand.Remove(clickedCard);
                isPlayerMelangkah = false;
                arahLangkah = 0;
                uiManager.UpdatePlayerHandUI(manusia);
                EndTurn();
            }
            else
            {
                Debug.Log("Kartu itu tidak bisa digunakan untuk arah yang dipilih!");
                isPlayerMelangkah = false;
                arahLangkah = 0;
                uiManager.SetPlayerHandInteractable(false);
                CheckAvailablePlayerActions();
                uiManager.ShowOpsiAwalPanel(true);
            }
        }
    }
    
    private void InitiateAttack(Player currentAttacker, Player currentDefender, int value)
    {
        attacker = currentAttacker;
        defender = currentDefender;
        attackValue = value;
        currentAttackPhase = AttackPhase.AwaitingTangkis;

        Debug.Log($"{attacker.playerName} menyerang {defender.playerName} dengan kekuatan {attackValue}.");

        if (defender.isAI)
        {
            Invoke("DecideAITangkis", 1f);
        }
        else
        {
            uiManager.ShowAksiTangkisPanel(true);
        }
    }

    private void DecideAITangkis()
    {
        Card parryCard = ai.hand.FirstOrDefault(c => c.value == attackValue);
        bool canParry = parryCard != null && ai.hand.Count > 1;

        if (canParry)
        {
            Debug.Log($"AI berhasil menangkis dengan kartu {parryCard.value}.");
            ai.hand.Remove(parryCard);
            RefillHand(ai);
            
            currentAttackPhase = AttackPhase.AwaitingSerangBalik;
            ExecuteAISerangBalik();
        }
        else
        {
            Debug.Log("AI gagal atau memilih tidak menangkis!");
            RoundOver(attacker);
        }
    }

    private void ExecuteAISerangBalik()
    {
        if (ai.hand.Count == 0) { RoundOver(ai); return; }

        Card counterCard = ai.hand[Random.Range(0, ai.hand.Count)];
        ai.hand.Remove(counterCard);

        Debug.Log($"AI melakukan Serang Balik dengan kartu {counterCard.value}!");
        InitiateAttack(ai, manusia, counterCard.value);
        currentAttackPhase = AttackPhase.AwaitingTangkisSerangBalik;
    }

    public void OnPlayerTangkisYes()
    {
        uiManager.ShowAksiTangkisPanel(false);
        Debug.Log("Pemain mencoba menangkis... (Logika pilih kartu belum ada)");
        RoundOver(attacker);
    }

    public void OnPlayerTangkisNo()
    {
        uiManager.ShowAksiTangkisPanel(false);
        Debug.Log("Pemain memilih tidak menangkis!");
        RoundOver(attacker);
    }

    private void ExecuteTieBreaker()
    {
        Debug.Log("--- TIE BREAKER DIMULAI ---");
        int totalManusia = manusia.hand.Sum(card => card.value);
        int totalAI = ai.hand.Sum(card => card.value);

        Debug.Log($"Total nilai tangan Manusia: {totalManusia}");
        Debug.Log($"Total nilai tangan AI: {totalAI}");

        if (totalManusia > totalAI)
        {
            RoundOver(manusia);
        }
        else if (totalAI > totalManusia)
        {
            RoundOver(ai);
        }
        else
        {
            Debug.Log("Hasil Tie Breaker seri! Tidak ada yang mendapat skor.");
            Invoke("StartRound", 3f);
        }
    }
    
    private void RoundOver(Player winner)
    {
        Debug.Log($"--- RONDE BERAKHIR! Pemenang: {winner.playerName} ---");
        winner.score++;
        uiManager.UpdateScoreUI(winner);

        if (winner.score >= 5)
        {
            Debug.Log($"--- GAME BERAKHIR! PEMENANG UTAMA: {winner.playerName} ---");
        }
        else
        {
            Invoke("StartRound", 3f);
        }
    }
    
    private void MovePawnVisual(Player player, int targetPosition)
    {
        GameObject pawnToMove = player.isAI ? pionAI : pionManusia;
        Transform targetPetak = petakPapan[targetPosition - 1];
        pawnToMove.transform.position = targetPetak.position;
    }

    private void RefillHand(Player player)
    {
        while (player.hand.Count < 5)
        {
            Card newCard = DrawCardFromDeck();
            if (newCard == null) 
            {
                Debug.Log("Deck habis saat refill! Memulai Tie Breaker...");
                ExecuteTieBreaker();
                return;
            }
            player.hand.Add(newCard);
        }
        
        if (!player.isAI) { uiManager.UpdatePlayerHandUI(player); }
        uiManager.UpdateMainDeckUI(deck.Count);
    }
    
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

    private void LogPlayerHand(Player player)
    {
        StringBuilder logTangan = new StringBuilder($"{player.playerName} : ");
        foreach (Card card in player.hand)
        {
            logTangan.Append(card.value + " ");
        }
        Debug.Log(logTangan.ToString());
    }
    
    private void LogDeckStock()
    {
        Dictionary<int, int> sisaStok = new Dictionary<int, int>();
        foreach (Card card in deck)
        {
            if (!sisaStok.ContainsKey(card.value))
            {
                sisaStok.Add(card.value, 0);
            }
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
}