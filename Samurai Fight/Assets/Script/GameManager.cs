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
    public Slider sliderVolume; 

    [Header("Referensi Objek di Scene")]
    public GameObject pionManusia;
    public GameObject pionAI;
    public Transform[] petakPapan;

    [Header("Game Settings")]
    public int posisiAwalPlayer = 1;
    public int posisiAwalAI = 23;
    public int maxUkuranKartu = 5;
    [Range(1, 5)] public int skorMenang = 5;
    public float kecepatanGerak = 6f;

    private enum StateGiliranPlayer
    {
        None,
        SelectingKartuMelangkah, 
        SelectingKartuSerang,  
        PerkuatSerangan,     
        SelectingKartuSergap,  
        SelectingKartuSerangBalik 
    }

    private enum AttackPhase
    {
        None,
        WaitingForAITangkis,       
        WaitingForPlayerTangkis,   
        PlayerSelectingKartuTangkis 
    }

    public enum TieBreakerReason { PlayerTrapped, DekEmpty }

    private StateGiliranPlayer playerState;
    private AttackPhase currentPhase = AttackPhase.None;

    private Player player;
    private Player ai;
    private Player activePlayer;

    private bool isGameOver = false;
    private int roundNumber = 0;
    private int arahLangkah = 0;

    private Player attackingSide; 
    private Player defendingSide; 
    
    private int attackValue;
    private Card kartuInitialSerang;
    private List<KeyValuePair<Card, SistemKartu>> selectedKartuTangkis = new List<KeyValuePair<Card, SistemKartu>>();
    private bool isSerangBalik = false;
    private bool isPaused = false;
    private const float DELAY_BUTTON_PRESS = 0.3f;

    void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        if (aiController != null) aiController.Initialize(this, uiManager);
        
        if (sliderVolume != null && TransisiScene.Instance != null)
        {
            sliderVolume.value = TransisiScene.Instance.GetMasterVolume();
            sliderVolume.onValueChanged.AddListener(OnVolumeChanged);
        }

        SetupGame();
        if (AnimasiManager.Instance != null) AnimasiManager.Instance.ResetAllAnimations();
        StartRonde();
    }

    public void OnVolumeChanged(float value)
    {
        if (TransisiScene.Instance != null) TransisiScene.Instance.SetMasterVolume(value);
    }

    void SetupGame()
    {
        player = new Player("Manusia", posisiAwalPlayer);
        ai = new Player("AI", posisiAwalAI, true);
        uiManager.ResetScoreUI();
    }

    public void StartRonde()
    {
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();

        currentPhase = AttackPhase.None;
        playerState = StateGiliranPlayer.None;
        isSerangBalik = false;
        isGameOver = false;
        roundNumber++;

        player.position = posisiAwalPlayer;
        ai.position = posisiAwalAI;
        MovePionVisual(player, player.position);
        MovePionVisual(ai, ai.position);

        DeckManager.Instance.SetupDek();
        
        player.hand.Clear();
        ai.hand.Clear();

        for (int i = 0; i < maxUkuranKartu; i++)
        {
            player.hand.Add(DeckManager.Instance.DrawDek());
            ai.hand.Add(DeckManager.Instance.DrawDek());
        }

        uiManager.UpdatePlayerHandUI(player);
        uiManager.UpdateMainDekUI(DeckManager.Instance.GetSisaDek());
        
        activePlayer = (roundNumber % 2 != 0) ? ai : player;

        StartCoroutine(ShowRondeStartMessageAndBeginTurn());
    }

    private void StartTurn()
    {
        if (isGameOver) return;
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
        
        playerState = StateGiliranPlayer.None;
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

        RefillKartu(player);
        RefillKartu(ai);

        if (DeckManager.Instance.IsDekEmpty() && (player.hand.Count < maxUkuranKartu || ai.hand.Count < maxUkuranKartu))
        {
            StartCoroutine(DelayTieBreaker(TieBreakerReason.DekEmpty));
            return;
        }

        activePlayer = (activePlayer == player) ? ai : player;
        StartTurn();
    }

    public void BtnMelangkahPressed()
    {
        if (isPaused) return;
        
        uiManager.HideMessage(); 

        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToMelangkah();
        StartCoroutine(BtnMelangkahPressedCoroutine());
    }

    public void BtnArahLangkahPressed(bool isMaju)
    {
        if (isPaused) return;
        StartCoroutine(BtnArahLangkahPressedCoroutine(isMaju));
    }

    public void BtnSerangPressed()
    {
        if (isPaused) return;
        StartCoroutine(BtnSerangPressedCoroutine());
    }

    public void BtnPerkuatSeranganYes()
    {
        if (isPaused) return;
        playerState = StateGiliranPlayer.PerkuatSerangan;
        uiManager.ShowSelectKartuAction(ActionType.Perkuat);
    }

    public void BtnPerkuatSeranganNo()
    {
        if (isPaused) return;
        
        uiManager.HideMessage();
        uiManager.HideAllPlayerPanels();
        
        InitiateSerangan(player, ai, kartuInitialSerang.value, false, false);
    }

    public void BtnSergapYes()
    {
        if (isPaused) return;
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
        playerState = StateGiliranPlayer.SelectingKartuSergap;
        uiManager.ShowSelectKartuAction(ActionType.Sergap);
    }

    public void BtnSergapNo()
    {
        if (isPaused) return;
        uiManager.HideAllPlayerPanels();
        EndTurn();
    }

    public void BtnTangkisYes()
    {
        if (isPaused) return;
        if (currentPhase != AttackPhase.WaitingForPlayerTangkis) return;
        
        currentPhase = AttackPhase.PlayerSelectingKartuTangkis;
        uiManager.ShowSelectKartuTangkis(attackValue);
        uiManager.SetupConfirmTangkisAction(BtnConfirmTangkis);
    }

    public void BtnTangkisNo()
    {
        if (isPaused) return;
        if (currentPhase != AttackPhase.WaitingForPlayerTangkis) return;

        uiManager.HideAllPlayerPanels();
        currentPhase = AttackPhase.None;

        uiManager.PlayCutscene(uiManager.clipManusiaGagalTangkis, () => 
        {
            StartCoroutine(DelayRondeEnd(attackingSide));
        });
    }

    public void BtnConfirmTangkis()
    {
        if (isPaused) return;
        uiManager.HideConfirmTangkisButton();
        uiManager.SetPlayerHandInteractable(false);
        uiManager.HideMessage();

        int totalTangkisValue = selectedKartuTangkis.Sum(entry => entry.Key.value);
        bool isValueCorrect = totalTangkisValue == attackValue;
        bool hasKartuLeftForCounter = player.hand.Count - selectedKartuTangkis.Count >= 1;

        if (isValueCorrect)
        {
            if (isSerangBalik)
            {
                uiManager.PlayCutscene(uiManager.clipManusiaBerhasilTangkis, () => 
                {
                    uiManager.ShowMessage("Serang balik berhasil ditangkis!", 3.0f);
                    foreach (var entry in selectedKartuTangkis)
                    {
                        player.hand.Remove(entry.Key);
                        entry.Value.HideSlot();
                    }
                    EndTurn();
                });
            }
            else if (hasKartuLeftForCounter)
            {
                uiManager.PlayCutscene(uiManager.clipManusiaBerhasilTangkis, () => 
                {
                    foreach (var entry in selectedKartuTangkis)
                    {
                        player.hand.Remove(entry.Key);
                        entry.Value.HideSlot();
                    }
                    StartCoroutine(ExecutePlayerSerangBalikCoroutine());
                });
            }
            else
            {
                uiManager.PlayCutscene(uiManager.clipManusiaGagalTangkis, () => 
                {
                    uiManager.ShowMessage("Tangkisan Tidak Sah! Wajib menyisakan 1 kartu untuk Serang Balik.", 3.5f);
                    StartCoroutine(DelayRondeEnd(attackingSide));
                });
            }
        }
        else
        {
            uiManager.PlayCutscene(uiManager.clipManusiaGagalTangkis, () => 
            {
                uiManager.ShowTangkisFailedMessage(3.5f);
                StartCoroutine(DelayRondeEnd(attackingSide));
            });
        }

        foreach (var entry in selectedKartuTangkis) entry.Value.ToggleSelection(false);
        selectedKartuTangkis.Clear();
        currentPhase = AttackPhase.None;
    }

    public void OnKartuHandPressed(Card clickedKartu, SistemKartu slotController)
    {
        if (isPaused) return;

        if (currentPhase == AttackPhase.PlayerSelectingKartuTangkis)
        {
            HandleKartuTangkisSelection(clickedKartu, slotController);
            return;
        }

        if (playerState == StateGiliranPlayer.None) return;

        uiManager.HideKartuPanel();
        uiManager.SetPlayerHandInteractable(false);
        uiManager.HideMessage();

        StartCoroutine(HandleKartuActionCoroutine(clickedKartu, slotController));
    }

    private void HandleSergap(Card clickedKartu, SistemKartu slotController)
    {
        int distance = ai.position - player.position;
        if (clickedKartu.value == distance)
        {
            player.hand.Remove(clickedKartu);
            slotController.HideSlot();
            if (CameraManager.Instance != null) CameraManager.Instance.SwitchToAttack();
            InitiateSerangan(player, ai, clickedKartu.value, false, false);
        }
        else
        {
            uiManager.ShowMessage("Sergap GAGAL!! Nilai tidak sesuai", 2.5f);
            EndTurn();
        }
    }

    public void InitiateSerangan(Player currentAttacker, Player currentDefender, int value, bool isCounter, bool showAttackUI = true)
    {
        attackingSide = currentAttacker;
        defendingSide = currentDefender;
        attackValue = value;
        isSerangBalik = isCounter;

        if (showAttackUI && currentAttacker == player) 
        {
            uiManager.ShowAttackValue(attackValue);
        }
        else
        {
            uiManager.HideAttackStrength();
        }

        if (defendingSide.isAI)
        {
            currentPhase = AttackPhase.WaitingForAITangkis;
            aiController.HandleAIAttacked(ai, player, attackValue, isCounter);
        }
        else
        {
            if (CameraManager.Instance != null) CameraManager.Instance.SwitchToTangkis();
            currentPhase = AttackPhase.WaitingForPlayerTangkis;
            StartCoroutine(uiManager.ShowTangkis(value, isCounter));
        }
    }

    private void HandleKartuTangkisSelection(Card clickedKartu, SistemKartu slotController)
    {
        var kartuEntry = new KeyValuePair<Card, SistemKartu>(clickedKartu, slotController);
        if (selectedKartuTangkis.Any(p => p.Value == slotController))
        {
            selectedKartuTangkis.RemoveAll(p => p.Value == slotController);
            slotController.ToggleSelection(false);
        }
        else
        {
            selectedKartuTangkis.Add(kartuEntry);
            slotController.ToggleSelection(true);
        }
        int currentTangkisTotal = selectedKartuTangkis.Sum(entry => entry.Key.value);
        uiManager.UpdateTotalTangkisValue(currentTangkisTotal, attackValue);
    }

    private void RefillKartu(Player targetPlayer)
    {
        if (targetPlayer.isAI)
        {
            while (targetPlayer.hand.Count < maxUkuranKartu)
            {
                Card newKartu = DeckManager.Instance.DrawDek();
                if (newKartu == null) return;
                targetPlayer.hand.Add(newKartu);
            }
        }
        else
        {
            foreach (SistemKartu slot in uiManager.cardSlots)
            {
                if (targetPlayer.hand.Count >= maxUkuranKartu) break;
                if (!slot.gameObject.activeSelf)
                {
                    Card newKartu = DeckManager.Instance.DrawDek();
                    if (newKartu == null) break;
                    targetPlayer.hand.Add(newKartu);
                    slot.Initialize(newKartu);
                }
            }
        }
        uiManager.UpdateMainDekUI(DeckManager.Instance.GetSisaDek());
    }

    private void CheckInitialOptions()
    {
        bool canMelangkah = player.hand.Any(kartu => (player.position + kartu.value < ai.position) || (player.position - kartu.value >= 1));
        int distance = ai.position - player.position;
        bool canSerang = player.hand.Any(kartu => kartu.value == distance);

        uiManager.ShowInitialChoice(canMelangkah, canSerang);

        if (!canMelangkah && !canSerang) StartCoroutine(DelayTieBreaker(TieBreakerReason.PlayerTrapped));
    }

    private void CheckAvailableArahLangkah()
    {
        bool canMaju = player.hand.Any(kartu => player.position + kartu.value < ai.position);
        bool canMundur = player.hand.Any(kartu => player.position - kartu.value >= 1);
        StartCoroutine(uiManager.ShowArahLangkah(canMaju, canMundur));
    }

    public void MovePionVisual(Player targetPlayer, int targetPosition)
    {
        GameObject pion = targetPlayer.isAI ? pionAI : pionManusia;
        if (targetPosition > 0 && targetPosition <= petakPapan.Length)
        {
            pion.transform.position = petakPapan[targetPosition - 1].position + new Vector3(0, 5f, 0);
        }
    }

    public IEnumerator MovePionPerLangkah(Player targetPlayer, int targetPosition)
    {
        GameObject pion = targetPlayer.isAI ? pionAI : pionManusia;
        if (targetPosition <= 0 || targetPosition > petakPapan.Length) yield break;

        int currentPos = targetPlayer.position;
        int stepDirection = (targetPosition > currentPos) ? 1 : -1;
        float stepDuration = 1.15f; 

        AnimasiManager.Instance.SetWalking(targetPlayer.isAI, true);

        while (currentPos != targetPosition)
        {
            int nextPos = currentPos + stepDirection;
            Vector3 startPos = petakPapan[currentPos - 1].position + new Vector3(0, 5f, 0);
            Vector3 endPos = petakPapan[nextPos - 1].position + new Vector3(0, 5f, 0);
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / stepDuration;
                pion.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            pion.transform.position = endPos;
            currentPos = nextPos;
            targetPlayer.position = currentPos;
            yield return null;
        }
        AnimasiManager.Instance.SetWalking(targetPlayer.isAI, false);
    }

    private IEnumerator ShowRondeStartMessageAndBeginTurn()
    {
        yield return StartCoroutine(uiManager.ShowRondePanel(roundNumber, 2.0f));
        StartTurn();
    }

    private IEnumerator ShowPlayerTurnMessage()
    {
        uiManager.panelInfo.SetActive(true);
        uiManager.ShowMessage("Giliran anda!!", 2.0f);
        yield return new WaitForSeconds(1.5f);
        CheckInitialOptions();
    }

    private IEnumerator BtnMelangkahPressedCoroutine()
    {
        if (activePlayer != player) yield break;
        yield return new WaitForSeconds(DELAY_BUTTON_PRESS);
        CheckAvailableArahLangkah();
    }

    private IEnumerator BtnArahLangkahPressedCoroutine(bool isMaju)
    {
        if (activePlayer != player) yield break;
        yield return new WaitForSeconds(DELAY_BUTTON_PRESS);

        arahLangkah = isMaju ? 1 : -1;
        playerState = StateGiliranPlayer.SelectingKartuMelangkah;
        uiManager.ShowSelectKartuAction(ActionType.Melangkah);
    }

    private IEnumerator BtnSerangPressedCoroutine()
    {
        if (activePlayer != player) yield break;
        yield return new WaitForSeconds(DELAY_BUTTON_PRESS);

        playerState = StateGiliranPlayer.SelectingKartuSerang;
        uiManager.ShowSelectKartuAction(ActionType.Serang);
        
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
    }

    private IEnumerator HandleKartuActionCoroutine(Card clickedKartu, SistemKartu slotController)
    {
        StateGiliranPlayer currentState = playerState;
        playerState = StateGiliranPlayer.None;

        switch (currentState)
        {
            case StateGiliranPlayer.SelectingKartuSerang:
                int distance = ai.position - player.position;
                if (clickedKartu.value == distance)
                {
                    kartuInitialSerang = clickedKartu;
                    player.hand.Remove(clickedKartu);
                    slotController.HideSlot();
                    
                    if (player.hand.Count > 0) uiManager.ShowPerkuatSerangan();
                    else InitiateSerangan(player, ai, kartuInitialSerang.value, false);
                }
                else
                {
                    uiManager.ShowMessage("kartu tidak sesuai! giliran ai", 2.0f);
                    yield return new WaitForSeconds(2.0f); 
                    EndTurn();
                }
                break;

            case StateGiliranPlayer.PerkuatSerangan:
                int totalSerangValue = kartuInitialSerang.value + clickedKartu.value;
                player.hand.Remove(clickedKartu);
                slotController.HideSlot();
                
                uiManager.ShowAttackValue(totalSerangValue);
                yield return new WaitForSeconds(0.7f);
                uiManager.HideAttackStrength();

                InitiateSerangan(player, ai, totalSerangValue, false, false);
                break;

            case StateGiliranPlayer.SelectingKartuMelangkah:
                yield return StartCoroutine(HandleMelangkahCoroutine(clickedKartu, slotController));
                break;

            case StateGiliranPlayer.SelectingKartuSergap:
                HandleSergap(clickedKartu, slotController);
                break;

            case StateGiliranPlayer.SelectingKartuSerangBalik:
                player.hand.Remove(clickedKartu);
                slotController.HideSlot();
                InitiateSerangan(player, ai, clickedKartu.value, true, false);
                break;
        }
    }   

    private IEnumerator HandleMelangkahCoroutine(Card clickedKartu, SistemKartu slotController)
    {
        int newPosition = player.position + (clickedKartu.value * arahLangkah);
        bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);

        if (isValidMove)
        {
            player.hand.Remove(clickedKartu);
            slotController.HideSlot();
            if (CameraManager.Instance != null) CameraManager.Instance.SwitchToBergerak();
            yield return new WaitForSeconds(1f);
            yield return StartCoroutine(MovePionPerLangkah(player, newPosition));

            if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();

            int newDistance = ai.position - player.position;
            bool canSergap = player.hand.Any(kartu => kartu.value == newDistance);
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
        playerState = StateGiliranPlayer.SelectingKartuSerangBalik;
        uiManager.ShowSelectKartuAction(ActionType.SerangBalik);
    }

    public IEnumerator DelayRondeEnd(Player winner)
    {
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
        uiManager.HideAllUIsForRondeEnd();

        if (winner.score < (skorMenang - 1)) uiManager.ShowMessage($"{winner.playerName} memenangkan Ronde Ke-{roundNumber}!", 3.0f);

        winner.score++;
        uiManager.UpdateScoreUI(winner);
        yield return new WaitForSeconds(3.0f);

        if (winner.score >= skorMenang)
        {
            isGameOver = true;
            yield return new WaitForSeconds(0.3f);
            string winnerName = winner.isAI ? "AI" : "Manusia";
            yield return StartCoroutine(uiManager.ShowGameWin(winnerName, 2f));

            string nextScene = (winner == player) ? "SceneWIN" : "SceneLOSE";
            
            if (TransisiScene.Instance != null) 
                TransisiScene.Instance.LoadSceneTransisi(nextScene);
            else 
                SceneManager.LoadScene(nextScene);
        }
        else StartRonde();
    }

    public IEnumerator DelayTieBreaker(TieBreakerReason reason)
    {
        uiManager.HideAllUIsForRondeEnd();
        string message = "";
        switch (reason)
        {
            case TieBreakerReason.PlayerTrapped: message = "Pemain Terpojok! TIE BREAKER diaktifkan."; break;
            case TieBreakerReason.DekEmpty: message = "Deck Kartu Habis! TIE BREAKER diaktifkan."; break;
        }
        uiManager.ShowMessage(message, 3.5f);
        yield return new WaitForSeconds(3.5f);

        int totalManusia = player.hand.Sum(kartu => kartu.value);
        int totalAI = ai.hand.Sum(kartu => kartu.value);
        Player winner = (totalManusia > totalAI) ? player : (totalAI > totalManusia ? ai : null);

        if (winner != null) StartCoroutine(DelayRondeEnd(winner));
        else
        {
            uiManager.ShowMessage("Hasil Tie Breaker seri!", 3.0f);
            yield return new WaitForSeconds(3.0f);
            StartRonde();
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
        bool shouldBeInteractable = playerState != StateGiliranPlayer.None || currentPhase == AttackPhase.PlayerSelectingKartuTangkis;
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

    public void BackToMainMenu()
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