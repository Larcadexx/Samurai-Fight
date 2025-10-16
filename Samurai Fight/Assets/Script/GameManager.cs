// Salin dan ganti seluruh isi script GameManager.cs Anda dengan kode ini

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    private enum PlayerTurnState
    {
        None,
        SelectingMoveCard,
        SelectingAttackCard,
        StrengtheningAttack,
        SelectingSergapCard,
        SelectingCounterCard
    }

    private Player manusia;
    private Player ai;
    private List<Card> deck = new List<Card>();

    private Player activePlayer;
    private bool isGameOver = false;
    private int roundNumber = 0;

    private PlayerTurnState playerState;
    private int arahLangkah = 0;

    private const float BUTTON_PRESS_DELAY = 0.3f;

    private enum AttackPhase { None, AwaitingTangkis, AwaitingSerangBalik, AwaitingTangkisPlayer, PlayerSelectingParryCards }
    private AttackPhase currentAttackPhase = AttackPhase.None;
    private bool isCurrentAttackACounter = false;

    public enum TieBreakerReason { PlayerCornered, DeckEmpty }

    private Player attacker;
    private Player defender;
    private int attackValue;
    private Card initialAttackCard;
    private List<KeyValuePair<Card, SistemKartu>> selectedParryCards = new List<KeyValuePair<Card, SistemKartu>>();

    void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
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
        uiManager.ResetScoreUI();
    }

    public void StartRound()
    {
        currentAttackPhase = AttackPhase.None;
        playerState = PlayerTurnState.None;
        isCurrentAttackACounter = false;
        isGameOver = false;
        roundNumber++;

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
        StartCoroutine(ShowRoundStartMessageAndBeginTurn());
    }

    private IEnumerator ShowRoundStartMessageAndBeginTurn()
    {
        uiManager.HideAllPlayerPanels();
        uiManager.ShowMessage($"Ronde Ke-{roundNumber}", 2.0f);
        yield return new WaitForSeconds(2.0f);
        StartTurn();
    }

    private void StartTurn()
    {
        if (isGameOver) return;

        playerState = PlayerTurnState.None;
        uiManager.RestoreDefaultLayout();

        if (activePlayer.isAI)
        {
            StartCoroutine(ExecuteAITurnCoroutine());
        }
        else
        {
            StartCoroutine(ShowPlayerTurnMessage());
        }
    }

    private IEnumerator ShowPlayerTurnMessage()
    {
        uiManager.ShowMessage("Giliran anda!!", 1.5f);
        yield return new WaitForSeconds(1.5f);
        CheckAvailablePlayerActions();
    }


    private void EndTurn()
    {
        if (isGameOver) return;

        RefillHand(manusia);
        RefillHand(ai);

        if (deck.Count == 0 && (manusia.hand.Count < 5 || ai.hand.Count < 5))
        {
            StartCoroutine(ExecuteTieBreakerCoroutine(TieBreakerReason.DeckEmpty));
            return;
        }

        activePlayer = (activePlayer == manusia) ? ai : manusia;
        StartTurn();
    }

    public void OnMelangkahButtonPressed()
    {
        StartCoroutine(OnMelangkahButtonPressedCoroutine());
    }

    private IEnumerator OnMelangkahButtonPressedCoroutine()
    {
        if (activePlayer != manusia) yield break;
        yield return new WaitForSeconds(BUTTON_PRESS_DELAY);
        CheckAvailableMoveDirections();
    }

    public void OnArahLangkahPressed(bool isMaju)
    {
        StartCoroutine(OnArahLangkahPressedCoroutine(isMaju));
    }

    private IEnumerator OnArahLangkahPressedCoroutine(bool isMaju)
    {
        if (activePlayer != manusia) yield break;
        yield return new WaitForSeconds(BUTTON_PRESS_DELAY);

        arahLangkah = isMaju ? 1 : -1;
        playerState = PlayerTurnState.SelectingMoveCard;
        uiManager.TampilkanUI_PilihKartuUntukAksi("melangkah");
    }

    public void OnSerangButtonPressed()
    {
        StartCoroutine(OnSerangButtonPressedCoroutine());
    }

    private IEnumerator OnSerangButtonPressedCoroutine()
    {
        if (activePlayer != manusia) yield break;
        yield return new WaitForSeconds(BUTTON_PRESS_DELAY);

        playerState = PlayerTurnState.SelectingAttackCard;
        uiManager.TampilkanUI_PilihKartuUntukAksi("serang");
    }

    public void OnCardSlotClicked(Card clickedCard, SistemKartu slotController)
    {
        if (currentAttackPhase == AttackPhase.PlayerSelectingParryCards)
        {
            HandleParrySelection(clickedCard, slotController);
            return;
        }

        if (playerState == PlayerTurnState.None) return;

        uiManager.SembunyikanPanelKartu();
        uiManager.SetPlayerHandInteractable(false);
        uiManager.HideMessage();

        StartCoroutine(HandleCardActionCoroutine(clickedCard));
    }

    private IEnumerator HandleCardActionCoroutine(Card clickedCard)
    {
        PlayerTurnState stateSaatIni = playerState;
        playerState = PlayerTurnState.None;

        switch (stateSaatIni)
        {
            case PlayerTurnState.SelectingAttackCard:
                int distance = ai.position - manusia.position;
                if (clickedCard.value == distance)
                {
                    initialAttackCard = clickedCard;
                    manusia.hand.Remove(clickedCard);
                    uiManager.UpdatePlayerHandUI(manusia);
                    if (manusia.hand.Count > 0)
                    {
                        uiManager.TampilkanUI_TawarkanPerkuatSerangan();
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
                break;

            case PlayerTurnState.StrengtheningAttack:
                int totalAttackValue = initialAttackCard.value + clickedCard.value;
                manusia.hand.Remove(clickedCard);
                uiManager.UpdatePlayerHandUI(manusia);
                InitiateAttack(manusia, ai, totalAttackValue, false);
                break;

            case PlayerTurnState.SelectingMoveCard:
                yield return StartCoroutine(HandleMoveCoroutine(clickedCard));
                break;

            case PlayerTurnState.SelectingSergapCard:
                // Diubah dari Coroutine kembali ke void biasa
                HandleSergap(clickedCard);
                break;

            case PlayerTurnState.SelectingCounterCard:
                manusia.hand.Remove(clickedCard);
                uiManager.UpdatePlayerHandUI(manusia);
                InitiateAttack(manusia, ai, clickedCard.value, true);
                break;
        }
    }

    private IEnumerator HandleMoveCoroutine(Card clickedCard)
    {
        int newPosition = manusia.position + (clickedCard.value * arahLangkah);
        bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);

        if (isValidMove)
        {
            manusia.position = newPosition;
            MovePawnVisual(manusia, manusia.position);
            manusia.hand.Remove(clickedCard);
            uiManager.UpdatePlayerHandUI(manusia);

            yield return new WaitForSeconds(2.5f);

            int newDistance = ai.position - newPosition;
            bool canSergap = manusia.hand.Any(card => card.value == newDistance);
            if (canSergap)
            {
                uiManager.TampilkanUI_TawarkanSergap();
            }
            else
            {
                EndTurn();
            }
        }
        else
        {
            StartTurn();
        }
    }
    
    // <-- PERUBAHAN 2: Diubah kembali menjadi void dan logika visual dihapus -->
    private void HandleSergap(Card clickedCard)
    {
        int distance = ai.position - manusia.position;
        if (clickedCard.value == distance)
        {
            manusia.hand.Remove(clickedCard);
            uiManager.UpdatePlayerHandUI(manusia);
            // Langsung panggil InitiateAttack tanpa efek visual sebelumnya
            InitiateAttack(manusia, ai, clickedCard.value, false);
        }
        else
        {
            uiManager.ShowMessage("Sergap GAGAL!! Nilai tidak sesuai", 2.5f);
            // Panggil EndTurn tanpa delay agar alur tetap cepat
            EndTurn();
        }
    }


    private void InitiateAttack(Player currentAttacker, Player currentDefender, int value, bool isCounter)
    {
        attacker = currentAttacker;
        defender = currentDefender;
        attackValue = value;
        isCurrentAttackACounter = isCounter;

        uiManager.ShowAttackStrength(attackValue);

        if (defender.isAI)
        {
            currentAttackPhase = AttackPhase.AwaitingTangkis;
            StartCoroutine(DecideAITangkisCoroutine());
        }
        else
        {
            currentAttackPhase = AttackPhase.AwaitingTangkisPlayer;
            uiManager.TampilkanUI_TangkisSerangan(value, isCounter);
        }
    }

    public void OnPlayerTangkisYes()
    {
        if (currentAttackPhase != AttackPhase.AwaitingTangkisPlayer) return;
        currentAttackPhase = AttackPhase.PlayerSelectingParryCards;
        uiManager.TampilkanUI_PilihKartuTangkis(attackValue);
        uiManager.KonfirmasiTangkisButton.onClick.RemoveAllListeners();
        uiManager.KonfirmasiTangkisButton.onClick.AddListener(OnPlayerConfirmTangkis);
    }

    public void OnPlayerTangkisNo()
    {
        if (currentAttackPhase != AttackPhase.AwaitingTangkisPlayer) return;
        uiManager.HideAllPlayerPanels();
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
        uiManager.UpdateSelectedParryTotal(currentParryTotal, attackValue);
    }

    public void OnPlayerConfirmTangkis()
    {
        uiManager.SembunyikanTombolKonfirmasiTangkis();
        uiManager.SetPlayerHandInteractable(false);
        uiManager.HideMessage();

        int totalParryValue = selectedParryCards.Sum(entry => entry.Key.value);
        bool isValueMatch = totalParryValue == attackValue;
        bool hasCardLeftForCounter = manusia.hand.Count - selectedParryCards.Count >= 1;

        if (isValueMatch)
        {
            if (isCurrentAttackACounter)
            {
                uiManager.ShowMessage("Serang balik berhasil ditangkis!", 3.0f);
                foreach (var entry in selectedParryCards) manusia.hand.Remove(entry.Key);
                uiManager.UpdatePlayerHandUI(manusia);
                EndTurn();
            }
            else if (hasCardLeftForCounter)
            {
                uiManager.ShowMessage("Tangkisan berhasil! Siapkan Serang Balik.", 2.5f);
                foreach (var entry in selectedParryCards) manusia.hand.Remove(entry.Key);
                uiManager.UpdatePlayerHandUI(manusia);
                StartCoroutine(ExecutePlayerSerangBalikCoroutine());
            }
            else
            {
                uiManager.ShowMessage("Tangkisan Tidak Sah! Wajib menyisakan 1 kartu untuk Serang Balik.", 3.5f);
                StartCoroutine(RoundOverCoroutine(attacker));
            }
        }
        else
        {
            uiManager.ShowTangkisanGagalMessage(3.5f);
            StartCoroutine(RoundOverCoroutine(attacker));
        }

        foreach (var entry in selectedParryCards) entry.Value.ToggleSelection(false);
        selectedParryCards.Clear();
        currentAttackPhase = AttackPhase.None;
    }

    private IEnumerator ExecutePlayerSerangBalikCoroutine()
    {
        yield return new WaitForSeconds(2.5f);
        playerState = PlayerTurnState.SelectingCounterCard;
        uiManager.TampilkanUI_PilihKartuUntukAksi("serangbalik");
    }

    private IEnumerator ExecuteAITurnCoroutine()
    {
        uiManager.TampilkanInfoGiliranAI();
        yield return new WaitForSeconds(2.5f);

        if (ai.hand.Count == 0) { EndTurn(); yield break; }

        int distance = ai.position - manusia.position;
        bool canAttack = ai.hand.Any(card => card.value == distance);
        bool canMoveForward = ai.hand.Any(card => ai.position - card.value > manusia.position);
        bool canMoveBackward = ai.hand.Any(card => ai.position + card.value <= 23);

        if (canAttack)
        {
            Card attackCard = ai.hand.First(card => card.value == distance);
            uiManager.ShowMessage($"AI menyerang dengan kekuatan {attackCard.value}.", 2.5f);
            yield return new WaitForSeconds(2.0f);
            ai.hand.Remove(attackCard);
            InitiateAttack(ai, manusia, attackCard.value, isCounter: false);
        }
        else if (canMoveForward)
        {
            var validCards = ai.hand.Where(card => ai.position - card.value > manusia.position).ToList();
            Card chosenCard = validCards[Random.Range(0, validCards.Count)];
            ai.position -= chosenCard.value;
            uiManager.ShowMessage("AI melangkah maju.", 2.5f);
            MovePawnVisual(ai, ai.position);
            ai.hand.Remove(chosenCard);
            yield return new WaitForSeconds(2.5f);
            EndTurn();
        }
        else if (canMoveBackward)
        {
            var validCards = ai.hand.Where(card => ai.position + card.value <= 23).ToList();
            Card chosenCard = validCards[Random.Range(0, validCards.Count)];
            ai.position += chosenCard.value;
            uiManager.ShowMessage("AI melangkah mundur.", 2.5f);
            MovePawnVisual(ai, ai.position);
            ai.hand.Remove(chosenCard);
            yield return new WaitForSeconds(2.5f);
            EndTurn();
        }
        else
        {
            StartCoroutine(ExecuteTieBreakerCoroutine(TieBreakerReason.PlayerCornered));
        }
    }

    private IEnumerator DecideAITangkisCoroutine()
    {
        yield return new WaitForSeconds(2.5f);
        List<Card> parryCombination = FindParryCombination(attackValue, ai.hand, !isCurrentAttackACounter);

        if (parryCombination != null)
        {
            uiManager.ShowMessage($"AI berhasil menangkis serangan Anda.", 3.0f);
            yield return new WaitForSeconds(3.0f);
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
            uiManager.ShowMessage($"AI gagal menangkis serangan Anda.", 2.5f);
            yield return new WaitForSeconds(2.5f);
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
        uiManager.ShowMessage($"AI melakukan Serang Balik dengan kekuatan {counterCard.value}!", 2.5f);
        yield return new WaitForSeconds(2.5f);
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
        uiManager.HideAllUIsForRoundEnd();
        uiManager.ShowMessage($"{winner.playerName} memenangkan Ronde Ke-{roundNumber}!", 3.5f);
        winner.score++;
        uiManager.UpdateScoreUI(winner);
        yield return new WaitForSeconds(3.5f);

        if (winner.score >= 5)
        {
            isGameOver = true;
            yield return new WaitForSeconds(0.5f);
            uiManager.ShowKemenanganPanel(winner == manusia);
        }
        else
        {
            StartRound();
        }
    }

    private IEnumerator ExecuteTieBreakerCoroutine(TieBreakerReason reason)
    {
        uiManager.HideAllUIsForRoundEnd();

        string message = "";
        switch (reason)
        {
            case TieBreakerReason.PlayerCornered:
                message = "Pemain Terpojok! TIE BREAKER diaktifkan.";
                break;
            case TieBreakerReason.DeckEmpty:
                message = "Deck Kartu Habis! TIE BREAKER diaktifkan.";
                break;
        }
        uiManager.ShowMessage(message, 3.5f);
        yield return new WaitForSeconds(3.5f);

        int totalManusia = manusia.hand.Sum(card => card.value);
        int totalAI = ai.hand.Sum(card => card.value);

        Player winner = null;
        if (totalManusia > totalAI) { winner = manusia; }
        else if (totalAI > totalManusia) { winner = ai; }

        if (winner != null)
        {
            StartCoroutine(RoundOverCoroutine(winner));
        }
        else
        {
            uiManager.ShowMessage("Hasil Tie Breaker seri!", 3.0f);
            yield return new WaitForSeconds(3.0f);
            StartRound();
        }
    }

    public void OnPerkuatYesButtonPressed()
    {
        playerState = PlayerTurnState.StrengtheningAttack;
        uiManager.TampilkanUI_PilihKartuUntukAksi("perkuat");
    }

    public void OnPerkuatNoButtonPressed()
    {
        uiManager.HideAllPlayerPanels();
        InitiateAttack(manusia, ai, initialAttackCard.value, false);
    }

    public void OnSergapYesButtonPressed()
    {
        playerState = PlayerTurnState.SelectingSergapCard;
        uiManager.TampilkanUI_PilihKartuUntukAksi("sergap");
    }

    public void OnSergapNoButtonPressed()
    {
        uiManager.HideAllPlayerPanels();
        EndTurn();
    }

    private void CheckAvailablePlayerActions()
    {
        bool canMove = manusia.hand.Any(card => (manusia.position + card.value < ai.position) || (manusia.position - card.value >= 1));
        int distance = ai.position - manusia.position;
        bool canAttack = manusia.hand.Any(card => card.value == distance);
        
        uiManager.TampilkanUI_PilihAksiAwal(canMove, canAttack);

        if (!canMove && !canAttack)
        {
            StartCoroutine(ExecuteTieBreakerCoroutine(TieBreakerReason.PlayerCornered));
        }
    }

    private void CheckAvailableMoveDirections()
    {
        bool canMoveForward = manusia.hand.Any(card => manusia.position + card.value < ai.position);
        bool canMoveBackward = manusia.hand.Any(card => manusia.position - card.value >= 1);

        uiManager.TampilkanUI_PilihArahLangkah(canMoveForward, canMoveBackward);
    }

    private void MovePawnVisual(Player player, int targetPosition)
    {
        GameObject pawn = player.isAI ? pionAI : pionManusia;
        if (targetPosition > 0 && targetPosition <= petakPapan.Length)
        {
            Transform targetPetak = petakPapan[targetPosition - 1];
            pawn.transform.position = targetPetak.position + new Vector3(0, 100f, 0);
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
        roundNumber = 0;
        Start();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}