using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public UIManager uiManager;
    public AIController aiController;

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

    private enum AttackPhase
    {
        None,
        AwaitingTangkis,
        AwaitingSerangBalik,
        AwaitingTangkisPlayer,
        PlayerSelectingTangkisCards
    }

    public enum TieBreakerReason
    {
        PlayerCornered,
        DeckEmpty
    }

    private Animator pionManusiaAnimator;


    private PlayerTurnState playerState;
    private AttackPhase currentAttackPhase = AttackPhase.None;

    private Player player;
    private Player ai;
    private List<Card> deck = new List<Card>();
    private Player activePlayer;

    private bool isGameOver = false;
    private int roundNumber = 0;
    private int arahLangkah = 0;

    private Player attacker;
    private Player defender;
    private int attackValue;
    private Card initialAttackCard;
    private List<KeyValuePair<Card, SistemKartu>> selectedTangkis = new List<KeyValuePair<Card, SistemKartu>>();

    private bool isCurrentAttackACounter = false;
    private bool isPaused = false;
    private AudioSource bgMusic; 
    private const float BUTTON_PRESS_DELAY = 0.3f;

    void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }

        bgMusic = GetComponent<AudioSource>(); 
    }

    void Start()
    {
        if (pionManusiaAnimator != null)
{
    pionManusiaAnimator.Rebind();
    pionManusiaAnimator.Update(0f);
    pionManusiaAnimator.SetBool("isWalking", false);
}

        pionManusiaAnimator = pionManusia.GetComponent<Animator>();

        if (aiController == null)
        {
            Debug.LogError("AIController belum di-assign di Inspector GameManager!");
        }
        else
        {
            aiController.Initialize(this, uiManager);
        }

        SetupGame();
        if (pionManusiaAnimator == null)
{
    pionManusiaAnimator = pionManusia.GetComponent<Animator>();
}

// Pastikan animasi dalam keadaan Idle saat game mulai
if (pionManusiaAnimator != null)
{
    pionManusiaAnimator.SetBool("isWalking", false);
}

        StartRound();
    }

    void SetupGame()
    {
        player = new Player("Manusia", 1);
        ai = new Player("AI", 23, true);
        uiManager.ResetScoreUI();
    }

    public void StartRound()
    {
        CameraManager.Instance.SwitchToDefault();

        currentAttackPhase = AttackPhase.None;
        playerState = PlayerTurnState.None;
        isCurrentAttackACounter = false;
        isGameOver = false;
        roundNumber++;

        player.position = 1;
        ai.position = 23;
        MovePionVisual(player, player.position);
        MovePionVisual(ai, ai.position);

        CreateDeck();
        ShuffleDeck();
        player.hand.Clear();
        ai.hand.Clear();

        for (int i = 0; i < 5; i++)
        {
            player.hand.Add(DrawCardFromDeck());
            ai.hand.Add(DrawCardFromDeck());
        }

        uiManager.UpdatePlayerHandUI(player);
        uiManager.UpdateMainDeckUI(deck.Count);

        activePlayer = ai;
        StartCoroutine(ShowRoundStartMessageAndBeginTurn());
    }

    private void StartTurn()
{
    if (isGameOver) return;

    CameraManager.Instance.SwitchToDefault();
    playerState = PlayerTurnState.None;
    uiManager.RestoreDefaultLayout();

    // Jika giliran AI, pastikan karakter manusia Idle
    if (pionManusiaAnimator != null)
    {
        pionManusiaAnimator.SetBool("isWalking", false);
    }

    if (activePlayer.isAI)
    {
        StartCoroutine(aiController.ExecuteTurn(ai, player));
    }
    else
    {
        StartCoroutine(ShowPlayerTurnMessage());
    }
}


    public void EndTurn()
    {
        if (isGameOver) return;

        RefillHand(player);
        RefillHand(ai);

        if (deck.Count == 0 && (player.hand.Count < 5 || ai.hand.Count < 5))
        {
            StartCoroutine(ExecuteTieBreakerDelay(TieBreakerReason.DeckEmpty));
            return;
        }

        activePlayer = (activePlayer == player) ? ai : player;
        StartTurn();
    }
    
    public void PauseGame()
    {
        if (isGameOver || isPaused) return;

        isPaused = true;
        Time.timeScale = 0f; 
        uiManager.ShowPausePanel();
        uiManager.SetPlayerHandInteractable(false);

        if (bgMusic != null) 
        {
            bgMusic.Pause(); 
        }
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;
        uiManager.HidePausePanel();
        
        if (bgMusic != null) 
        {
            bgMusic.UnPause(); 
        }

        bool shouldBeInteractable = 
            playerState != PlayerTurnState.None || 
            currentAttackPhase == AttackPhase.PlayerSelectingTangkisCards;
            
        uiManager.SetPlayerHandInteractable(shouldBeInteractable);
    }


    public void btnMelangkahPressed()
    {
        if (isPaused) return; 
        CameraManager.Instance.SwitchToMelangkah(); 
        StartCoroutine(btnMelangkahPressedCoroutine());
    }

    public void btnArahLangkahPressed(bool isMaju)
    {
        if (isPaused) return; 
        StartCoroutine(btnArahLangkahPressedCoroutine(isMaju));
    }

    public void btnSerangPressed()
    {
        if (isPaused) return; 
        StartCoroutine(btnSerangPressedCoroutine());
    }

    public void btnPerkuatSeranganYes()
    {
        if (isPaused) return; 
        playerState = PlayerTurnState.StrengtheningAttack;
        uiManager.ShowPilihKartuAksi("perkuat");
    }

    public void btnPerkuatSeranganNo()
    {
        if (isPaused) return; 
        uiManager.HideAllPlayerPanels();
        InitiateAttack(player, ai, initialAttackCard.value, false);
    }

    public void btnSergapYes()
    {
        if (isPaused) return; 
        CameraManager.Instance.SwitchToDefault();
        playerState = PlayerTurnState.SelectingSergapCard;
        uiManager.ShowPilihKartuAksi("sergap");
    }

    public void btnSergapNo()
    {
        if (isPaused) return; 
        uiManager.HideAllPlayerPanels();
        EndTurn();
    }

    public void btnTangkisYes()
    {
        if (isPaused) return; 
        if (currentAttackPhase != AttackPhase.AwaitingTangkisPlayer) return;
        currentAttackPhase = AttackPhase.PlayerSelectingTangkisCards;
        uiManager.ShowPilihKartuTangkis(attackValue);
        uiManager.KonfirmasiTangkisButton.onClick.RemoveAllListeners();
        uiManager.KonfirmasiTangkisButton.onClick.AddListener(btnConfirmTangkis);
    }

    public void btnTangkisNo()
    {
        if (isPaused) return; 
        if (currentAttackPhase != AttackPhase.AwaitingTangkisPlayer) return;
        uiManager.HideAllPlayerPanels();
        currentAttackPhase = AttackPhase.None;
        StartCoroutine(RoundOverDelay(attacker));
    }

    public void btnConfirmTangkis()
    {
        if (isPaused) return; 
        uiManager.HideConfirmTangkisbtn();
        uiManager.SetPlayerHandInteractable(false);
        uiManager.HideMessage();

        int totalTangkisValue = selectedTangkis.Sum(entry => entry.Key.value);
        bool isValueMatch = totalTangkisValue == attackValue;
        bool hasCardLeftForCounter = player.hand.Count - selectedTangkis.Count >= 1;

        if (isValueMatch)
        {
            if (isCurrentAttackACounter)
            {
                uiManager.ShowMessage("Serang balik berhasil ditangkis!", 3.0f);
                foreach (var entry in selectedTangkis)
                {
                    player.hand.Remove(entry.Key);
                    entry.Value.HideSlot();
                }
                EndTurn();
            }
            else if (hasCardLeftForCounter)
            {
                uiManager.ShowMessage("Tangkisan berhasil! Siapkan Serang Balik.", 2.5f);
                foreach (var entry in selectedTangkis)
                {
                    player.hand.Remove(entry.Key);
                    entry.Value.HideSlot();
                }
                StartCoroutine(ExecutePlayerSerangBalikCoroutine());
            }
            else
            {
                uiManager.ShowMessage("Tangkisan Tidak Sah! Wajib menyisakan 1 kartu untuk Serang Balik.", 3.5f);
                StartCoroutine(RoundOverDelay(attacker));
            }
        }
        else
        {
            uiManager.ShowTangkisanGagalMessage(3.5f);
            StartCoroutine(RoundOverDelay(attacker));
        }

        foreach (var entry in selectedTangkis) entry.Value.ToggleSelection(false);
        selectedTangkis.Clear();
        currentAttackPhase = AttackPhase.None;
    }

    public void CardSlotClicked(Card clickedCard, SistemKartu slotController)
    {
        if (isPaused) return; 

        if (currentAttackPhase == AttackPhase.PlayerSelectingTangkisCards)
        {
            HandleTangkisSelection(clickedCard, slotController);
            return;
        }

        if (playerState == PlayerTurnState.None) return;

        uiManager.HideCardPanel();
        uiManager.SetPlayerHandInteractable(false);
        uiManager.HideMessage();

        StartCoroutine(HandleCardActionCoroutine(clickedCard, slotController));
    }

    private void HandleSergap(Card clickedCard, SistemKartu slotController)
    {
        int distance = ai.position - player.position;
        if (clickedCard.value == distance)
        {
            player.hand.Remove(clickedCard);
            slotController.HideSlot();

            CameraManager.Instance.SwitchToAttack();

            InitiateAttack(player, ai, clickedCard.value, false, false);
        }
        else
        {
            uiManager.ShowMessage("Sergap GAGAL!! Nilai tidak sesuai", 2.5f);
            EndTurn();
        }
    }

    public void InitiateAttack(Player currentAttacker, Player currentDefender, int value, bool isCounter, bool showAttackValueUI = true)
    {
        attacker = currentAttacker;
        defender = currentDefender;
        attackValue = value;
        isCurrentAttackACounter = isCounter;

        if (showAttackValueUI)
        {
            uiManager.ShowAttackStrength(attackValue);
        }

        if (defender.isAI)
        {
            currentAttackPhase = AttackPhase.AwaitingTangkis;
            aiController.HandleAIAttacked(ai, player, attackValue, isCounter);
        }
        else
        {
            CameraManager.Instance.SwitchToTangkis();
            currentAttackPhase = AttackPhase.AwaitingTangkisPlayer;
            StartCoroutine(uiManager.ShowTangkis(value, isCounter));
        }
    }

    private void HandleTangkisSelection(Card clickedCard, SistemKartu slotController)
    {
        var cardEntry = new KeyValuePair<Card, SistemKartu>(clickedCard, slotController);
        if (selectedTangkis.Any(p => p.Value == slotController))
        {
            selectedTangkis.RemoveAll(p => p.Value == slotController);
            slotController.ToggleSelection(false);
        }
        else
        {
            selectedTangkis.Add(cardEntry);
            slotController.ToggleSelection(true);
        }
        int currentTangkisTotal = selectedTangkis.Sum(entry => entry.Key.value);
        uiManager.UpdateSelectedTangkisTotal(currentTangkisTotal, attackValue);
    }

    private void RefillHand(Player player)
    {
        if (player.isAI)
        {
            while (player.hand.Count < 5)
            {
                Card newCard = DrawCardFromDeck();
                if (newCard == null) return;
                player.hand.Add(newCard);
            }
        }
        else
        {
            foreach (SistemKartu slot in uiManager.cardSlots)
            {
                if (player.hand.Count >= 5) break;

                if (!slot.gameObject.activeSelf)
                {
                    Card newCard = DrawCardFromDeck();
                    if (newCard == null) break;

                    player.hand.Add(newCard);
                    slot.Initialize(newCard);
                }
            }
        }

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

    private void CheckAvailablePlayerActions()
    {
        bool canMove = player.hand.Any(card => (player.position + card.value < ai.position) || (player.position - card.value >= 1));
        int distance = ai.position - player.position;
        bool canAttack = player.hand.Any(card => card.value == distance);

        uiManager.ShowOpsiAwal(canMove, canAttack);

        if (!canMove && !canAttack)
        {
            StartCoroutine(ExecuteTieBreakerDelay(TieBreakerReason.PlayerCornered));
        }
    }

    private void CheckAvailableMoveDirections()
    {
        bool canMoveForward = player.hand.Any(card => player.position + card.value < ai.position);
        bool canMoveBackward = player.hand.Any(card => player.position - card.value >= 1);

        StartCoroutine(uiManager.ShowArahLangkah(canMoveForward, canMoveBackward));
    }



    public void MovePionVisual(Player player, int targetPosition)
{
    GameObject pawn = player.isAI ? pionAI : pionManusia;
    if (targetPosition > 0 && targetPosition <= petakPapan.Length)
    {
        Transform targetPetak = petakPapan[targetPosition - 1];
        pawn.transform.position = targetPetak.position + new Vector3(0, 5f, 0);
    }
}


private IEnumerator MovePionStepByStep(Player player, int targetPosition)
{
    GameObject pawn = player.isAI ? pionAI : pionManusia;
    int currentPos = player.position;
    int step = (targetPosition > currentPos) ? 1 : -1;

    // Aktifkan animasi jalan untuk pion manusia
    if (!player.isAI && pionManusiaAnimator != null)
    {
        pionManusiaAnimator.SetBool("isWalking", true);
    }

    // --- PERUBAHAN DI SINI ---

    // 1. Durasi 1 petak = durasi 1 siklus animasi "Walk"
    //    (155 - 120) / 24 = 35 / 24 = ~1.458 detik
    const float moveDuration = 35f / 24f; // Menghasilkan ~1.458f

    // 2. Hitung 'speed' berdasarkan 'moveDuration'
    //    Rumus: speed = 1.0f / durasi_yang_diinginkan
    float speed = 1.0f / moveDuration; // Ini akan menjadi 1.0f / 1.458f ≈ 0.6857f

    // --- SELESAI PERUBAHAN ---


    // Gerak per petak
    for (int pos = currentPos; pos != targetPosition; pos += step)
    {
        int nextPos = pos + step;

        if (nextPos > 0 && nextPos <= petakPapan.Length)
        {
            Vector3 startPos = petakPapan[pos - 1].position + new Vector3(0, 5f, 0);
            Vector3 endPos = petakPapan[nextPos - 1].position + new Vector3(0, 5f, 0);
            float t = 0f;
            
            while (t < 1f)
            {
                // 't' akan bertambah dari 0 ke 1 selama 1.458 detik
                t += Time.deltaTime * speed; 
                pawn.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            pawn.transform.position = endPos; 
        }
    }

    player.position = targetPosition;

    // Matikan animasi jalan
    if (!player.isAI && pionManusiaAnimator != null)
    {
        pionManusiaAnimator.SetBool("isWalking", false);
    }
}


    public void PlayAgain()
    {
        Time.timeScale = 1f; 
        isPaused = false;    
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; 
        isPaused = false;    
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator ShowRoundStartMessageAndBeginTurn()
    {
        yield return StartCoroutine(uiManager.ShowRoundStartPanel(roundNumber, 2.0f));
        StartTurn();
    }

    private IEnumerator ShowPlayerTurnMessage()
    {
        uiManager.InfoPanel.SetActive(true);
        uiManager.ShowMessage("Giliran anda!!", 2.0f);
        yield return new WaitForSeconds(2.0f);
        CheckAvailablePlayerActions();
    }

    private IEnumerator btnMelangkahPressedCoroutine()
    {
        if (activePlayer != player) yield break;
        yield return new WaitForSeconds(BUTTON_PRESS_DELAY);
        CheckAvailableMoveDirections();
    }

    private IEnumerator btnArahLangkahPressedCoroutine(bool isMaju)
    {
        if (activePlayer != player) yield break;
        yield return new WaitForSeconds(BUTTON_PRESS_DELAY);

        arahLangkah = isMaju ? 1 : -1;
        playerState = PlayerTurnState.SelectingMoveCard;
        uiManager.ShowPilihKartuAksi("melangkah");

    }

    private IEnumerator btnSerangPressedCoroutine()
    {
        if (activePlayer != player) yield break;
        yield return new WaitForSeconds(BUTTON_PRESS_DELAY);

        playerState = PlayerTurnState.SelectingAttackCard;
        uiManager.ShowPilihKartuAksi("serang");
        CameraManager.Instance.SwitchToAttack(); 
    }

    private IEnumerator HandleCardActionCoroutine(Card clickedCard, SistemKartu slotController)
    {
        PlayerTurnState stateSaatIni = playerState;
        playerState = PlayerTurnState.None;

        switch (stateSaatIni)
        {
            case PlayerTurnState.SelectingAttackCard:
                int distance = ai.position - player.position;
                if (clickedCard.value == distance)
                {
                    initialAttackCard = clickedCard;
                    player.hand.Remove(clickedCard);
                    slotController.HideSlot();
                    if (player.hand.Count > 0)
                    {
                        uiManager.ShowPerkuatSerangan();
                    }
                    else
                    {
                        InitiateAttack(player, ai, initialAttackCard.value, false);
                    }
                }
                else
                {
                    StartTurn();
                }
                break;

            case PlayerTurnState.StrengtheningAttack:
                int totalAttackValue = initialAttackCard.value + clickedCard.value;
                player.hand.Remove(clickedCard);
                slotController.HideSlot();
                InitiateAttack(player, ai, totalAttackValue, false);
                break;

            case PlayerTurnState.SelectingMoveCard:
                yield return StartCoroutine(HandleMoveCoroutine(clickedCard, slotController));
                break;

            case PlayerTurnState.SelectingSergapCard:
                HandleSergap(clickedCard, slotController);
                break;

            case PlayerTurnState.SelectingCounterCard:
                player.hand.Remove(clickedCard);
                slotController.HideSlot();
                InitiateAttack(player, ai, clickedCard.value, true);
                break;
        }
    }

    private IEnumerator HandleMoveCoroutine(Card clickedCard, SistemKartu slotController)
{
    int newPosition = player.position + (clickedCard.value * arahLangkah);
    bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);

    if (isValidMove)
    {
        // Hapus kartu dari tangan lebih awal
        player.hand.Remove(clickedCard);
        slotController.HideSlot();

        // Aktifkan kamera bergerak
        CameraManager.Instance.SwitchToBergerak();

        // Jalankan animasi per-petak + animasi jalan
        yield return StartCoroutine(MovePionStepByStep(player, newPosition));

        // Setelah animasi selesai, pastikan kamera kembali ke default
        CameraManager.Instance.SwitchToDefault();

        // Hitung ulang jarak dan periksa peluang sergap
        int newDistance = ai.position - player.position;
        bool canSergap = player.hand.Any(card => card.value == newDistance);
        if (canSergap)
        {
            CameraManager.Instance.SwitchToMelangkah();
            StartCoroutine(uiManager.ShowSergap());
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


    private IEnumerator ExecutePlayerSerangBalikCoroutine()
    {
        yield return new WaitForSeconds(2.5f);
        CameraManager.Instance.SwitchToDefault();
        playerState = PlayerTurnState.SelectingCounterCard;
        uiManager.ShowPilihKartuAksi("serangbalik");
    }

    public IEnumerator RoundOverDelay(Player winner)
    {
        CameraManager.Instance.SwitchToDefault();
        uiManager.HideAllUIsForRoundEnd();

        if (winner.score < 4)
        {
            uiManager.ShowMessage($"{winner.playerName} memenangkan Ronde Ke-{roundNumber}!", 3.0f);
        }

        winner.score++;
        uiManager.UpdateScoreUI(winner);
        yield return new WaitForSeconds(3.0f);

        if (winner.score >= 5)
        {
            isGameOver = true;
            yield return new WaitForSeconds(0.3f);

            string winnerName = winner.isAI ? "AI" : "Pemain";
            yield return StartCoroutine(uiManager.ShowGameWinRoundText(winnerName, 2f));

            uiManager.ShowKemenanganPanel(winner == player);
        }
        else
        {
            StartRound();
        }
    }

    public IEnumerator ExecuteTieBreakerDelay(TieBreakerReason reason)
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

        int totalManusia = player.hand.Sum(card => card.value);
        int totalAI = ai.hand.Sum(card => card.value);

        Player winner = null;
        if (totalManusia > totalAI) { winner = player; }
        else if (totalAI > totalManusia) { winner = ai; }

        if (winner != null)
        {
            StartCoroutine(RoundOverDelay(winner));
        }
                else
        {
            uiManager.ShowMessage("Hasil Tie Breaker seri!", 3.0f);
            yield return new WaitForSeconds(3.0f);
            StartRound();
        }
    }
}