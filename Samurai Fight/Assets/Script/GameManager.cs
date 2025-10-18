using System;
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
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetupGame();
        StartRound();
    }

    void SetupGame()
    {
        manusia = new Player("Manusia", 1);
        ai = new Player("AI", 23, true);
        if (uiManager != null) uiManager.ResetScoreUI();
    }

    public void StartRound()
    {
        currentAttackPhase = AttackPhase.None;
        isPlayerMelangkah = false;
        isPlayerMenyerang = false;
        isPlayerInSergapMode = false;
        isPlayerStrengtheningAttack = false;
        isPlayerInSerangBalikMode = false;
        isCurrentAttackACounter = false;
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

        if (uiManager != null)
        {
            uiManager.UpdatePlayerHandUI(manusia);
            uiManager.UpdateMainDeckUI(deck.Count);
        }

        activePlayer = ai;
        StartTurn();
    }

    private void StartTurn()
    {
        if (isGameOver) return;

        if (uiManager != null) uiManager.RestoreDefaultLayout();
        Debug.Log($"Sekarang giliran: {activePlayer.playerName}");

        if (activePlayer.isAI)
        {
            StartCoroutine(ExecuteAITurnCoroutine());
        }
        else
        {
            if (uiManager != null) uiManager.ShowMessage("Giliran Anda! Pilih aksi: Melangkah atau Serang.", 0f);
            CheckAvailablePlayerActions();
            if (uiManager != null && uiManager.OpsiAwalPanel != null) uiManager.OpsiAwalPanel.SetActive(true);

            if (uiManager != null && uiManager.KartuManusiaPanel != null)
            {
                uiManager.PindahkanPanelKartu(uiManager.posisiPanelDefault);
                uiManager.KartuManusiaPanel.SetActive(true);
            }

            if (uiManager != null) uiManager.SetPlayerHandInteractable(false);
        }
    }

    private void EndTurn()
    {
        if (isGameOver) return;

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

    public void OnMelangkahButtonPressed()
    {
        if (activePlayer != manusia) return;
        isPlayerMelangkah = true;
        if (uiManager != null) uiManager.ShowMessage("Pilih arah tujuan Anda.", 0f);
        if (uiManager != null && uiManager.OpsiAwalPanel != null) uiManager.OpsiAwalPanel.SetActive(false);

        // sembunyikan panel kartu
        if (uiManager != null && uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(false);

        if (uiManager != null && uiManager.AksiMelangkahPanel != null) uiManager.AksiMelangkahPanel.SetActive(true);
        CheckAvailableMoveDirections();
    }

    public void OnArahLangkahPressed(bool isMaju)
    {
        if (activePlayer != manusia) return;
        if (uiManager != null && uiManager.AksiMelangkahPanel != null) uiManager.AksiMelangkahPanel.SetActive(false);
        arahLangkah = isMaju ? 1 : -1;
        string arah = isMaju ? "maju" : "mundur";
        if (uiManager != null) uiManager.ShowMessage($"Anda melangkah {arah}. Pilih satu kartu untuk menentukan jarak.", 0f);

        if (uiManager != null && uiManager.KartuManusiaPanel != null)
        {
            uiManager.PindahkanPanelKartu(uiManager.posisiPanelKanan);
            uiManager.KartuManusiaPanel.SetActive(true);
        }
        if (uiManager != null) uiManager.SetPlayerHandInteractable(true);
    }

    public void OnSerangButtonPressed()
    {
        if (activePlayer != manusia) return;
        if (uiManager != null && uiManager.OpsiAwalPanel != null) uiManager.OpsiAwalPanel.SetActive(false);
        isPlayerMenyerang = true;
        if (uiManager != null) uiManager.ShowMessage("Pilih kartu yang nilainya sama dengan jarak Anda ke lawan.", 0f);

        if (uiManager != null && uiManager.KartuManusiaPanel != null)
        {
            uiManager.PindahkanPanelKartu(uiManager.posisiPanelBawah);
            uiManager.KartuManusiaPanel.SetActive(true);
        }
        if (uiManager != null) uiManager.SetPlayerHandInteractable(true);
    }

    public void OnCardSlotClicked(Card clickedCard, SistemKartu slotController)
    {
        // BLOK 1: prioritas saat memilih kartu tangkisan
        if (currentAttackPhase == AttackPhase.PlayerSelectingParryCards)
        {
            HandleParrySelection(clickedCard, slotController);
            return;
        }

        // BLOK 2: aksi normal / reaktif
        if (activePlayer != manusia && !isPlayerInSerangBalikMode) return;
        if (!isPlayerMenyerang && !isPlayerMelangkah && !isPlayerInSergapMode && !isPlayerStrengtheningAttack && !isPlayerInSerangBalikMode)
            return;

        if (uiManager != null) uiManager.SetPlayerHandInteractable(false);
        if (uiManager != null) uiManager.HideMessage();
        HandleCardAction(clickedCard);
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
                if (uiManager != null) uiManager.UpdatePlayerHandUI(manusia);

                if (manusia.hand.Count > 0)
                {
                    // sembunyikan panel kartu sebelum menampilkan pilihan
                    if (uiManager != null && uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(false);
                    if (uiManager != null && uiManager.PerkuatSeranganPanel != null) uiManager.PerkuatSeranganPanel.SetActive(true);
                    if (uiManager != null) uiManager.ShowMessage("Serangan awal siap. Ingin perkuat dengan kartu tambahan?", 0f);
                }
                else
                {
                    InitiateAttack(manusia, ai, initialAttackCard.value, false);
                }
            }
            else
            {
                StartTurn();
            }
        }
        else if (isPlayerStrengtheningAttack)
        {
            isPlayerStrengtheningAttack = false;
            int totalAttackValue = initialAttackCard.value + clickedCard.value;
            manusia.hand.Remove(clickedCard);
            if (uiManager != null) uiManager.UpdatePlayerHandUI(manusia);
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
            if (uiManager != null) uiManager.UpdatePlayerHandUI(manusia);
            InitiateAttack(manusia, ai, clickedCard.value, true);
        }
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
            if (uiManager != null) uiManager.UpdatePlayerHandUI(manusia);
            isPlayerMelangkah = false;

            int newDistance = ai.position - newPosition;
            bool canSergap = manusia.hand.Any(card => card.value == newDistance);
            if (canSergap)
            {
                if (uiManager != null) uiManager.ShowMessage("Langkah berhasil! Anda kini dalam jangkauan. Lakukan Sergap?", 0f);
                if (uiManager != null && uiManager.AksiSergapPanel != null) uiManager.AksiSergapPanel.SetActive(true);
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
            if (uiManager != null) uiManager.UpdatePlayerHandUI(manusia);
            InitiateAttack(manusia, ai, clickedCard.value, false);
        }
        else
        {
            EndTurn();
        }
    }

    private void InitiateAttack(Player currentAttacker, Player currentDefender, int value, bool isCounter)
    {
        attacker = currentAttacker;
        defender = currentDefender;
        attackValue = value;
        isCurrentAttackACounter = isCounter;

        if (uiManager != null) uiManager.ShowAttackStrength(attackValue);

        if (defender.isAI)
        {
            currentAttackPhase = AttackPhase.AwaitingTangkis;
            StartCoroutine(DecideAITangkisCoroutine());
        }
        else
        {
            currentAttackPhase = AttackPhase.AwaitingTangkisPlayer;
            string message = isCounter ? $"AI melakukan Serang Balik dengan kekuatan {value}! Tangkis serangan ini?" : $"Anda diserang dengan kekuatan {value}! Tangkis serangan ini?";
            if (uiManager != null) uiManager.ShowMessage(message, 0f);

            // sembunyikan panel kartu dan info serangan saat pilihan muncul
            if (uiManager != null && uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(false);
            if (uiManager != null) uiManager.HideAttackStrength();
            if (uiManager != null && uiManager.AksiTangkisPanel != null) uiManager.AksiTangkisPanel.SetActive(true);
        }
    }

    public void OnPlayerTangkisYes()
    {
        if (currentAttackPhase != AttackPhase.AwaitingTangkisPlayer) return;

        currentAttackPhase = AttackPhase.PlayerSelectingParryCards;
        if (uiManager != null && uiManager.AksiTangkisPanel != null) uiManager.AksiTangkisPanel.SetActive(false);
        if (uiManager != null && uiManager.KonfirmasiTangkisButton != null)
        {
            uiManager.KonfirmasiTangkisButton.gameObject.SetActive(true);
            uiManager.KonfirmasiTangkisButton.onClick.RemoveAllListeners();
            uiManager.KonfirmasiTangkisButton.onClick.AddListener(OnPlayerConfirmTangkis);
        }

        if (uiManager != null) uiManager.ShowMessage($"Pilih kartu dengan total nilai {attackValue}, lalu tekan Konfirmasi.", 0f);

        if (uiManager != null && uiManager.KartuManusiaPanel != null)
        {
            uiManager.PindahkanPanelKartu(uiManager.posisiPanelKanan);
            uiManager.KartuManusiaPanel.SetActive(true);
        }

        if (uiManager != null) uiManager.SetPlayerHandInteractable(true);
        if (uiManager != null) uiManager.UpdateSelectedParryTotal(0, attackValue);
    }

    public void OnPlayerTangkisNo()
    {
        if (currentAttackPhase != AttackPhase.AwaitingTangkisPlayer) return;
        if (uiManager != null) uiManager.HideAllPlayerPanels();
        currentAttackPhase = AttackPhase.None;
        StartCoroutine(RoundOverCoroutine(attacker));
    }

    private void HandleParrySelection(Card clickedCard, SistemKartu slotController)
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
        if (uiManager != null) uiManager.UpdateSelectedParryTotal(currentParryTotal, attackValue);
    }

    public void OnPlayerConfirmTangkis()
    {
        if (uiManager != null && uiManager.KonfirmasiTangkisButton != null)
            uiManager.KonfirmasiTangkisButton.gameObject.SetActive(false);

        if (uiManager != null) uiManager.SetPlayerHandInteractable(false);
        if (uiManager != null) uiManager.HideMessage();

        int totalParryValue = selectedParryCards.Sum(entry => entry.Key.value);
        bool hasEnoughValue = totalParryValue == attackValue;
        bool hasRemainingCardForCounter = manusia.hand.Count - selectedParryCards.Count >= 1;

        if (hasEnoughValue)
        {
            if (isCurrentAttackACounter)
            {
                if (uiManager != null) uiManager.ShowMessage("Serang balik berhasil ditangkis!", 2.5f);
                foreach (var entry in selectedParryCards) manusia.hand.Remove(entry.Key);
                if (uiManager != null) uiManager.UpdatePlayerHandUI(manusia);
                EndTurn();
            }
            else if (hasRemainingCardForCounter)
            {
                if (uiManager != null) uiManager.ShowMessage("Tangkisan berhasil! Siapkan Serang Balik.", 1.5f);
                foreach (var entry in selectedParryCards) manusia.hand.Remove(entry.Key);
                if (uiManager != null) uiManager.UpdatePlayerHandUI(manusia);
                StartCoroutine(ExecutePlayerSerangBalikCoroutine());
            }
            else
            {
                if (uiManager != null) uiManager.ShowMessage("Tangkisan Gagal! Anda harus menyisakan minimal 1 kartu untuk bisa Serang Balik.", 3f);
                StartCoroutine(RoundOverCoroutine(attacker));
            }
        }
        else
        {
            if (uiManager != null) uiManager.ShowTangkisanGagalMessage(totalParryValue, attackValue, 3f);
            StartCoroutine(RoundOverCoroutine(attacker));
        }

        foreach (var entry in selectedParryCards)
            entry.Value.ToggleSelection(false);

        selectedParryCards.Clear();
        currentAttackPhase = AttackPhase.None;
    }

    private IEnumerator ExecutePlayerSerangBalikCoroutine()
    {
        yield return new WaitForSeconds(1.5f);
        isPlayerInSerangBalikMode = true;
        if (uiManager != null) uiManager.ShowMessage("Pilih satu kartu untuk melakukan Serang Balik.", 0f);
        if (uiManager != null && uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(true);
        if (uiManager != null) uiManager.SetPlayerHandInteractable(true);
    }

    private IEnumerator ExecuteAITurnCoroutine()
    {
        if (uiManager != null) uiManager.ShowMessage("Giliran AI...", 0f);
        yield return new WaitForSeconds(1.5f);

        if (ai.hand.Count == 0) { EndTurn(); yield break; }

        int distance = ai.position - manusia.position;
        bool canAttack = ai.hand.Any(card => card.value == distance);
        bool canMoveForward = ai.hand.Any(card => ai.position - card.value > manusia.position);
        bool canMoveBackward = ai.hand.Any(card => ai.position + card.value <= 23);

        if (canAttack)
        {
            Card attackCard = ai.hand.First(card => card.value == distance);
            if (uiManager != null) uiManager.ShowMessage($"AI menyerang dengan kekuatan {attackCard.value}.", 2f);
            yield return new WaitForSeconds(1f);
            ai.hand.Remove(attackCard);
            InitiateAttack(ai, manusia, attackCard.value, isCounter: false);
        }
        else if (canMoveForward)
        {
            var validCards = ai.hand.Where(card => ai.position - card.value > manusia.position).ToList();
            Card chosenCard = validCards[UnityEngine.Random.Range(0, validCards.Count)];
            ai.position -= chosenCard.value;
            if (uiManager != null) uiManager.ShowMessage("AI melangkah maju.", 2f);
            MovePawnVisual(ai, ai.position);
            ai.hand.Remove(chosenCard);
            yield return new WaitForSeconds(1.5f);
            EndTurn();
        }
        else if (canMoveBackward)
        {
            var validCards = ai.hand.Where(card => ai.position + card.value <= 23).ToList();
            Card chosenCard = validCards[UnityEngine.Random.Range(0, validCards.Count)];
            ai.position += chosenCard.value;
            if (uiManager != null) uiManager.ShowMessage("AI melangkah mundur.", 2f);
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
            if (uiManager != null) uiManager.ShowMessage($"AI berhasil menangkis serangan Anda.", 2.5f);
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
            StartCoroutine(RoundOverCoroutine(attacker));
        }
    }

    private IEnumerator ExecuteAISerangBalikCoroutine()
    {
        if (ai.hand.Count == 0) { EndTurn(); yield break; }
        Card counterCard = ai.hand[UnityEngine.Random.Range(0, ai.hand.Count)];
        if (uiManager != null) uiManager.ShowMessage($"AI melakukan Serang Balik dengan kekuatan {counterCard.value}!", 2f);
        yield return new WaitForSeconds(1.5f);
        ai.hand.Remove(counterCard);
        InitiateAttack(ai, manusia, counterCard.value, true);
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

    private IEnumerator RoundOverCoroutine(Player winner)
    {
        if (uiManager != null) uiManager.HideAllPlayerPanels();
        if (uiManager != null) uiManager.HideAttackStrength();
        if (uiManager != null) uiManager.HideMessage();
        if (uiManager != null) uiManager.ShowMessage($"{winner.playerName} memenangkan ronde!", 2.5f);

        winner.score++;
        if (uiManager != null) uiManager.UpdateScoreUI(winner);
        yield return new WaitForSeconds(2.5f);

        if (winner.score >= 5)
        {
            isGameOver = true;
            yield return new WaitForSeconds(0.5f);
            if (uiManager != null) uiManager.ShowKemenanganPanel(winner == manusia);
        }
        else
        {
            StartRound();
        }
    }

    private IEnumerator ExecuteTieBreakerCoroutine()
    {
        if (uiManager != null) uiManager.HideAllPlayerPanels();
        if (uiManager != null) uiManager.HideAttackStrength();
        if (uiManager != null) uiManager.ShowMessage("Tidak ada aksi tersisa! TIE BREAKER diaktifkan.", 2.5f);
        yield return new WaitForSeconds(2.5f);

        int totalManusia = manusia.hand.Sum(card => card.value);
        int totalAI = ai.hand.Sum(card => card.value);

        Player winner = null;
        if (totalManusia > totalAI) winner = manusia;
        else if (totalAI > totalManusia) winner = ai; // fixed logical comparison (was bug in original)

        if (winner != null)
        {
            StartCoroutine(RoundOverCoroutine(winner));
        }
        else
        {
            if (uiManager != null) uiManager.ShowMessage("Hasil Tie Breaker seri!", 2.5f);
            yield return new WaitForSeconds(2.5f);
            StartRound();
        }
    }

    public void OnPerkuatYesButtonPressed()
    {
        if (uiManager != null && uiManager.PerkuatSeranganPanel != null) uiManager.PerkuatSeranganPanel.SetActive(false);

        isPlayerStrengtheningAttack = true;
        if (uiManager != null) uiManager.ShowMessage("Pilih satu kartu tambahan untuk memperkuat serangan.", 0f);

        if (uiManager != null && uiManager.KartuManusiaPanel != null)
        {
            uiManager.PindahkanPanelKartu(uiManager.posisiPanelKanan);
            uiManager.KartuManusiaPanel.SetActive(true);
        }
        if (uiManager != null) uiManager.SetPlayerHandInteractable(true);
    }

    public void OnPerkuatNoButtonPressed()
    {
        if (uiManager != null) uiManager.HideAllPlayerPanels();
        InitiateAttack(manusia, ai, initialAttackCard.value, false);
    }

    public void OnSergapYesButtonPressed()
    {
        if (uiManager != null && uiManager.AksiSergapPanel != null) uiManager.AksiSergapPanel.SetActive(false);
        isPlayerInSergapMode = true;
        if (uiManager != null) uiManager.ShowMessage("Pilih kartu untuk melakukan Sergap.", 0f);

        if (uiManager != null && uiManager.KartuManusiaPanel != null)
        {
            uiManager.PindahkanPanelKartu(uiManager.posisiPanelBawah);
            uiManager.KartuManusiaPanel.SetActive(true);
        }
        if (uiManager != null) uiManager.SetPlayerHandInteractable(true);
    }

    public void OnSergapNoButtonPressed()
    {
        if (uiManager != null) uiManager.HideAllPlayerPanels();
        EndTurn();
    }

    private void CheckAvailablePlayerActions()
    {
        bool canMove = manusia.hand.Any(card => (manusia.position + card.value < ai.position) || (manusia.position - card.value >= 1));
        int distance = ai.position - manusia.position;
        bool canAttack = manusia.hand.Any(card => card.value == distance);
        if (uiManager != null) uiManager.UpdateActionButtons(canMove, canAttack);

        if (!canMove && !canAttack)
        {
            StartCoroutine(ExecuteTieBreakerCoroutine());
        }
    }

    private void CheckAvailableMoveDirections()
    {
        bool canMoveForward = manusia.hand.Any(card => manusia.position + card.value < ai.position);
        bool canMoveBackward = manusia.hand.Any(card => manusia.position - card.value >= 1);
        if (uiManager != null) uiManager.UpdateMoveDirectionButtons(canMoveForward, canMoveBackward);
    }

    private void MovePawnVisual(Player player, int targetPosition)
    {
        GameObject pawn = player.isAI ? pionAI : pionManusia;
        if (pawn == null || petakPapan == null) return;
        if (targetPosition > 0 && targetPosition <= petakPapan.Length)
        {
            Transform targetPetak = petakPapan[targetPosition - 1];
            if (targetPetak != null)
                pawn.transform.position = targetPetak.position + new Vector3(0, 75f, 0);
        }
    }

    private void RefillHand(Player player)
    {
        while (player.hand.Count < 5)
        {
            Card newCard = DrawCardFromDeck();
            if (newCard == null) return;
            player.hand.Add(newCard);
        }
        if (!player.isAI && uiManager != null) uiManager.UpdatePlayerHandUI(player);
        if (uiManager != null) uiManager.UpdateMainDeckUI(deck.Count);
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
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card value = deck[k];
            deck[k] = deck[n];
            deck[n] = value;
        }
    }

    private Card DrawCardFromDeck()
    {
        if (deck.Count == 0) return null;
        Card drawn = deck[0];
        deck.RemoveAt(0);
        return drawn;
    }

    public void PlayAgain()
    {
        manusia.score = 0;
        ai.score = 0;
        Start();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
