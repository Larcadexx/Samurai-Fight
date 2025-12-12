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

    private const string TAG_MANUSIA = "Manusia";
    private const string TAG_AI = "AI";
    private const string SCENE_WIN = "SceneWIN";
    private const string SCENE_LOSE = "SceneLOSE";
    private const string SCENE_MAIN_MENU = "MainMenu";

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
    private int rondeNumber = 0;
    private int arahLangkah = 0;

    private Player attackingSide; 
    private Player defendingSide; 
    
    private int attackValue;
    private Card kartuInitialSerang;
    
    private List<KeyValuePair<Card, CardSystem>> selectedKartuTangkis = new List<KeyValuePair<Card, CardSystem>>();
    private bool isSerangBalik = false;
    private bool isPaused = false;
    private const float DELAY_BUTTON_PRESS = 0.3f;

    private List<int> emptySlots = new List<int>();

    void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        if (aiController != null) aiController.Initialize(this, uiManager);
        
        SetupGame();
        if (AnimasiManager.Instance != null) AnimasiManager.Instance.ResetAllAnimations();
        StartRonde();
    }

    void SetupGame()
    {
        player = new Player(TAG_MANUSIA, posisiAwalPlayer);
        ai = new Player(TAG_AI, posisiAwalAI, true);
        uiManager.ResetScoreUI();
    }

    public void StartRonde()
    {
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();

        currentPhase = AttackPhase.None;
        playerState = StateGiliranPlayer.None;
        isSerangBalik = false;
        isGameOver = false;
        rondeNumber++;

        emptySlots.Clear();

        player.position = posisiAwalPlayer;
        ai.position = posisiAwalAI;
        MovePionVisual(player, player.position);
        MovePionVisual(ai, ai.position);

        DekManager.Instance.SetupDek();
        
        player.hand.Clear();
        ai.hand.Clear();

        uiManager.UpdatePlayerHandUI(player);
        uiManager.UpdateMainDekUI(DekManager.Instance.GetSisaDek());
        
        activePlayer = (rondeNumber % 2 != 0) ? ai : player;

        StartCoroutine(StartRondeSequence());
    }

    private IEnumerator StartRondeSequence()
    {
        yield return StartCoroutine(uiManager.ShowRondePanel(rondeNumber, 2.0f));

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(RefillKartuAI());

        yield return new WaitForSeconds(0.8f);

        int dekAfterAI = DekManager.Instance.GetSisaDek();
        List<int> slotsToFill = new List<int>();
        
        for (int i = 0; i < maxUkuranKartu; i++)
        {
            slotsToFill.Add(i);
            Card newKartu = DekManager.Instance.DrawDek();
            player.hand.Add(newKartu);

            if (i < uiManager.cardSlots.Length)
            {
                uiManager.cardSlots[i].Initialize(newKartu);
                uiManager.cardSlots[i].SetInteractable(false); 
                uiManager.cardSlots[i].gameObject.SetActive(false); 
            }
        }

        bool animationDone = false;
        
        StartCoroutine(uiManager.AnimateCardRefill(slotsToFill, dekAfterAI, () => { animationDone = true; }));

        uiManager.SetPlayerHandInteractable(false, false); 

        while (!animationDone) yield return null;
        
        uiManager.UpdatePlayerHandUI(player);

        StartTurn();
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

        if (activePlayer.isAI)
        {
            if (uiManager.panelDek != null) 
            {
                if (uiManager.panelDekCanvasGroup != null)
                     StartCoroutine(uiManager.TogglePanelFade(uiManager.panelDek, uiManager.panelDekCanvasGroup, true));
                else
                     uiManager.panelDek.SetActive(true);
            }

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
        StartCoroutine(EndTurnSequence());
    }

    private IEnumerator EndTurnSequence()
    {
        yield return new WaitForSeconds(1.5f);

        int playerNeeds = maxUkuranKartu - player.hand.Count;
        int aiNeeds = maxUkuranKartu - ai.hand.Count; 
        int totalNeeded = playerNeeds + aiNeeds;
        
        int currentDekCount = DekManager.Instance.GetSisaDek();
        if (currentDekCount < totalNeeded)
        {
            StartCoroutine(DelayTieBreaker(TieBreakerReason.DekEmpty));
            yield break; 
        }

        yield return StartCoroutine(RefillKartuAI());

        yield return new WaitForSeconds(0.8f);

        if (playerNeeds > 0)
        {
            int dekAfterAI = DekManager.Instance.GetSisaDek();
            emptySlots.Sort(); 

            foreach (int indexTarget in emptySlots)
            {
                Card newKartu = DekManager.Instance.DrawDek();
                if (newKartu != null)
                {
                    if (indexTarget <= player.hand.Count)
                        player.hand.Insert(indexTarget, newKartu);
                    else
                        player.hand.Add(newKartu);
                    
                    if (indexTarget < uiManager.cardSlots.Length)
                    {
                        uiManager.cardSlots[indexTarget].Initialize(newKartu);
                        
                        uiManager.cardSlots[indexTarget].SetInteractable(false);

                        uiManager.cardSlots[indexTarget].gameObject.SetActive(false); 
                    }
                }
            }

            bool animationDone = false;
            
            StartCoroutine(uiManager.AnimateCardRefill(new List<int>(emptySlots), dekAfterAI, () => { animationDone = true; }));
            
            uiManager.SetPlayerHandInteractable(false, false);
            
            while (!animationDone) yield return null;
            
            uiManager.UpdatePlayerHandUI(player);
        }

        emptySlots.Clear();

        activePlayer = (activePlayer == player) ? ai : player;
        StartTurn();
    }

    private IEnumerator RefillKartuAI()
    {
        if (uiManager.panelDek != null) 
        {
            if (uiManager.panelDekCanvasGroup != null)
            {
                yield return StartCoroutine(uiManager.TogglePanelFade(uiManager.panelDek, uiManager.panelDekCanvasGroup, true, 0.5f));
            }
            else
            {
                uiManager.panelDek.SetActive(true);
            }
        }

        yield return new WaitForSeconds(0.3f);

        while (ai.hand.Count < maxUkuranKartu)
        {
            Card newKartu = DekManager.Instance.DrawDek();
            if (newKartu == null) break; 
            
            ai.hand.Add(newKartu);
            
            uiManager.UpdateMainDekUI(DekManager.Instance.GetSisaDek());

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void RegisterEmptySlot(CardSystem slot)
    {
        int idx = System.Array.IndexOf(uiManager.cardSlots, slot);
        if (idx != -1 && !emptySlots.Contains(idx))
        {
            emptySlots.Add(idx);
        }
    }

    private IEnumerator ShowPlayerTurnMessage()
    {
        uiManager.panelInfo.SetActive(true);
        uiManager.panelDek.SetActive(true);
        uiManager.ShowMessage("Giliran Anda!", 2.0f);
        yield return new WaitForSeconds(1.5f);
        CheckOpsiAwal();
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
        uiManager.SetConfirmTangkisInteractable(false); 
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

        uiManager.HideAttackStrength();

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
                        RegisterEmptySlot(entry.Value); 
                        player.hand.Remove(entry.Key);
                        entry.Value.HideSlot(); 
                    }

                    CleanupTangkisState(); 
                    EndTurn();
                });
            }
            else if (hasKartuLeftForCounter)
            {
                uiManager.PlayCutscene(uiManager.clipManusiaBerhasilTangkis, () => 
                {
                    foreach (var entry in selectedKartuTangkis)
                    {
                        RegisterEmptySlot(entry.Value); 
                        player.hand.Remove(entry.Key);
                    }

                    CleanupTangkisState(); 
                    StartCoroutine(ExecutePlayerSerangBalikCoroutine());
                });
            }
            else
            {
                uiManager.PlayCutscene(uiManager.clipManusiaGagalTangkis, () => 
                {
                    CleanupTangkisState();
                    uiManager.ShowMessage("Tangkisan Tidak Sah! Wajib menyisakan 1 kartu untuk Serang Balik.", 3.5f);
                    StartCoroutine(DelayRondeEnd(attackingSide));
                });
            }
        }
        else
        {
            uiManager.PlayCutscene(uiManager.clipManusiaGagalTangkis, () => 
            {
                CleanupTangkisState();
                uiManager.ShowTangkisFailedMessage(3.5f);
                StartCoroutine(DelayRondeEnd(attackingSide));
            });
        }
    }

    private void CleanupTangkisState()
    {
        uiManager.HideAttackStrength();
        currentPhase = AttackPhase.None;

        foreach (var entry in selectedKartuTangkis) 
        {
            if(entry.Value != null)
            {
                entry.Value.ToggleSelection(false);
                entry.Value.HideSlot(); 
            }
        }
        
        selectedKartuTangkis.Clear();
    }

    public void OnCardHandPressed(Card clickedKartu, CardSystem slotController)
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

    private void HandleSergap(Card clickedKartu, CardSystem slotController)
    {
        int distance = ai.position - player.position;
        if (clickedKartu.value == distance)
        {
            RegisterEmptySlot(slotController); 
            player.hand.Remove(clickedKartu);
            slotController.HideSlot();
            if (CameraManager.Instance != null) CameraManager.Instance.SwitchToAttack();
            InitiateSerangan(player, ai, clickedKartu.value, false, false);
        }
        else
        {
            uiManager.ShowMessage("Sergap GAGAL!! Kartu tidak sesuai", 2.5f);
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

    private void HandleKartuTangkisSelection(Card clickedKartu, CardSystem slotController)
    {
        var kartuEntry = new KeyValuePair<Card, CardSystem>(clickedKartu, slotController);
        
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

        bool adaKartu = selectedKartuTangkis.Count > 0;
        uiManager.SetConfirmTangkisInteractable(adaKartu);
    }

    private void CheckOpsiAwal()
    {
        SetAllKartuInteractable(false);
        bool canMelangkah = player.hand.Any(kartu => (player.position + kartu.value < ai.position) || (player.position - kartu.value >= 1));
        int distance = ai.position - player.position;
        bool canSerang = player.hand.Any(kartu => kartu.value == distance);

        uiManager.ShowOpsiAwal(canMelangkah, canSerang);

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

        SetAllKartuInteractable(true);
    }

    private IEnumerator BtnSerangPressedCoroutine()
    {
        if (activePlayer != player) yield break;
        yield return new WaitForSeconds(DELAY_BUTTON_PRESS);

        playerState = StateGiliranPlayer.SelectingKartuSerang;
        uiManager.ShowSelectKartuAction(ActionType.Serang);
        
        SetAllKartuInteractable(true);

        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
    }

    private IEnumerator HandleKartuActionCoroutine(Card clickedKartu, CardSystem slotController)
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
                    RegisterEmptySlot(slotController); 
                    player.hand.Remove(clickedKartu);
                    slotController.HideSlot();
                    
                    if (player.hand.Count > 0) uiManager.ShowPerkuatSerangan();
                    else InitiateSerangan(player, ai, kartuInitialSerang.value, false);
                }
                else
                {
                    uiManager.ShowMessage("Kartu tidak sesuai!", 2.0f);
                    yield return new WaitForSeconds(2.0f); 
                    EndTurn();
                }
                break;
            case StateGiliranPlayer.PerkuatSerangan:
                int totalSerangValue = kartuInitialSerang.value + clickedKartu.value;
                RegisterEmptySlot(slotController); 
                player.hand.Remove(clickedKartu);
                slotController.HideSlot();

                InitiateSerangan(player, ai, totalSerangValue, false, false);
                break;
            case StateGiliranPlayer.SelectingKartuMelangkah:
                yield return StartCoroutine(HandleMelangkahCoroutine(clickedKartu, slotController));
                break;

            case StateGiliranPlayer.SelectingKartuSergap:
                HandleSergap(clickedKartu, slotController);
                break;
            case StateGiliranPlayer.SelectingKartuSerangBalik:
                RegisterEmptySlot(slotController); 
                player.hand.Remove(clickedKartu);
                slotController.HideSlot();
                
                uiManager.HideMessage();
                uiManager.HideAttackStrength();

                InitiateSerangan(player, ai, clickedKartu.value, true, false);
                break;
        }
    }

    private IEnumerator HandleMelangkahCoroutine(Card clickedKartu, CardSystem slotController)
    {
        int newPosition = player.position + (clickedKartu.value * arahLangkah);
        
        bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);

        RegisterEmptySlot(slotController); 
        player.hand.Remove(clickedKartu);
        slotController.HideSlot();

        if (isValidMove)
        {
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
            else 
            {
                EndTurn();
            }
        }
        else
        {
            uiManager.ShowMessage("Langkah GAGAL! Kartu tidak sesuai.", 2.5f);
            yield return new WaitForSeconds(2.5f);
            EndTurn(); 
        }
    }

    private IEnumerator ExecutePlayerSerangBalikCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToAttack();
        
        playerState = StateGiliranPlayer.SelectingKartuSerangBalik;
        
        uiManager.ShowSelectKartuAction(ActionType.SerangBalik);

        uiManager.HideAttackStrength(); 
    }

    public IEnumerator DelayRondeEnd(Player winner)
    {
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();

        uiManager.HideAllUIsForRondeEnd();

        winner.score++;
        uiManager.UpdateScoreUI(winner);

        yield return new WaitForSeconds(2.0f); 

        if (winner.score >= skorMenang)
        {
            isGameOver = true;
            yield return new WaitForSeconds(0.3f);
            string winnerName = winner.isAI ? TAG_AI : TAG_MANUSIA;
            yield return StartCoroutine(uiManager.ShowGameWin(winnerName, 2f));

            string nextScene = (winner == player) ? SCENE_WIN : SCENE_LOSE;
            
            if (TransisiScene.Instance != null) 
                TransisiScene.Instance.LoadSceneTransisi(nextScene);
            else 
                SceneManager.LoadScene(nextScene);
        }
        else 
        {
            StartRonde(); 
        }
    }

    public IEnumerator DelayTieBreaker(TieBreakerReason reason)
    {
        uiManager.HideAllUIsForRondeEnd();
        string message = "";
        switch (reason)
        {
            case TieBreakerReason.PlayerTrapped: message = "Pemain Terpojok! TIE BREAKER diaktifkan."; break;
            case TieBreakerReason.DekEmpty: message = "Dek Kartu Habis! TIE BREAKER diaktifkan."; break;
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
            TransisiScene.Instance.LoadSceneTransisi(SCENE_MAIN_MENU);
        }
        else
        {
            SceneManager.LoadScene(SCENE_MAIN_MENU);
        }
    }

    private void SetAllKartuInteractable(bool aktif)
    {
        if (uiManager != null && uiManager.cardSlots != null)
        {
            foreach (CardSystem slot in uiManager.cardSlots)
            {
                if (slot != null) slot.SetInteractable(aktif);
            }
        }
    }

    public List<Card> GetAIHand()
    {
        if (ai != null)
        {
            return ai.hand;
        }
        return null;
    }
}