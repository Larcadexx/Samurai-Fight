using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    private bool isGameOver = false;

    private bool isPlayerMelangkah, isPlayerMenyerang, isPlayerInSergapMode, isPlayerStrengtheningAttack, isPlayerInSerangBalikMode;
    private int arahLangkah = 0;

    private enum AttackPhase { None, AwaitingTangkis, AwaitingSerangBalik, AwaitingTangkisPlayer, PlayerSelectingParryCards }
    private AttackPhase currentAttackPhase = AttackPhase.None;
    private bool isCurrentAttackACounter = false; 

    private Player attacker;
    private Player defender;
    private int attackValue;
    private Card initialAttackCard;
    private List<KeyValuePair<Card, SistemKartu>> selectedParryCards = new List<KeyValuePair<Card, SistemKartu>>();

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        SetupGame();
        StartRound();
    }
    
    // --- FUNGSI UTAMA UNTUK INTERAKSI KARTU ---
    public void OnCardSlotClicked(Card clickedCard, SistemKartu slotController)
    {
        // Izinkan klik jika ini giliran manusia, ATAU saat memilih kartu tangkis, ATAU saat memilih kartu serang balik.
        if (activePlayer != manusia && currentAttackPhase != AttackPhase.PlayerSelectingParryCards && !isPlayerInSerangBalikMode)
        {
            return;
        }

        // Logika untuk memilih kartu tangkisan
        if (currentAttackPhase == AttackPhase.PlayerSelectingParryCards)
        {
            var cardEntry = new KeyValuePair<Card, SistemKartu>(clickedCard, slotController);
            if (selectedParryCards.Any(p => p.Value == slotController))
            {
                selectedParryCards.RemoveAll(p => p.Value == slotController);
                slotController.ToggleSelection(false);
            }
            else
            {
                selectedParryCards.Add(cardEntry);
                slotController.ToggleSelection(true);
            }
            int currentParryTotal = selectedParryCards.Sum(entry => entry.Key.value);
            uiManager.UpdateSelectedParryTotal(currentParryTotal); // Menampilkan "TOTAL TANGKISAN: Y"
            return;
        }

        // Cek jika ada aksi aktif yang membutuhkan kartu
        if (!isPlayerMenyerang && !isPlayerMelangkah && !isPlayerInSergapMode && !isPlayerStrengtheningAttack && !isPlayerInSerangBalikMode)
            return;

        uiManager.SetPlayerHandInteractable(false);
        HandleCardAction(clickedCard);
    }

    // --- MANAJEMEN ALUR PERMAINAN ---

    private void StartTurn()
    {
        if (isGameOver) return;
        Debug.Log($"Sekarang giliran: {activePlayer.playerName}");

        // Selalu sembunyikan nilai serangan di awal giliran.
        uiManager.HideAttackStrength();

        if (activePlayer.isAI)
        {
            uiManager.RestoreDefaultLayout();
            StartCoroutine(ExecuteAITurnCoroutine());
        }
        else
        {
            uiManager.ShowFullPlayerUI();
            CheckAvailablePlayerActions();
            uiManager.SetPlayerHandInteractable(true);
        }
    }
    
    private void EndTurn()
    {
        Debug.Log($"Giliran {activePlayer.playerName} berakhir.");
        RefillHand(manusia); 
        RefillHand(ai);      
        
        if (deck.Count == 0 && (manusia.hand.Count < 5 || ai.hand.Count < 5)) 
        {
            StartCoroutine(ExecuteTieBreakerCoroutine());
            return;
        }
        
        activePlayer = (activePlayer == manusia) ? ai : manusia;
        StartTurn();
    }

    private IEnumerator RoundOverCoroutine(Player winner)
    {
        isGameOver = true; // Mencegah aksi lain saat coroutine berjalan
        uiManager.EnterFokusMode("");
        
        // Selalu sembunyikan nilai serangan di akhir ronde.
        uiManager.HideAttackStrength();
        
        uiManager.ShowMessage($"{winner.playerName} memenangkan ronde!", 2.5f);
        winner.score++;
        uiManager.UpdateScoreUI(winner);
        yield return new WaitForSeconds(2.5f);

        if (winner.score >= 5)
        {
            uiManager.ShowKemenanganPanel(winner == manusia);
        }
        else
        {
            isGameOver = false; // Izinkan game berlanjut
            StartRound();
        }
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
        isPlayerMelangkah = isPlayerMenyerang = isPlayerInSergapMode = isPlayerStrengtheningAttack = isPlayerInSerangBalikMode = false;
        isCurrentAttackACounter = isGameOver = false;
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
        activePlayer = ai;
        StartTurn();
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // --- LOGIKA SERANGAN & PERTAHANAN ---

    private void InitiateAttack(Player currentAttacker, Player currentDefender, int value, bool isCounter)
    {
        attacker = currentAttacker;
        defender = currentDefender;
        attackValue = value;
        isCurrentAttackACounter = isCounter;
        
        // Tampilkan nilai serangan HANYA saat serangan dimulai.
        uiManager.ShowAttackStrength(attackValue);

        if (defender.isAI)
        {
            uiManager.ShowMessage($"{attacker.playerName} menyerang dengan kekuatan {value}!", 0);
            currentAttackPhase = AttackPhase.AwaitingTangkis;
            StartCoroutine(DecideAITangkisCoroutine());
        }
        else
        {
            currentAttackPhase = AttackPhase.AwaitingTangkisPlayer;
            string message = isCounter ? $"Anda diserang balik!" : $"Anda diserang!";
            uiManager.EnterFokusMode(message + " Tangkis?");
            uiManager.AksiTangkisPanel.SetActive(true);
        }
    }
    
    private void HandleCardAction(Card clickedCard)
    {
        if (isPlayerMenyerang)
        {
            int distance = ai.position - manusia.position;
            if (clickedCard.value == distance)
            {
                isPlayerMenyerang = false;
                initialAttackCard = clickedCard;
                manusia.hand.Remove(clickedCard);
                uiManager.UpdatePlayerHandUI(manusia);
                uiManager.KartuManusiaPanel.SetActive(false);
                if (manusia.hand.Count > 0)
                {
                    uiManager.EnterFokusMode("Perkuat serangan?");
                    uiManager.PerkuatSeranganPanel.SetActive(true);
                }
                else
                {
                    InitiateAttack(manusia, ai, initialAttackCard.value, false);
                }
            } else {
                StartTurn();
            }
        }
        else if (isPlayerStrengtheningAttack)
        {
            isPlayerStrengtheningAttack = false;
            int totalAttackValue = initialAttackCard.value + clickedCard.value;
            manusia.hand.Remove(clickedCard);
            uiManager.UpdatePlayerHandUI(manusia);
            InitiateAttack(manusia, ai, totalAttackValue, false);
        }
        else if (isPlayerMelangkah)
        {
            HandleMove(clickedCard);
        }
        else if (isPlayerInSergapMode)
        {
            HandleSergap(clickedCard);
        }
        else if (isPlayerInSerangBalikMode)
        {
            isPlayerInSerangBalikMode = false;
            manusia.hand.Remove(clickedCard);
            uiManager.UpdatePlayerHandUI(manusia);
            InitiateAttack(manusia, ai, clickedCard.value, true);
        }
    }

    public void OnPlayerConfirmTangkis()
    {
        uiManager.KonfirmasiTangkisButton.gameObject.SetActive(false);
        uiManager.SetPlayerHandInteractable(false);
        uiManager.HideMessage();
        
        int totalParryValue = selectedParryCards.Sum(entry => entry.Key.value);
        bool hasEnoughValue = totalParryValue == attackValue;
        bool hasRemainingCardForCounter = manusia.hand.Count - selectedParryCards.Count >= 1;

        if (hasEnoughValue)
        {
            if (isCurrentAttackACounter)
            {
                uiManager.ShowMessage("Serang balik berhasil ditangkis!", 2.5f);
                foreach (var entry in selectedParryCards) manusia.hand.Remove(entry.Key);
                uiManager.UpdatePlayerHandUI(manusia);
                EndTurn();
            }
            else if (hasRemainingCardForCounter)
            {
                uiManager.ShowMessage("Serangan berhasil ditangkis!", 1.5f);
                foreach (var entry in selectedParryCards) manusia.hand.Remove(entry.Key);
                uiManager.UpdatePlayerHandUI(manusia);
                StartCoroutine(ExecutePlayerSerangBalikCoroutine());
            }
            else
            {
                uiManager.ShowMessage("Tangkisan Gagal! Harus sisa 1 kartu.", 3f);
                StartCoroutine(RoundOverCoroutine(attacker));
            }
        }
        else
        {
            uiManager.ShowMessage("Tangkisan Gagal! Nilai tidak sesuai.", 3f);
            StartCoroutine(RoundOverCoroutine(attacker));
        }

        foreach (var entry in selectedParryCards) entry.Value.ToggleSelection(false);
        selectedParryCards.Clear();
        currentAttackPhase = AttackPhase.None;
    }
    
    private IEnumerator ExecutePlayerSerangBalikCoroutine()
    {
        yield return new WaitForSeconds(1.5f);
        uiManager.HideAttackStrength();
        isPlayerInSerangBalikMode = true;
        uiManager.EnterFokusMode("Pilih 1 kartu untuk SERANG BALIK.");
        uiManager.KartuManusiaPanel.SetActive(true);
        uiManager.SetPlayerHandInteractable(true);
    }
    
    // --- AKSI TOMBOL PEMAIN (UI BUTTONS) ---

    public void OnMelangkahButtonPressed()
    {
        if (activePlayer != manusia) return;
        isPlayerMelangkah = true;
        uiManager.EnterFokusMode("Pilih arah untuk melangkah.");
        uiManager.AksiMelangkahPanel.SetActive(true);
        CheckAvailableMoveDirections();
    }
    
    public void OnArahLangkahPressed(bool isMaju)
    {
        if (activePlayer != manusia || !isPlayerMelangkah) return;
        uiManager.AksiMelangkahPanel.SetActive(false);
        arahLangkah = isMaju ? 1 : -1;
        string arah = isMaju ? "maju" : "mundur";
        uiManager.ShowMessage($"Pilih kartu untuk melangkah {arah}.", 0f);
        uiManager.KartuManusiaPanel.SetActive(true);
        uiManager.SetPlayerHandInteractable(true);
    }

    public void OnSerangButtonPressed()
    {
        if (activePlayer != manusia) return;
        isPlayerMenyerang = true;
        uiManager.EnterFokusMode("Pilih kartu dengan nilai sama dengan jarak.");
        uiManager.KartuManusiaPanel.SetActive(true);
        uiManager.SetPlayerHandInteractable(true);
    }
    
    public void OnPerkuatYesButtonPressed()
    {
        isPlayerStrengtheningAttack = true;
        uiManager.PerkuatSeranganPanel.SetActive(false);
        uiManager.ShowMessage("Pilih satu kartu untuk memperkuat.", 0f);
        uiManager.KartuManusiaPanel.SetActive(true);
        uiManager.SetPlayerHandInteractable(true);
    }

    public void OnPerkuatNoButtonPressed()
    {
        uiManager.HideAllPlayerPanels();
        InitiateAttack(manusia, ai, initialAttackCard.value, false);
    }
    
    public void OnSergapYesButtonPressed()
    {
        isPlayerInSergapMode = true;
        uiManager.AksiSergapPanel.SetActive(false);
        uiManager.ShowMessage("Pilih kartu untuk melakukan Sergap.", 0f);
        uiManager.KartuManusiaPanel.SetActive(true);
        uiManager.SetPlayerHandInteractable(true);
    }

    public void OnSergapNoButtonPressed()
    {
        EndTurn();
    }

    public void OnPlayerTangkisYes()
    {
        if (currentAttackPhase != AttackPhase.AwaitingTangkisPlayer) return;
        currentAttackPhase = AttackPhase.PlayerSelectingParryCards;
        uiManager.AksiTangkisPanel.SetActive(false);
        uiManager.KonfirmasiTangkisButton.gameObject.SetActive(true);
        uiManager.KonfirmasiTangkisButton.onClick.RemoveAllListeners();
        uiManager.KonfirmasiTangkisButton.onClick.AddListener(OnPlayerConfirmTangkis);
        uiManager.ShowMessage("Pilih kartu, lalu tekan Konfirmasi.", 0f);
        uiManager.KartuManusiaPanel.SetActive(true);
        uiManager.SetPlayerHandInteractable(true);
        uiManager.UpdateSelectedParryTotal(0);
    }

    public void OnPlayerTangkisNo()
    {
        StartCoroutine(RoundOverCoroutine(attacker));
    }
    
    // --- LOGIKA AI (ARTIFICIAL INTELLIGENCE) ---

    private IEnumerator ExecuteAITurnCoroutine()
    {
        uiManager.ShowMessage("Giliran AI...", 0f);
        yield return new WaitForSeconds(1.5f);
        if (ai.hand.Count == 0) { EndTurn(); yield break; }

        int distance = ai.position - manusia.position;
        bool canAttack = ai.hand.Any(card => card.value == distance);
        bool canMoveForward = ai.hand.Any(card => ai.position - card.value > manusia.position);
        bool canMoveBackward = ai.hand.Any(card => ai.position + card.value <= 23);

        if (canAttack)
        {
            Card attackCard = ai.hand.First(card => card.value == distance);
            ai.hand.Remove(attackCard);
            InitiateAttack(ai, manusia, attackCard.value, false);
        }
        else if (canMoveForward)
        {
            var validCards = ai.hand.Where(card => ai.position - card.value > manusia.position).ToList();
            Card chosenCard = validCards[Random.Range(0, validCards.Count)];
            ai.position -= chosenCard.value;
            uiManager.ShowMessage("AI melangkah maju", 2f);
            MovePawnVisual(ai, ai.position);
            ai.hand.Remove(chosenCard);
            yield return new WaitForSeconds(1.5f);
            EndTurn();
        }
        else if (canMoveBackward)
        {
            var validCards = ai.hand.Where(card => ai.position + card.value <= 23).ToList();
            Card chosenCard = validCards[Random.Range(0, validCards.Count)];
            ai.position += chosenCard.value;
            uiManager.ShowMessage("AI melangkah mundur", 2f);
            MovePawnVisual(ai, ai.position);
            ai.hand.Remove(chosenCard);
            yield return new WaitForSeconds(1.5f);
            EndTurn();
        }
        else
        {
            StartCoroutine(ExecuteTieBreakerCoroutine());
        }
    }

    private IEnumerator DecideAITangkisCoroutine()
    {
        yield return new WaitForSeconds(1.5f);
        List<Card> parryCombination = FindParryCombination(attackValue, ai.hand, !isCurrentAttackACounter);
        if (parryCombination != null)
        {
            var cardValues = parryCombination.Select(c => c.value.ToString());
            uiManager.ShowMessage($"AI menangkis dengan [{string.Join(" + ", cardValues)}]!", 2.5f);
            yield return new WaitForSeconds(2f);
            foreach (var card in parryCombination) ai.hand.Remove(card);
            if (isCurrentAttackACounter) 
            {
                EndTurn();
            }
            else
            {
                currentAttackPhase = AttackPhase.AwaitingSerangBalik;
                StartCoroutine(ExecuteAISerangBalikCoroutine());
            }
        }
        else
        {
            uiManager.ShowMessage("AI gagal menangkis!", 2.0f);
            yield return new WaitForSeconds(2.0f);
            StartCoroutine(RoundOverCoroutine(attacker));
        }
    }

    private IEnumerator ExecuteAISerangBalikCoroutine()
    {
        if (ai.hand.Count == 0)
        {
            EndTurn();
            yield break;
        }
        Card counterCard = ai.hand[Random.Range(0, ai.hand.Count)];
        ai.hand.Remove(counterCard);
        InitiateAttack(ai, manusia, counterCard.value, true);
    }

    // --- FUNGSI BANTU (HELPER FUNCTIONS) ---

    private void CheckAvailablePlayerActions()
    {
        bool canMove = manusia.hand.Any(card => (manusia.position + card.value < ai.position) || (manusia.position - card.value >= 1));
        int distance = ai.position - manusia.position;
        bool canAttack = manusia.hand.Any(card => card.value == distance);
        uiManager.UpdateActionButtons(canMove, canAttack);
        if (!canMove && !canAttack)
        {
            StartCoroutine(ExecuteTieBreakerCoroutine());
        }
    }
    
    private void CheckAvailableMoveDirections()
    {
        bool canMoveForward = manusia.hand.Any(card => manusia.position + card.value < ai.position);
        bool canMoveBackward = manusia.hand.Any(card => manusia.position - card.value >= 1);
        uiManager.UpdateMoveDirectionButtons(canMoveForward, canMoveBackward);
    }
    
    private void HandleMove(Card clickedCard)
    {
        int newPosition = manusia.position + (clickedCard.value * arahLangkah);
        bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);
        if (isValidMove)
        {
            manusia.position = newPosition;
            MovePawnVisual(manusia, manusia.position);
            manusia.hand.Remove(clickedCard);
            uiManager.UpdatePlayerHandUI(manusia);
            isPlayerMelangkah = false;
            int newDistance = ai.position - newPosition;
            bool canSergap = manusia.hand.Any(card => card.value == newDistance);
            if (canSergap)
            {
                uiManager.EnterFokusMode("Anda bisa melakukan Sergap!");
                uiManager.AksiSergapPanel.SetActive(true);
            }
            else
            {
                EndTurn();
            }
        }
        else
        {
            isPlayerMelangkah = false;
            StartTurn();
        }
    }

    private void HandleSergap(Card clickedCard)
    {
        isPlayerInSergapMode = false;
        int distance = ai.position - manusia.position;
        if (clickedCard.value == distance)
        {
            manusia.hand.Remove(clickedCard);
            uiManager.UpdatePlayerHandUI(manusia);
            InitiateAttack(manusia, ai, clickedCard.value, false);
        }
        else
        {
            EndTurn();
        }
    }

    private List<Card> FindParryCombination(int target, List<Card> hand, bool mustHaveCardLeft)
    {
        List<Card> FindSubsetSum(int currentTarget, List<Card> currentHand, List<Card> currentCombination)
        {
            if (currentTarget == 0)
            {
                if (mustHaveCardLeft && hand.Count - currentCombination.Count < 1) return null;
                return currentCombination;
            }
            if (currentTarget < 0 || currentHand.Count == 0) return null;
            Card head = currentHand[0];
            List<Card> tail = currentHand.GetRange(1, currentHand.Count - 1);
            var withHead = new List<Card>(currentCombination) { head };
            var resultWith = FindSubsetSum(currentTarget - head.value, tail, withHead);
            if (resultWith != null) return resultWith;
            var resultWithout = FindSubsetSum(currentTarget, tail, currentCombination);
            return resultWithout;
        }
        return FindSubsetSum(target, new List<Card>(hand), new List<Card>());
    }

    private IEnumerator ExecuteTieBreakerCoroutine()
    {
        uiManager.EnterFokusMode("Pemain terpojok atau dek habis! TIE BREAKER!");
        yield return new WaitForSeconds(2.5f);
        int totalManusia = manusia.hand.Sum(card => card.value);
        int totalAI = ai.hand.Sum(card => card.value);
        Player winner = null;
        if (totalManusia > totalAI) winner = manusia;
        else if (totalAI > totalManusia) winner = ai;
        if (winner != null)
        {
            StartCoroutine(RoundOverCoroutine(winner));
        }
        else
        {
            uiManager.ShowMessage("Hasil Tie Breaker seri!", 2.5f);
            yield return new WaitForSeconds(2.5f);
            StartRound();
        }
    }

    private void MovePawnVisual(Player player, int targetPosition)
    {
        GameObject pawn = player.isAI ? pionAI : pionManusia;
        Transform targetPetak = petakPapan[targetPosition - 1];
        pawn.transform.position = targetPetak.position + new Vector3(0, 75f, 0);
    }

    private void RefillHand(Player player)
    {
        while (player.hand.Count < 5)
        {
            Card newCard = DrawCardFromDeck();
            if (newCard == null)
            {
                if(!isGameOver) StartCoroutine(ExecuteTieBreakerCoroutine());
                return;
            }
            player.hand.Add(newCard);
        }
        if (!player.isAI) uiManager.UpdatePlayerHandUI(player);
        uiManager.UpdateMainDeckUI(deck.Count);
    }
    
    private void CreateDeck()
    {
        deck.Clear();
        for (int value = 1; value <= 5; value++)
        {
            for (int i = 0; i < 5; i++) deck.Add(new Card(value));
        }
    }

    private void ShuffleDeck()
    {
        System.Random rng = new System.Random();
        deck = deck.OrderBy(a => rng.Next()).ToList();
    }

    private Card DrawCardFromDeck()
    {
        if (deck.Count == 0) return null;
        Card drawn = deck[0];
        deck.RemoveAt(0);
        return drawn;
    }
}