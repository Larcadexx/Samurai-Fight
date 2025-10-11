using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    private enum AttackPhase { None, AwaitingTangkis, AwaitingSerangBalik, AwaitingTangkisPlayer, PlayerSelectingParryCards }
    private AttackPhase currentAttackPhase = AttackPhase.None;

    private Player attacker;
    private Player defender;
    private int attackValue;

    private List<KeyValuePair<Card, CardController>> selectedParryCards = new List<KeyValuePair<Card, CardController>>();

    private bool isPlayerMelangkah = false;
    private bool isPlayerMenyerang = false;
    private bool isPlayerInSergapMode = false;
    private bool isPlayerStrengtheningAttack = false;
    private int arahLangkah = 0;
    private bool isGameOver = false;

    private Card initialAttackCard;

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
        ResetTangkisButton();
        currentAttackPhase = AttackPhase.None;
        isPlayerMelangkah = false;
        isPlayerMenyerang = false;
        isPlayerInSergapMode = false;
        isPlayerStrengtheningAttack = false;
        isGameOver = false;

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
        if (isGameOver) return;
        
        uiManager.RestoreDefaultLayout();
        
        Debug.Log($"Sekarang giliran: {activePlayer.playerName}");
        if (activePlayer.isAI)
        {
            StartCoroutine(ExecuteAITurnCoroutine());
        }
        else
        {
            uiManager.ShowMessage("Giliran Anda!", 2f);
            CheckAvailablePlayerActions();
            uiManager.ShowOpsiAwalPanel(true);
            uiManager.SetPlayerHandInteractable(false);
        }
    }
    
    private void EndTurn()
    {
        Debug.Log($"Giliran {activePlayer.playerName} berakhir.");
        RefillHand(activePlayer);
        
        if (deck.Count == 0 && (manusia.hand.Count < 5 || ai.hand.Count < 5)) return;

        activePlayer = (activePlayer == manusia) ? ai : manusia;
        StartTurn();
    }
    
    public void PlayAgain()
    {
        Debug.Log("Memulai permainan baru...");
        uiManager.KemenanganPanel.SetActive(false);
        manusia.score = 0;
        ai.score = 0;
        Start();
    }

    public void GoToMainMenu()
    {
        Debug.Log("Kembali ke Main Menu...");
        SceneManager.LoadScene("MainMenu");
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
            StartCoroutine(ExecuteTieBreakerCoroutine());
        }
    }
    
    private IEnumerator ExecuteAITurnCoroutine()
    {
        uiManager.ShowMessage("Giliran AI...", 0f);
        yield return new WaitForSeconds(1.5f);

        if (ai.hand.Count == 0) { EndTurn(); yield break; }

        var possibleActions = new List<string>();
        int distance = manusia.position - ai.position;

        if (ai.hand.Any(card => card.value == distance)) { possibleActions.Add("Serang"); }
        if (ai.hand.Any(card => ai.position - card.value > manusia.position)) { possibleActions.Add("Maju"); }
        if (ai.hand.Any(card => ai.position + card.value <= 23)) { possibleActions.Add("Mundur"); }

        if (possibleActions.Count == 0) { StartCoroutine(ExecuteTieBreakerCoroutine()); yield break; }

        string chosenAction = possibleActions[Random.Range(0, possibleActions.Count)];
        
        if (chosenAction == "Serang")
        {
            Card attackCard = ai.hand.First(card => card.value == distance);
            uiManager.ShowMessage($"AI menyerang dengan kartu {attackCard.value}", 2f);
            yield return new WaitForSeconds(1f);
            ai.hand.Remove(attackCard);
            InitiateAttack(ai, manusia, attackCard.value);
        }
        else 
        {
            if (chosenAction == "Maju")
            {
                var validCards = ai.hand.Where(card => ai.position - card.value > manusia.position).ToList();
                Card chosenCard = validCards[Random.Range(0, validCards.Count)];
                ai.position -= chosenCard.value;
                uiManager.ShowMessage($"AI melangkah maju", 2f);
                MovePawnVisual(ai, ai.position);
                ai.hand.Remove(chosenCard);
            }
            else if (chosenAction == "Mundur")
            {
                var validCards = ai.hand.Where(card => ai.position + card.value <= 23).ToList();
                Card chosenCard = validCards[Random.Range(0, validCards.Count)];
                ai.position += chosenCard.value;
                uiManager.ShowMessage($"AI melangkah mundur", 2f);
                MovePawnVisual(ai, ai.position);
                ai.hand.Remove(chosenCard);
            }
            yield return new WaitForSeconds(1.5f);
            EndTurn();
        }
    }
    
    public void OnMelangkahButtonPressed()
    {
        if (activePlayer != manusia) return;
        uiManager.ShowMessage("Pilih arah untuk melangkah.", 0f);
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
        string arah = isMaju ? "maju" : "mundur";
        uiManager.ShowMessage($"Pilih kartu untuk melangkah {arah}.", 0f);
        
        uiManager.ShowHandPanelOnly("move");
        uiManager.SetPlayerHandInteractable(true);
    }

    public void OnSerangButtonPressed()
    {
        if (activePlayer != manusia) return;
        isPlayerMenyerang = true;
        uiManager.ShowMessage("Pilih kartu yang nilainya sama dengan jarak.", 0f);
        
        uiManager.ShowHandPanelOnly("attack");
        uiManager.SetPlayerHandInteractable(true);
    }

    public void OnCardInHandClicked(Card clickedCard, CardController cardController)
    {
        if (activePlayer != manusia) return;

        if (currentAttackPhase == AttackPhase.PlayerSelectingParryCards)
        {
            var cardEntry = new KeyValuePair<Card, CardController>(clickedCard, cardController);
            if (selectedParryCards.Contains(cardEntry))
            {
                selectedParryCards.Remove(cardEntry);
                cardController.ToggleSelection(false);
            }
            else
            {
                selectedParryCards.Add(cardEntry);
                cardController.ToggleSelection(true);
            }
            int currentParryTotal = selectedParryCards.Sum(entry => entry.Key.value);
            uiManager.UpdateSelectedParryTotal(currentParryTotal);
            return;
        }

        if (!isPlayerMenyerang && !isPlayerMelangkah && !isPlayerInSergapMode && !isPlayerStrengtheningAttack) return;
        uiManager.SetPlayerHandInteractable(false);
        uiManager.HideMessage();

        if (isPlayerMenyerang)
        {
            int distance = ai.position - manusia.position;
            if (clickedCard.value == distance)
            {
                isPlayerMenyerang = false;
                initialAttackCard = clickedCard;
                manusia.hand.Remove(clickedCard);
                uiManager.UpdatePlayerHandUI(manusia);

                if (manusia.hand.Count > 0)
                {
                    uiManager.ShowPerkuatSeranganPanel(true);
                    uiManager.ShowMessage("Perkuat serangan?", 0f);
                }
                else
                {
                    InitiateAttack(manusia, ai, initialAttackCard.value);
                }
            }
            else
            {
                isPlayerMenyerang = false;
                CheckAvailablePlayerActions();
                uiManager.ShowOpsiAwalPanel(true);
            }
        }
        else if (isPlayerStrengtheningAttack)
        {
            isPlayerStrengtheningAttack = false;
            int totalAttackValue = initialAttackCard.value + clickedCard.value;
            Debug.Log($"Serangan diperkuat! Total: {initialAttackCard.value} + {clickedCard.value} = {totalAttackValue}");
            manusia.hand.Remove(clickedCard);
            uiManager.UpdatePlayerHandUI(manusia);
            InitiateAttack(manusia, ai, totalAttackValue);
        }
        else if (isPlayerMelangkah)
        {
            int newPosition = manusia.position + (clickedCard.value * arahLangkah);
            bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);

            if (isValidMove)
            {
                int newDistance = ai.position - newPosition;
                bool canSergap = manusia.hand.Any(card => card != clickedCard && card.value == newDistance);

                manusia.position = newPosition;
                MovePawnVisual(manusia, manusia.position);
                manusia.hand.Remove(clickedCard);
                isPlayerMelangkah = false;
                arahLangkah = 0;
                uiManager.UpdatePlayerHandUI(manusia);

                if (canSergap)
                {
                    uiManager.ShowMessage("Anda bisa melakukan Sergap!", 0f);
                    uiManager.ShowAksiSergapPanel(true);
                }
                else
                {
                    EndTurn();
                }
            }
            else
            {
                isPlayerMelangkah = false;
                arahLangkah = 0;
                uiManager.SetPlayerHandInteractable(false);
                CheckAvailablePlayerActions();
                uiManager.ShowOpsiAwalPanel(true);
            }
        }
        else if (isPlayerInSergapMode)
        {
            isPlayerInSergapMode = false;
            int distance = ai.position - manusia.position;

            if (clickedCard.value == distance)
            {
                Debug.Log($"Sergap berhasil dengan kartu {clickedCard.value}!");
                manusia.hand.Remove(clickedCard);
                uiManager.UpdatePlayerHandUI(manusia);
                InitiateAttack(manusia, ai, clickedCard.value);
            }
            else
            {
                Debug.Log($"Sergap gagal! Kartu {clickedCard.value} tidak sesuai dengan jarak {distance}.");
                uiManager.ShowMessage("Sergap Gagal!", 2f);
                EndTurn();
            }
        }
    }

    public void OnPerkuatYesButtonPressed()
    {
        isPlayerStrengtheningAttack = true;
        uiManager.ShowMessage("Pilih satu kartu untuk memperkuat.", 0f);
        uiManager.ShowHandPanelOnly("attack");
        uiManager.SetPlayerHandInteractable(true);
    }

    public void OnPerkuatNoButtonPressed()
    {
        uiManager.HideAllPlayerPanels();
        InitiateAttack(manusia, ai, initialAttackCard.value);
    }

    public void OnSergapYesButtonPressed()
    {
        isPlayerInSergapMode = true;
        uiManager.ShowMessage("Pilih kartu untuk melakukan Sergap.", 0f);
        uiManager.ShowHandPanelOnly("attack");
        uiManager.SetPlayerHandInteractable(true);
    }

    public void OnSergapNoButtonPressed()
    {
        uiManager.HideAllPlayerPanels();
        EndTurn();
    }
    
    private void InitiateAttack(Player currentAttacker, Player currentDefender, int value)
    {
        attacker = currentAttacker;
        defender = currentDefender;
        attackValue = value;
        uiManager.ShowAttackStrength(attackValue);

        if (defender.isAI)
        {
            currentAttackPhase = AttackPhase.AwaitingTangkis;
            StartCoroutine(DecideAITangkisCoroutine());
        }
        else
        {
            currentAttackPhase = AttackPhase.AwaitingTangkisPlayer;
            uiManager.ShowMessage($"Anda diserang! Kekuatan serangan: {value}. Pilih 'Tangkis' untuk melawan.", 0f);
            uiManager.ShowAksiTangkisPanel(true);
        }
    }

    private IEnumerator DecideAITangkisCoroutine()
    {
        yield return new WaitForSeconds(1f);
        List<Card> parryCombination = FindParryCombination(attackValue, ai.hand);
        
        if (parryCombination != null)
        {
            var cardValues = parryCombination.Select(c => c.value.ToString());
            uiManager.ShowMessage($"AI menangkis dengan [{string.Join(" + ", cardValues)}]!", 2f);
            yield return new WaitForSeconds(1.5f);
            
            foreach(var card in parryCombination) { ai.hand.Remove(card); }
            RefillHand(ai);
            
            currentAttackPhase = AttackPhase.AwaitingSerangBalik;
            StartCoroutine(ExecuteAISerangBalikCoroutine());
        }
        else
        {
            uiManager.ShowMessage("Serangan berhasil!", 2f);
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(RoundOverCoroutine(attacker));
        }
    }

    private List<Card> FindParryCombination(int target, List<Card> hand)
    {
        List<Card> FindSubsetSum(int currentTarget, List<Card> currentHand, List<Card> currentCombination)
        {
            if (currentTarget == 0)
            {
                if (hand.Count - currentCombination.Count >= 1) { return currentCombination; }
                return null;
            }
            if (currentTarget < 0 || currentHand.Count == 0) return null;

            Card head = currentHand[0];
            List<Card> tail = currentHand.GetRange(1, currentHand.Count - 1);
            
            List<Card> withHeadCombination = new List<Card>(currentCombination);
            withHeadCombination.Add(head);
            List<Card> resultWith = FindSubsetSum(currentTarget - head.value, tail, withHeadCombination);
            if (resultWith != null) return resultWith;

            List<Card> resultWithout = FindSubsetSum(currentTarget, tail, currentCombination);
            if (resultWithout != null) return resultWithout;

            return null;
        }
        return FindSubsetSum(target, new List<Card>(hand), new List<Card>());
    }

    private IEnumerator ExecuteAISerangBalikCoroutine()
    {
        if (ai.hand.Count == 0) { StartCoroutine(RoundOverCoroutine(ai)); yield break; }
        
        Card counterCard = ai.hand[Random.Range(0, ai.hand.Count)];
        uiManager.ShowMessage($"AI melakukan Serang Balik dengan kartu {counterCard.value}!", 2f);
        yield return new WaitForSeconds(1.5f);

        ai.hand.Remove(counterCard);
        InitiateAttack(ai, manusia, counterCard.value);
    }

    public void OnPlayerTangkisYes()
    {
        if (currentAttackPhase != AttackPhase.AwaitingTangkisPlayer) return;
        
        currentAttackPhase = AttackPhase.PlayerSelectingParryCards;
        
        uiManager.ShowHandPanelOnly("move");
        uiManager.ShowMessage("Pilih kartu, lalu tekan 'Tangkis' lagi untuk konfirmasi.", 0f);
        
        var tangkisButton = uiManager.AksiTangkisPanel.transform.Find("TangkisYesButton").GetComponent<Button>();
        tangkisButton.onClick.RemoveAllListeners();
        tangkisButton.onClick.AddListener(OnPlayerConfirmTangkis);
        
        uiManager.AksiTangkisPanel.SetActive(true);
        uiManager.SetPlayerHandInteractable(true);
        uiManager.UpdateSelectedParryTotal(0);
    }

    public void OnPlayerConfirmTangkis()
    {
        uiManager.SetPlayerHandInteractable(false);
        uiManager.HideAllPlayerPanels();

        int totalParryValue = selectedParryCards.Sum(entry => entry.Key.value);
        bool hasEnoughValue = totalParryValue == attackValue;
        bool hasRemainingCard = manusia.hand.Count - selectedParryCards.Count >= 1;

        if (hasEnoughValue && hasRemainingCard)
        {
            uiManager.ShowMessage("Serangan berhasil ditangkis!", 2.5f);
            foreach(var entry in selectedParryCards)
            {
                manusia.hand.Remove(entry.Key);
            }
            uiManager.UpdatePlayerHandUI(manusia);
            EndTurn();
        }
        else
        {
            Debug.Log($"Pemain gagal menangkis!");
            StartCoroutine(RoundOverCoroutine(attacker));
        }
        
        foreach(var entry in selectedParryCards) { entry.Value.ToggleSelection(false); }
        selectedParryCards.Clear();
        currentAttackPhase = AttackPhase.None;
        ResetTangkisButton();
    }

    public void OnPlayerTangkisNo()
    {
        if (currentAttackPhase != AttackPhase.AwaitingTangkisPlayer && currentAttackPhase != AttackPhase.PlayerSelectingParryCards) return;
        
        uiManager.HideAllPlayerPanels();
        currentAttackPhase = AttackPhase.None;
        ResetTangkisButton();
        
        Debug.Log("Pemain memilih tidak menangkis!");
        StartCoroutine(RoundOverCoroutine(attacker));
    }
    
    private void ResetTangkisButton()
    {
        if (uiManager != null && uiManager.AksiTangkisPanel != null)
        {
            var tangkisButton = uiManager.AksiTangkisPanel.transform.Find("TangkisYesButton").GetComponent<Button>();
            if (tangkisButton != null)
            {
                tangkisButton.onClick.RemoveAllListeners();
                tangkisButton.onClick.AddListener(OnPlayerTangkisYes);
            }
        }
    }
    
    private void RoundOver(Player winner)
    {
        StartCoroutine(RoundOverCoroutine(winner));
    }

    private IEnumerator RoundOverCoroutine(Player winner)
    {
        Debug.Log($"--- RONDE BERAKHIR! Pemenang: {winner.playerName} ---");
        uiManager.HideAttackStrength();
        uiManager.HideMessage();
        ResetTangkisButton();
        
        uiManager.ShowMessage($"{winner.playerName} memenangkan ronde!", 2.5f);
        
        winner.score++;
        uiManager.UpdateScoreUI(winner);

        yield return new WaitForSeconds(2.5f);

        if (winner.score >= 5)
        {
            isGameOver = true;
            uiManager.ShowMessage($"{winner.playerName} adalah PEMENANGNYA!", 0f);
            Debug.Log($"--- GAME BERAKHIR! PEMENANG UTAMA: {winner.playerName} ---");
            yield return new WaitForSeconds(2f);
            uiManager.ShowKemenanganPanel(winner == manusia);
        }
        else
        {
            StartRound();
        }
    }

    private void ExecuteTieBreaker()
    {
        StartCoroutine(ExecuteTieBreakerCoroutine());
    }

    private IEnumerator ExecuteTieBreakerCoroutine()
    {
        Debug.Log("--- TIE BREAKER DIMULAI ---");
        uiManager.HideAttackStrength();
        uiManager.ShowMessage("Kartu habis atau pemain terpojok! TIE BREAKER!", 2.5f);
        yield return new WaitForSeconds(2.5f);

        int totalManusia = manusia.hand.Sum(card => card.value);
        int totalAI = ai.hand.Sum(card => card.value);
        Debug.Log($"Total nilai tangan Manusia: {totalManusia}");
        Debug.Log($"Total nilai tangan AI: {totalAI}");
        
        if (totalManusia > totalAI) { RoundOver(manusia); }
        else if (totalAI > totalManusia) { RoundOver(ai); }
        else
        {
            uiManager.ShowMessage("Hasil Tie Breaker seri!", 2.5f);
            yield return new WaitForSeconds(2.5f);
            StartRound();
        }
    }
    
    private void MovePawnVisual(Player player, int targetPosition)
{
    GameObject pawnToMove = player.isAI ? pionAI : pionManusia;
    Transform targetPetak = petakPapan[targetPosition - 1];

    // --- BARIS YANG DIUBAH ---
    // Awalnya:
    // pawnToMove.transform.position = targetPetak.position;

    // Menjadi (tambahkan nilai Y untuk mengangkat pion):
    pawnToMove.transform.position = targetPetak.position + new Vector3(0, 75f, 0);
    // -------------------------
}

    private void RefillHand(Player player)
    {
        while (player.hand.Count < 5)
        {
            Card newCard = DrawCardFromDeck();
            if (newCard == null) 
            {
                Debug.Log("Deck habis saat refill!");
                StartCoroutine(ExecuteTieBreakerCoroutine());
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
}