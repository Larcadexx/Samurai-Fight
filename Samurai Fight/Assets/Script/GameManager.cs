using UnityEngine;
using System.Linq;
using UnityEngine.UI; 
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

    [Header("Game Settings")]
    public int playerStartingPos = 1;
    public int aiStartingPos = 23;
    public int maxHandSize = 5;
    [Range(1, 5)] public int winningScore = 5;
    public float movementSpeed = 6f;

    private enum PlayerTurnState
    {
        None,
        MemilihKartuLangkah, 
        MemilihKartuSerang,  
        PerkuatSerangan,     
        MemilihKartuSergap,  
        MemilihKartuSerangBalik 
    }

    private enum PhaseSerangan
    {
        None,
        MenungguTangkisAI,       
        MenungguTangkisPemain,   
        PemainMemilihKartuTangkis 
    }

    public enum TieBreakerReason { PlayerCornered, DekEmpty }

    private PlayerTurnState playerState;
    private PhaseSerangan phaseSaatIni = PhaseSerangan.None;

    private Player player;
    private Player ai;
    private Player activePlayer;

    private bool isGameOver = false;
    private int roundNumber = 0;
    private int arahLangkah = 0;

    private Player pihakPenyerang; 
    private Player pihakPenangkis; 
    
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
        if (aiController != null) aiController.Initialize(this, uiManager);
        
        if (volumeSlider != null && TransisiScene.Instance != null)
        {
            volumeSlider.value = TransisiScene.Instance.GetMasterVolume();
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        SetupGame();
        if (AnimasiManager.Instance != null) AnimasiManager.Instance.ResetAllAnimations();
        StartRound();
    }

    public void OnVolumeChanged(float value)
    {
        if (TransisiScene.Instance != null) TransisiScene.Instance.SetMasterVolume(value);
    }

    void SetupGame()
    {
        player = new Player("Manusia", playerStartingPos);
        ai = new Player("AI", aiStartingPos, true);
        uiManager.ResetScoreUI();
    }

    public void StartRound()
    {
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();

        phaseSaatIni = PhaseSerangan.None;
        playerState = PlayerTurnState.None;
        IsSeranganBalik = false;
        isGameOver = false;
        roundNumber++;

        player.position = playerStartingPos;
        ai.position = aiStartingPos;
        MovePionVisual(player, player.position);
        MovePionVisual(ai, ai.position);

        DeckManager.Instance.SetupDek();
        
        player.hand.Clear();
        ai.hand.Clear();

        for (int i = 0; i < maxHandSize; i++)
        {
            player.hand.Add(DeckManager.Instance.DrawDek());
            ai.hand.Add(DeckManager.Instance.DrawDek());
        }

        uiManager.UpdatePlayerHandUI(player);
        uiManager.UpdateMainDeckUI(DeckManager.Instance.GetSisaDek());
        
        activePlayer = (roundNumber % 2 != 0) ? ai : player;

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

        if (activePlayer.isAI) StartCoroutine(aiController.ExecuteTurn(ai, player));
        else StartCoroutine(ShowPlayerTurnMessage());
    }

    public void EndTurn()
    {
        if (isGameOver) return;

        RefillHand(player);
        RefillHand(ai);

        if (DeckManager.Instance.IsDekEmpty() && (player.hand.Count < maxHandSize || ai.hand.Count < maxHandSize))
        {
            StartCoroutine(TieBreakerDelay(TieBreakerReason.DekEmpty));
            return;
        }

        activePlayer = (activePlayer == player) ? ai : player;
        StartTurn();
    }

    public void btnMelangkahPressed()
    {
        if (isPaused) return;
        
        uiManager.HideMessage(); 

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
        playerState = PlayerTurnState.PerkuatSerangan;
        uiManager.ShowPilihKartuAksi(ActionType.Perkuat);
    }

    public void btnPerkuatSeranganNo()
    {
        if (isPaused) return;
        
        uiManager.HideMessage();
        uiManager.HideAllPlayerPanels();
        
        InisiasiSerangan(player, ai, InisiasiKartuSerang.value, false, false);
    }

    public void btnSergapYes()
    {
        if (isPaused) return;
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
        playerState = PlayerTurnState.MemilihKartuSergap;
        uiManager.ShowPilihKartuAksi(ActionType.Sergap);
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
        if (phaseSaatIni != PhaseSerangan.MenungguTangkisPemain) return;
        
        phaseSaatIni = PhaseSerangan.PemainMemilihKartuTangkis;
        uiManager.ShowPilihKartuTangkis(attackValue);
        uiManager.SetupConfirmTangkisAction(btnConfirmTangkis);
    }

    public void btnTangkisNo()
    {
        if (isPaused) return;
        if (phaseSaatIni != PhaseSerangan.MenungguTangkisPemain) return;

        uiManager.HideAllPlayerPanels();
        phaseSaatIni = PhaseSerangan.None;

        uiManager.PlayCutscene(uiManager.clipPlayerTangkisGagal, () => 
        {
            StartCoroutine(RoundOverDelay(pihakPenyerang));
        });
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
                uiManager.PlayCutscene(uiManager.clipPlayerTangkisSukses, () => 
                {
                    uiManager.ShowMessage("Serang balik berhasil ditangkis!", 3.0f);
                    foreach (var entry in selectedTangkis)
                    {
                        player.hand.Remove(entry.Key);
                        entry.Value.HideSlot();
                    }
                    EndTurn();
                });
            }
            else if (hasCardLeftForCounter)
            {
                uiManager.PlayCutscene(uiManager.clipPlayerTangkisSukses, () => 
                {
                    foreach (var entry in selectedTangkis)
                    {
                        player.hand.Remove(entry.Key);
                        entry.Value.HideSlot();
                    }
                    StartCoroutine(ExecutePlayerSerangBalikCoroutine());
                });
            }
            else
            {
                uiManager.PlayCutscene(uiManager.clipPlayerTangkisGagal, () => 
                {
                    uiManager.ShowMessage("Tangkisan Tidak Sah! Wajib menyisakan 1 kartu untuk Serang Balik.", 3.5f);
                    StartCoroutine(RoundOverDelay(pihakPenyerang));
                });
            }
        }
        else
        {
            uiManager.PlayCutscene(uiManager.clipPlayerTangkisGagal, () => 
            {
                uiManager.ShowTangkisanGagalMessage(3.5f);
                StartCoroutine(RoundOverDelay(pihakPenyerang));
            });
        }

        foreach (var entry in selectedTangkis) entry.Value.ToggleSelection(false);
        selectedTangkis.Clear();
        phaseSaatIni = PhaseSerangan.None;
    }

    public void CardHandPressed(Card clickedCard, SistemKartu slotController)
    {
        if (isPaused) return;

        if (phaseSaatIni == PhaseSerangan.PemainMemilihKartuTangkis)
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
        pihakPenyerang = currentAttacker;
        pihakPenangkis = currentDefender;
        attackValue = value;
        IsSeranganBalik = isCounter;

        if (showAttackValueUI && currentAttacker == player) 
        {
            uiManager.ShowNilaiSerangan(attackValue);
        }
        else
        {
            uiManager.HideAttackStrength();
        }

        if (pihakPenangkis.isAI)
        {
            phaseSaatIni = PhaseSerangan.MenungguTangkisAI;
            aiController.HandleAIAttacked(ai, player, attackValue, isCounter);
        }
        else
        {
            if (CameraManager.Instance != null) CameraManager.Instance.SwitchToTangkis();
            phaseSaatIni = PhaseSerangan.MenungguTangkisPemain;
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
            while (player.hand.Count < maxHandSize)
            {
                Card newCard = DeckManager.Instance.DrawDek();
                if (newCard == null) return;
                player.hand.Add(newCard);
            }
        }
        else
        {
            foreach (SistemKartu slot in uiManager.cardSlots)
            {
                if (player.hand.Count >= maxHandSize) break;
                if (!slot.gameObject.activeSelf)
                {
                    Card newCard = DeckManager.Instance.DrawDek();
                    if (newCard == null) break;
                    player.hand.Add(newCard);
                    slot.Initialize(newCard);
                }
            }
        }
        uiManager.UpdateMainDeckUI(DeckManager.Instance.GetSisaDek());
    }

    private void CheckOpsiAwal()
    {
        bool canMove = player.hand.Any(card => (player.position + card.value < ai.position) || (player.position - card.value >= 1));
        int distance = ai.position - player.position;
        bool canAttack = player.hand.Any(card => card.value == distance);

        uiManager.ShowOpsiAwal(canMove, canAttack);

        if (!canMove && !canAttack) StartCoroutine(TieBreakerDelay(TieBreakerReason.PlayerCornered));
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
            pawn.transform.position = petakPapan[targetPosition - 1].position + new Vector3(0, 5f, 0);
        }
    }

    public IEnumerator MovePionStepByStep(Player player, int targetPosition)
    {
        GameObject pawn = player.isAI ? pionAI : pionManusia;
        if (targetPosition <= 0 || targetPosition > petakPapan.Length) yield break;

        int currentPos = player.position;
        int stepDirection = (targetPosition > currentPos) ? 1 : -1;
        float stepDuration = 1.15f; 

        AnimasiManager.Instance.SetWalking(player.isAI, true);

        while (currentPos != targetPosition)
        {
            int nextPos = currentPos + stepDirection;
            Vector3 startPos = petakPapan[currentPos - 1].position + new Vector3(0, 5f, 0);
            Vector3 endPos   = petakPapan[nextPos - 1].position + new Vector3(0, 5f, 0);
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / stepDuration;
                pawn.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            pawn.transform.position = endPos;
            currentPos = nextPos;
            player.position = currentPos;
            yield return null;
        }
        AnimasiManager.Instance.SetWalking(player.isAI, false);
    }

    private IEnumerator ShowRoundStartMessageAndBeginTurn()
    {
        yield return StartCoroutine(uiManager.ShowRondePanel(roundNumber, 2.0f));
        StartTurn();
    }

    private IEnumerator ShowPlayerTurnMessage()
    {
        uiManager.InfoPanel.SetActive(true);
        uiManager.ShowMessage("Giliran anda!!",2.0f);
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
        playerState = PlayerTurnState.MemilihKartuLangkah;
        uiManager.ShowPilihKartuAksi(ActionType.Melangkah);
    }

    private IEnumerator btnSerangPressedCoroutine()
    {
        if (activePlayer != player) yield break;
        yield return new WaitForSeconds(BUTTON_PRESS_DELAY);

        playerState = PlayerTurnState.MemilihKartuSerang;
        uiManager.ShowPilihKartuAksi(ActionType.Serang);
        
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
    }

    private IEnumerator HandleCardActionCoroutine(Card clickedCard, SistemKartu slotController)
    {
        PlayerTurnState stateSaatIni = playerState;
        playerState = PlayerTurnState.None;

        switch (stateSaatIni)
        {
            case PlayerTurnState.MemilihKartuSerang:
                int distance = ai.position - player.position;
                if (clickedCard.value == distance)
                {
                    InisiasiKartuSerang = clickedCard;
                    player.hand.Remove(clickedCard);
                    slotController.HideSlot();
                    
                    if (player.hand.Count > 0) uiManager.ShowPerkuatSerangan();
                    else InisiasiSerangan(player, ai, InisiasiKartuSerang.value, false);
                }
                else
                {
                    uiManager.ShowMessage("kartu tidak sesuai! giliran ai", 2.0f);
                    yield return new WaitForSeconds(2.0f); 
                    EndTurn();
                }
                break;

            case PlayerTurnState.PerkuatSerangan:
                int totalAttackValue = InisiasiKartuSerang.value + clickedCard.value;
                player.hand.Remove(clickedCard);
                slotController.HideSlot();
                
                uiManager.ShowNilaiSerangan(totalAttackValue);
                yield return new WaitForSeconds(0.7f);
                uiManager.HideAttackStrength();

                InisiasiSerangan(player, ai, totalAttackValue, false, false);
                break;

            case PlayerTurnState.MemilihKartuLangkah:
                yield return StartCoroutine(HandleMoveCoroutine(clickedCard, slotController));
                break;

            case PlayerTurnState.MemilihKartuSergap:
                HandleSergap(clickedCard, slotController);
                break;

            case PlayerTurnState.MemilihKartuSerangBalik:
                player.hand.Remove(clickedCard);
                slotController.HideSlot();
                InisiasiSerangan(player, ai, clickedCard.value, true, false);
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
            else EndTurn();
        }
        else StartTurn();
    }

    private IEnumerator ExecutePlayerSerangBalikCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToAttack();
        playerState = PlayerTurnState.MemilihKartuSerangBalik;
        uiManager.ShowPilihKartuAksi(ActionType.SerangBalik);
    }

    public IEnumerator RoundOverDelay(Player winner)
    {
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
        uiManager.HideAllUIsForRoundEnd();

        if (winner.score < (winningScore - 1)) uiManager.ShowMessage($"{winner.playerName} memenangkan Ronde Ke-{roundNumber}!", 3.0f);

        winner.score++;
        uiManager.UpdateScoreUI(winner);
        yield return new WaitForSeconds(3.0f);

        if (winner.score >= winningScore)
        {
            isGameOver = true;
            yield return new WaitForSeconds(0.3f);
            string winnerName = winner.isAI ? "AI" : "Pemain";
            yield return StartCoroutine(uiManager.ShowGameWIN(winnerName, 2f));

            string nextScene = (winner == player) ? "SceneWIN" : "SceneLOSE";
            
            if (TransisiScene.Instance != null) 
                TransisiScene.Instance.LoadSceneTransisi(nextScene);
            else 
                SceneManager.LoadScene(nextScene);
        }
        else StartRound();
    }

    public IEnumerator TieBreakerDelay(TieBreakerReason reason)
    {
        uiManager.HideAllUIsForRoundEnd();
        string message = "";
        switch (reason)
        {
            case TieBreakerReason.PlayerCornered: message = "Pemain Terpojok! TIE BREAKER diaktifkan."; break;
            case TieBreakerReason.DekEmpty: message = "Deck Kartu Habis! TIE BREAKER diaktifkan."; break;
        }
        uiManager.ShowMessage(message, 3.5f);
        yield return new WaitForSeconds(3.5f);

        int totalManusia = player.hand.Sum(card => card.value);
        int totalAI = ai.hand.Sum(card => card.value);
        Player winner = (totalManusia > totalAI) ? player : (totalAI > totalManusia ? ai : null);

        if (winner != null) StartCoroutine(RoundOverDelay(winner));
        else
        {
            uiManager.ShowMessage("Hasil Tie Breaker seri!", 3.0f);
            yield return new WaitForSeconds(3.0f);
            StartRound();
        }
    }

    public void PauseGame()
    {
        if (isGameOver || isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        uiManager.ShowPausePanel();
        uiManager.SetPlayerHandInteractable(false);
        if (TransisiScene.Instance != null && TransisiScene.Instance.bgmSource != null) TransisiScene.Instance.bgmSource.Pause();
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        uiManager.HidePausePanel();
        if (TransisiScene.Instance != null && TransisiScene.Instance.bgmSource != null) TransisiScene.Instance.bgmSource.UnPause();
        bool shouldBeInteractable = playerState != PlayerTurnState.None || phaseSaatIni == PhaseSerangan.PemainMemilihKartuTangkis;
        uiManager.SetPlayerHandInteractable(shouldBeInteractable);
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        if (TransisiScene.Instance != null)
        {
            TransisiScene.Instance.LoadSceneTransisi(SceneManager.GetActiveScene().name);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (TransisiScene.Instance != null)
        {
            TransisiScene.Instance.LoadSceneTransisi("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}