using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public UIManager uiManager;
    public AIController aiController;

    [Header("Volume Slider")]
    public Slider volumeSlider; 

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

    private PlayerTurnState playerState;
    private AttackPhase PhaseSerangan = AttackPhase.None;

    private Player player;
    private Player ai;
    private Player activePlayer;

    private bool isGameOver = false;
    private int roundNumber = 0;
    private int arahLangkah = 0;

    private Player serangan;
    private Player tangkisan;
    
    private int attackValue;
    
    private Card InisiasiKartuSerang;
    private List<KeyValuePair<Card, SistemKartu>> selectedTangkis = new List<KeyValuePair<Card, SistemKartu>>();
    private bool IsSeranganBalik = false;
    
    private bool isPaused = false;
    private const float BUTTON_PRESS_DELAY = 0.3f;

    void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        if (aiController == null)
        {
            Debug.LogError("AIController belum di-assign di Inspector GameManager!");
        }
        else
        {
            aiController.Initialize(this, uiManager);
        }

        if (volumeSlider != null)
        {
            if (TransisiScene.Instance != null)
            {
                volumeSlider.value = TransisiScene.Instance.GetMasterVolume();
            }

            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        SetupGame();

        if (AnimasiManager.Instance != null)
        {
            AnimasiManager.Instance.ResetAllAnimations();
        }

        StartRound();
    }

    public void OnVolumeChanged(float value)
    {
        if (TransisiScene.Instance != null)
        {
            TransisiScene.Instance.SetMasterVolume(value);
        }
    }

    void SetupGame()
    {
        player = new Player("Manusia", 1);
        ai = new Player("AI", 23, true);
        uiManager.ResetScoreUI();
    }

    public void StartRound()
    {
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();

        PhaseSerangan = AttackPhase.None;
        playerState = PlayerTurnState.None;
        IsSeranganBalik = false;
        isGameOver = false;
        
        roundNumber++;

        player.position = 1;
        ai.position = 23;
        MovePionVisual(player, player.position);
        MovePionVisual(ai, ai.position);

        DeckManager.Instance.SetupDeck();
        
        player.hand.Clear();
        ai.hand.Clear();

        for (int i = 0; i < 5; i++)
        {
            player.hand.Add(DeckManager.Instance.AcakCard());
            ai.hand.Add(DeckManager.Instance.AcakCard());
        }

        uiManager.UpdatePlayerHandUI(player);
        uiManager.UpdateMainDeckUI(DeckManager.Instance.DeckCount());

        if (roundNumber % 2 != 0)
        {
            activePlayer = ai;
        }
        else
        {
            activePlayer = player;
        }

        StartCoroutine(ShowRoundStartMessageAndBeginTurn());
    }

    private void StartTurn()
    {
        if (isGameOver) return;

        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
        playerState = PlayerTurnState.None;
        uiManager.RestoreDefaultLayout();

        if (AnimasiManager.Instance != null)
        {
            AnimasiManager.Instance.SetWalking(false, false); 
            AnimasiManager.Instance.SetWalking(true, false); 
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

        if (DeckManager.Instance.IsDeckEmpty() && (player.hand.Count < 5 || ai.hand.Count < 5))
        {
            StartCoroutine(TieBreakerDelay(TieBreakerReason.DeckEmpty));
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

        if (TransisiScene.Instance != null && TransisiScene.Instance.bgmSource != null)
        {
            TransisiScene.Instance.bgmSource.Pause();
        }
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;
        uiManager.HidePausePanel();

        if (TransisiScene.Instance != null && TransisiScene.Instance.bgmSource != null)
        {
            TransisiScene.Instance.bgmSource.UnPause();
        }

        bool shouldBeInteractable =
            playerState != PlayerTurnState.None ||
            PhaseSerangan == AttackPhase.PlayerSelectingTangkisCards;

        uiManager.SetPlayerHandInteractable(shouldBeInteractable);
    }


    public void btnMelangkahPressed()
    {
        if (isPaused) return;
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToMelangkah();
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
        InisiasiSerangan(player, ai, InisiasiKartuSerang.value, false);
    }

    public void btnSergapYes()
    {
        if (isPaused) return;
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
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
        if (PhaseSerangan != AttackPhase.AwaitingTangkisPlayer) return;
        
        PhaseSerangan = AttackPhase.PlayerSelectingTangkisCards;
        uiManager.ShowPilihKartuTangkis(attackValue);
        uiManager.KonfirmasiTangkisButton.onClick.RemoveAllListeners();
        uiManager.KonfirmasiTangkisButton.onClick.AddListener(btnConfirmTangkis);
    }

    public void btnTangkisNo()
    {
        if (isPaused) return;
        if (PhaseSerangan != AttackPhase.AwaitingTangkisPlayer) return;
        
        uiManager.HideAllPlayerPanels();
        PhaseSerangan = AttackPhase.None;
        StartCoroutine(RoundOverDelay(serangan));
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
            if (IsSeranganBalik)
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
                StartCoroutine(RoundOverDelay(serangan));
            }
        }
        else
        {
            uiManager.ShowTangkisanGagalMessage(3.5f);
            StartCoroutine(RoundOverDelay(serangan));
        }

        foreach (var entry in selectedTangkis) entry.Value.ToggleSelection(false);
        selectedTangkis.Clear();
        PhaseSerangan = AttackPhase.None;
    }

    public void CardHandPressed(Card clickedCard, SistemKartu slotController)
    {
        if (isPaused) return;

        if (PhaseSerangan == AttackPhase.PlayerSelectingTangkisCards)
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

            if (CameraManager.Instance != null) CameraManager.Instance.SwitchToAttack();

            InisiasiSerangan(player, ai, clickedCard.value, false, false);
        }
        else
        {
            uiManager.ShowMessage("Sergap GAGAL!! Nilai tidak sesuai", 2.5f);
            EndTurn();
        }
    }

    public void InisiasiSerangan(Player currentAttacker, Player currentDefender, int value, bool isCounter, bool showAttackValueUI = true)
    {
        serangan = currentAttacker;
        tangkisan = currentDefender;
        attackValue = value;
        IsSeranganBalik = isCounter;

        if (showAttackValueUI)
        {
            uiManager.ShowNilaiSerangan(attackValue);
        }

        if (tangkisan.isAI)
        {
            PhaseSerangan = AttackPhase.AwaitingTangkis;
            aiController.HandleAIAttacked(ai, player, attackValue, isCounter);
        }
        else
        {
            if (CameraManager.Instance != null) CameraManager.Instance.SwitchToTangkis();
            PhaseSerangan = AttackPhase.AwaitingTangkisPlayer;
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
        uiManager.UpdateTotalNilaiTangkisan(currentTangkisTotal, attackValue);
    }

    private void RefillHand(Player player)
    {
        if (player.isAI)
        {
            while (player.hand.Count < 5)
            {
                Card newCard = DeckManager.Instance.AcakCard();
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
                    Card newCard = DeckManager.Instance.AcakCard();
                    if (newCard == null) break;

                    player.hand.Add(newCard);
                    slot.Initialize(newCard);
                }
            }
        }

        uiManager.UpdateMainDeckUI(DeckManager.Instance.DeckCount());
    }


    private void CheckOpsiAwal()
    {
        bool canMove = player.hand.Any(card => (player.position + card.value < ai.position) || (player.position - card.value >= 1));
        int distance = ai.position - player.position;
        bool canAttack = player.hand.Any(card => card.value == distance);

        uiManager.ShowOpsiAwal(canMove, canAttack);

        if (!canMove && !canAttack)
        {
            StartCoroutine(TieBreakerDelay(TieBreakerReason.PlayerCornered));
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

    public IEnumerator MovePionStepByStep(Player player, int targetPosition)
    {
        GameObject pawn = player.isAI ? pionAI : pionManusia;
        int currentPos = player.position;
        int step = (targetPosition > currentPos) ? 1 : -1;

        if (AnimasiManager.Instance != null)
        {
            AnimasiManager.Instance.SetWalking(player.isAI, true);
        }

        const float moveDuration = 1f;
        float speed = 1.0f / moveDuration;

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
                    t += Time.deltaTime * speed;
                    pawn.transform.position = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }

                pawn.transform.position = endPos;
            }
        }

        player.position = targetPosition;

        if (AnimasiManager.Instance != null)
        {
            AnimasiManager.Instance.SetWalking(player.isAI, false);
        }
    }

    public void PlayAgain()
    {
        if (TransisiScene.Instance != null)
            TransisiScene.Instance.PindahKeScene(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        if (TransisiScene.Instance != null)
            TransisiScene.Instance.PindahKeScene("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator ShowRoundStartMessageAndBeginTurn()
    {
        yield return StartCoroutine(uiManager.ShowRondePanel(roundNumber, 2.0f));
        StartTurn();
    }

    private IEnumerator ShowPlayerTurnMessage()
    {
        uiManager.InfoPanel.SetActive(true);
        uiManager.ShowMessage("Giliran anda!!", 1.5f);
        yield return new WaitForSeconds(1.5f);
        CheckOpsiAwal();
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
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToAttack();
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
                    InisiasiKartuSerang = clickedCard;
                    player.hand.Remove(clickedCard);
                    slotController.HideSlot();
                    if (player.hand.Count > 0)
                    {
                        uiManager.ShowPerkuatSerangan();
                    }
                    else
                    {
                        InisiasiSerangan(player, ai, InisiasiKartuSerang.value, false);
                    }
                }
                else
                {
                    StartTurn();
                }
                break;

            case PlayerTurnState.StrengtheningAttack:
                int totalAttackValue = InisiasiKartuSerang.value + clickedCard.value;
                player.hand.Remove(clickedCard);
                slotController.HideSlot();
                InisiasiSerangan(player, ai, totalAttackValue, false);
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
                InisiasiSerangan(player, ai, clickedCard.value, true);
                break;
        }
    }

    private IEnumerator HandleMoveCoroutine(Card clickedCard, SistemKartu slotController)
    {
        int newPosition = player.position + (clickedCard.value * arahLangkah);
        bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);

        if (isValidMove)
        {
            player.hand.Remove(clickedCard);
            slotController.HideSlot();
            if (CameraManager.Instance != null) CameraManager.Instance.SwitchToBergerak();
            yield return new WaitForSeconds(1f);
            yield return StartCoroutine(MovePionStepByStep(player, newPosition));

            if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();

            int newDistance = ai.position - player.position;
            bool canSergap = player.hand.Any(card => card.value == newDistance);
            if (canSergap)
            {
                if (CameraManager.Instance != null) CameraManager.Instance.SwitchToMelangkah();
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
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
        playerState = PlayerTurnState.SelectingCounterCard;
        uiManager.ShowPilihKartuAksi("serangbalik");
    }

    public IEnumerator RoundOverDelay(Player winner)
    {
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
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
            yield return StartCoroutine(uiManager.ShowGameWIN(winnerName, 2f));

            string nextScene = (winner == player) ? "SceneWIN" : "SceneLOSE";

            if (TransisiScene.Instance != null)
            {
                TransisiScene.Instance.PindahKeScene(nextScene);
            }
            else
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(nextScene);
            }
        }
        else
        {
            StartRound();
        }
    }

    public IEnumerator TieBreakerDelay(TieBreakerReason reason)
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