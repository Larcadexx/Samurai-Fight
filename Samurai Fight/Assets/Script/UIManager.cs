using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; 
using System.Linq; 
using UnityEngine.Events;
using UnityEngine.Video; 

public enum ActionType
{
    Melangkah,
    Serang,
    Perkuat,
    Sergap,
    SerangBalik
}

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Setup")]
    public Image[] scoreManusiaImages;
    public Image[] scoreAiImages;
    
    public CardSystem[] cardSlots;

    [Header("Position Panel Kartu")]
    public Transform defaultPanelPosition;
    public Transform rightPanelPosition;
    public Transform bottomPanelPosition;
    
    [Header("Animation References")]
    public RectTransform deckPosition; 
    
    [Header("Text System")]
    public TextMeshProUGUI attackValueText;
    public TextMeshProUGUI systemText;
    public TextMeshProUGUI remainingDekText;

    [Header("Display Ronde & Victory")]
    public Image rondeImage; 
    public Sprite[] roundNumberSprites;
    public Sprite playerWinSprite; 
    public Sprite aiWinSprite; 

    [Header("Panels")]
    public GameObject panelKartuManusia;
    public GameObject panelDek; 
    public GameObject panelInfo;
    public GameObject panelOpsiAwal;
    public GameObject panelAksiMelangkah;
    public GameObject panelAksiTangkis;
    public GameObject panelAksiSergap;
    public GameObject panelPerkuatSerangan;
    public GameObject panelRonde;
    public GameObject panelPause; 

    [Header("Canvas Groups References")] 
    public CanvasGroup panelDekCanvasGroup;
    public CanvasGroup panelKartuManusiaCanvasGroup;

    [Header("Action Buttons")]
    public Button btnMelangkah;
    public Button btnSerang;
    public Button btnMaju;
    public Button btnMundur;
    public Button confirmTangkisButton;
    
    [Header("Volume Control & Pause")]
    public Slider sliderVolume; 
    public Button volumeButton;
    public Sprite volumeOnSprite;
    public Sprite volumeOffSprite;

    private Coroutine messageCoroutine;
    private CanvasGroup roundPanelCanvasGroup;

    [Header("Visual Indicators")]
    public GameObject melangkahArrow;

    [Header("Cutscene System")]
    public VideoPlayer videoPlayer; 
    public GameObject cutscenePanel; 
    public VideoClip clipManusiaBerhasilTangkis;
    public VideoClip clipManusiaGagalTangkis; 
    public VideoClip clipAIBerhasilTangkis;
    public VideoClip clipAIGagalTangkis;

    void Awake()
    {
        instance = this;
        
        if (panelDek != null && panelDekCanvasGroup == null) 
            panelDekCanvasGroup = panelDek.GetComponent<CanvasGroup>();
            
        if (panelKartuManusia != null && panelKartuManusiaCanvasGroup == null)
            panelKartuManusiaCanvasGroup = panelKartuManusia.GetComponent<CanvasGroup>();

        HideAllPlayerPanels();
        
        if (panelDek != null) panelDek.SetActive(false);

        if (attackValueText != null) attackValueText.gameObject.SetActive(false);
        if (systemText != null) systemText.gameObject.SetActive(false);
        if (confirmTangkisButton != null) confirmTangkisButton.gameObject.SetActive(false);

        if (panelRonde != null)
        {
            roundPanelCanvasGroup = panelRonde.GetComponent<CanvasGroup>();
            panelRonde.SetActive(false);
        }

        if (panelPause != null) panelPause.SetActive(false); 
        
        if (volumeButton != null)
        {
            volumeButton.onClick.RemoveAllListeners();
            volumeButton.onClick.AddListener(OnVolumeButtonPressed);
        }

        if (sliderVolume != null)
        {
            if (TransisiScene.Instance != null)
            {
                sliderVolume.value = TransisiScene.Instance.GetMasterVolume();
                UpdateVolumeButtonSprite(TransisiScene.Instance.IsMuted());
            }
            sliderVolume.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    public IEnumerator AnimateCardRefill(List<int> targetSlots, int startDeckCount, System.Action onComplete)
    {
        MoveKartuPanel(defaultPanelPosition); 
        
        if (panelKartuManusiaCanvasGroup != null)
            yield return StartCoroutine(FadePanel(panelKartuManusia, panelKartuManusiaCanvasGroup, true, 0.5f));
        else 
            panelKartuManusia.SetActive(true);

        if (panelDek != null && !panelDek.activeSelf)
        {
            if (panelDekCanvasGroup != null)
                StartCoroutine(FadePanel(panelDek, panelDekCanvasGroup, true, 0.5f));
            else
                panelDek.SetActive(true);
        }
        
        foreach(int idx in targetSlots)
        {
            if(idx < cardSlots.Length) cardSlots[idx].gameObject.SetActive(false);
        }
        
        int visualDeckCount = startDeckCount;
        UpdateMainDekUI(visualDeckCount);

        foreach (int slotIndex in targetSlots)
        {
            if (slotIndex >= cardSlots.Length) continue;

            visualDeckCount--;
            UpdateMainDekUI(visualDeckCount);

            GameObject flyingCard = new GameObject("FlyingCard");
            flyingCard.transform.SetParent(panelKartuManusia.transform.parent); 
            if (deckPosition != null) flyingCard.transform.position = deckPosition.position;
            flyingCard.transform.localScale = Vector3.one;

            Image img = flyingCard.AddComponent<Image>();
            if (deckPosition != null && deckPosition.GetComponent<Image>() != null)
            {
                img.sprite = deckPosition.GetComponent<Image>().sprite; 
            }
            
            RectTransform flyRect = flyingCard.GetComponent<RectTransform>();
            RectTransform targetRect = cardSlots[slotIndex].GetComponent<RectTransform>();
            flyRect.sizeDelta = targetRect.sizeDelta;

            float duration = 0.65f; 
            float elapsed = 0f;
            Vector3 startPos = (deckPosition != null) ? deckPosition.position : Vector3.zero;
            Vector3 endPos = targetRect.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                flyingCard.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            Destroy(flyingCard);
            cardSlots[slotIndex].gameObject.SetActive(true); 

            yield return new WaitForSeconds(0.3f);
        }
        yield return new WaitForSeconds(0.5f);
        
        if (panelDek != null && panelDek.activeSelf)
        {
             if (panelDekCanvasGroup != null)
                yield return StartCoroutine(FadePanel(panelDek, panelDekCanvasGroup, false, 0.5f));
            else
                panelDek.SetActive(false);
        }

        onComplete?.Invoke();
    }

    public IEnumerator FadePanel(GameObject panel, CanvasGroup cg, bool show, float duration)
    {
        if (show)
        {
            panel.SetActive(true);
            cg.alpha = 0f; 
            yield return StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, duration));
        }
        else
        {
            cg.alpha = 1f; 
            yield return StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, duration));
            panel.SetActive(false); 
        }
    }

    public void OnSliderChanged(float value)
    {
        if (TransisiScene.Instance != null)
        {
            TransisiScene.Instance.SetMasterVolume(value);
            
            bool isMuted = (value <= 0);
            UpdateVolumeButtonSprite(isMuted);
        }
    }

    void OnVolumeButtonPressed()
    {
        if (TransisiScene.Instance != null)
        {
            TransisiScene.Instance.ToggleMute();

            bool isMuted = TransisiScene.Instance.IsMuted();
            UpdateVolumeButtonSprite(isMuted);

            sliderVolume.value = TransisiScene.Instance.GetMasterVolume();
        }
    }

    void UpdateVolumeButtonSprite(bool isMuted)
    {
        if (volumeButton != null)
        {
            volumeButton.image.sprite = isMuted ? volumeOffSprite : volumeOnSprite;
        }
    }

    public void SetupConfirmTangkisAction(UnityAction action)
    {
        if (confirmTangkisButton != null)
        {
            confirmTangkisButton.onClick.RemoveAllListeners();
            confirmTangkisButton.onClick.AddListener(action);
            confirmTangkisButton.gameObject.SetActive(true);
        }
    }

    public void ShowSelectKartuAction(ActionType actionType)
    {
        HideAllPlayerPanels();
        string message = "";
        Transform targetPosition = defaultPanelPosition;

        switch (actionType)
        {
            case ActionType.Melangkah:
                message = "Pilih kartu untuk menentukan jarak langkah!";
                targetPosition = rightPanelPosition;
                break;
            case ActionType.Serang:
                message = "Pilih kartu yang jaraknya sesuai dengan lawan.";
                targetPosition = bottomPanelPosition;
                break;
            case ActionType.Perkuat:
                message = "Pilih kartu tambahan untuk memperkuat serangan.";
                targetPosition = rightPanelPosition;
                break;
            case ActionType.Sergap:
                message = "Pilih kartu yang jaraknya sesuai dengan lawan. untuk melakukan Sergap.";
                targetPosition = bottomPanelPosition;
                break;
            case ActionType.SerangBalik:
                HideAttackStrength();
                message = "Pilih kartu untuk melakukan Serang Balik!!";
                targetPosition = bottomPanelPosition;
                break;
        }

        ShowMessage(message, 0f);
        MoveKartuPanel(targetPosition);
        panelKartuManusia.SetActive(true);
        SetPlayerHandInteractable(true);
    }

    public void UpdatePlayerHandUI(Player player)
    {
        foreach (var slot in cardSlots) slot.HideSlot();
        for (int i = 0; i < player.hand.Count; i++)
        {
            if (i < cardSlots.Length) cardSlots[i].Initialize(player.hand[i]);
        }
    }

    public void SetPlayerHandInteractable(bool isInteractable, bool shouldLookDisabled = false)
    {
        foreach (var slot in cardSlots)
        {
            Button button = slot.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = !shouldLookDisabled; 
            }
            if (slot != null) 
            {
                slot.SetInteractable(isInteractable);
            }
        }
    }

    public void ResetScoreUI()
    {
        foreach (Image img in scoreManusiaImages) img.gameObject.SetActive(false);
        foreach (Image img in scoreAiImages) img.gameObject.SetActive(false);
    }

    public void UpdateScoreUI(Player player)
    {
        Image[] targetImages = player.isAI ? scoreAiImages : scoreManusiaImages;
        if (player.score > 0 && player.score <= targetImages.Length)
            targetImages[player.score - 1].gameObject.SetActive(true);
    }

    public void UpdateMainDekUI(int cardCount)
    {
        if (remainingDekText == null) return;
        remainingDekText.text = cardCount.ToString();
    }

    public void ShowMessage(string message, float duration = 2f)
    {
        if (systemText == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    public void ShowTangkisFailedMessage(float duration = 3f)
    {
        ShowMessage("Tangkisan GAGAL! Kartu tidak sesuai", duration);
    }

    public void HideMessage()
    {
        if (systemText == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        systemText.gameObject.SetActive(false);
    }

    public void ShowAttackValue(int strength)
    {
        if (attackValueText == null) return;
        attackValueText.gameObject.SetActive(true);
        attackValueText.text = $"KEKUATAN SERANGAN: {strength}";
    }

    public void UpdateTotalTangkisValue(int total, int requiredStrength)
    {
        if (attackValueText == null) return;
        attackValueText.gameObject.SetActive(true);
        attackValueText.text = $"TANGKISAN ANDA: {total} / {requiredStrength}";
    }

    public void HideAttackStrength()
    {
        if (attackValueText != null) attackValueText.gameObject.SetActive(false);
    }

    public void MoveKartuPanel(Transform targetPosition)
    {
        if (panelKartuManusia != null && targetPosition != null)
        {
            panelKartuManusia.transform.position = targetPosition.position;
        }
    }

    public void RestoreDefaultLayout()
    {
        HideAllPlayerPanels();
        HideAttackStrength();
        panelKartuManusia.SetActive(false);
    }

    public void ShowPausePanel()
    {
        if (panelPause != null)
        {
            panelPause.SetActive(true);
            if (TransisiScene.Instance != null) UpdateVolumeButtonSprite(TransisiScene.Instance.IsMuted());
        }
    }

    public void HidePausePanel()
    {
        if (panelPause != null) panelPause.SetActive(false);
    }

    public void HideConfirmTangkisButton()
    {
        if (confirmTangkisButton != null) confirmTangkisButton.gameObject.SetActive(false);
    }

    public void HideKartuPanel()
    {
        if (panelKartuManusia != null) panelKartuManusia.SetActive(false);
    }

    public void HideAllUIsForRondeEnd()
    {
        HideAllPlayerPanels();
        HideAttackStrength();
        HideMessage();
        if (panelDek != null) panelDek.SetActive(false); 
    }

    public void HideAllPlayerPanels()
    {
        panelOpsiAwal.SetActive(false);
        panelAksiMelangkah.SetActive(false);
        panelAksiTangkis.SetActive(false);
        panelAksiSergap.SetActive(false);
        panelPerkuatSerangan.SetActive(false);
        if (panelInfo != null) panelInfo.SetActive(false);
        confirmTangkisButton.gameObject.SetActive(false);
        panelKartuManusia.SetActive(false);
        if (panelDek != null) panelDek.SetActive(false);
    }

    public void ShowOpsiAwal(bool canMelangkah, bool canSerang)
    {
        RestoreDefaultLayout();

        ShowMessage("Silahkan pilih aksi melangkah atau serang", 0f);
        if (melangkahArrow != null) 
        {
            melangkahArrow.SetActive(canMelangkah && !canSerang);
        }

        panelOpsiAwal.SetActive(true);
        
        if (panelDek != null) panelDek.SetActive(true);

        if (panelInfo != null) panelInfo.SetActive(true);
        MoveKartuPanel(defaultPanelPosition);
        panelKartuManusia.SetActive(true);
        
        btnMelangkah.interactable = canMelangkah;
        btnSerang.interactable = canSerang;

        SetPlayerHandInteractable(false, true);
    }

    public void ShowPerkuatSerangan()
    {
        panelPerkuatSerangan.SetActive(true);
        ShowMessage("Serangan berhasil! Ingin perkuat serangan??", 0f);
    }

    public void ShowSelectKartuTangkis(int attackStrength)
    {
        panelAksiTangkis.SetActive(false);
        ShowMessage($"Pilih satu kartu atau kombinasi dengan total nilai {attackStrength}, lalu Tekan Tangkis!", 0f);
        MoveKartuPanel(rightPanelPosition);
        panelKartuManusia.SetActive(true);
        SetPlayerHandInteractable(true);
        UpdateTotalTangkisValue(0, attackStrength);
    }

    public IEnumerator ShowRondePanel(int roundNumber, float totalDuration)
    {
        HideAllUIsForRondeEnd();

        float fadeDuration = 0.5f;
        float holdDuration = totalDuration - (fadeDuration * 2);

        if (panelRonde != null && rondeImage != null && roundNumberSprites.Length > 0)
        {
            if (roundNumber >= 1 && roundNumber <= roundNumberSprites.Length)
            {
                rondeImage.sprite = roundNumberSprites[roundNumber - 1];
                rondeImage.gameObject.SetActive(true);
                panelRonde.SetActive(true);
                roundPanelCanvasGroup.alpha = 0f; 

                yield return StartCoroutine(FadeCanvasGroup(roundPanelCanvasGroup, 0f, 1f, fadeDuration));

                if (holdDuration > 0) yield return new WaitForSeconds(1.2f);

                yield return StartCoroutine(FadeCanvasGroup(roundPanelCanvasGroup, 1f, 0f, fadeDuration));

                panelRonde.SetActive(false);
            }
        }
        else
        {
            yield return new WaitForSeconds(totalDuration);
        }
    }

    public IEnumerator ShowGameWin(string winnerName, float totalDuration)
    {
        float fadeDuration = 0.5f;
        float holdDuration = totalDuration - (fadeDuration * 2);

        if (panelRonde != null && rondeImage != null)
        {
            if (winnerName == "Manusia") rondeImage.sprite = playerWinSprite;
            else rondeImage.sprite = aiWinSprite;

            rondeImage.gameObject.SetActive(true);
            panelRonde.SetActive(true);
            roundPanelCanvasGroup.alpha = 0f; 

            yield return StartCoroutine(FadeCanvasGroup(roundPanelCanvasGroup, 0f, 1f, fadeDuration));

            if (holdDuration > 0) yield return new WaitForSeconds(0.8f);

            yield return StartCoroutine(FadeCanvasGroup(roundPanelCanvasGroup, 1f, 0f, fadeDuration));

            panelRonde.SetActive(false);
        }
    }

    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        systemText.text = message;
        systemText.gameObject.SetActive(true);
        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);
            systemText.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }
        cg.alpha = endAlpha; 
    }

    public IEnumerator ShowArahLangkah(bool canMaju, bool canMundur)
    {
        HideAllPlayerPanels();
        yield return new WaitForSeconds(1.5f);
        ShowMessage("Pilih arah untuk melangkah!!", 0f);
        panelAksiMelangkah.SetActive(true);
        btnMaju.interactable = canMaju;
        btnMundur.interactable = canMundur;
    }

    public IEnumerator ShowSergap()
    {
        yield return new WaitForSeconds(2.0f);
        ShowMessage("Anda berada di jangkauan sergap, lakukan sergap??", 0f);
        panelAksiSergap.SetActive(true);
    }

    public IEnumerator ShowTangkis(int strength, bool isSerangBalik)
    {
        HideAllPlayerPanels();
        yield return new WaitForSeconds(1.5f);
        HideAttackStrength();
        string message = isSerangBalik ? "AI melakukan serang balik. Tangkis Serangan Balik??" : "Anda diserang! Tangkis serangan ini?";
        ShowMessage(message, 0f);
        panelAksiTangkis.SetActive(true);
    }

    public void PlayCutscene(VideoClip clip, System.Action onComplete)
    {
        StopAllCoroutines(); 
        StartCoroutine(PlayCutsceneCoroutine(clip, onComplete));
    }

    private IEnumerator PlayCutsceneCoroutine(VideoClip clip, System.Action onComplete)
    {
        if (clip == null || videoPlayer == null) 
        {
            onComplete?.Invoke();
            yield break;
        }

        if (GameManager.instance != null) GameManager.instance.uiManager.SetPlayerHandInteractable(false);
        
        if (TransisiScene.Instance != null) TransisiScene.Instance.MuteBGMForCutscene(0.5f);

        cutscenePanel.SetActive(true);
        CanvasGroup cg = cutscenePanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = cutscenePanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        videoPlayer.clip = clip;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        float timer = 0f;
        float fadeDuration = 0.5f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;

        videoPlayer.Play();

        bool isVideoFinished = false;
        void OnVideoFinished(VideoPlayer vp) { isVideoFinished = true; }
        videoPlayer.loopPointReached += OnVideoFinished;

        while (!isVideoFinished)
        {
            yield return null;
        }

        videoPlayer.loopPointReached -= OnVideoFinished;

        yield return new WaitForSeconds(0.5f); 

        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        cg.alpha = 0f;

        videoPlayer.Stop();
        cutscenePanel.SetActive(false);

        if (TransisiScene.Instance != null) TransisiScene.Instance.ResumeBGMAfterCutscene(0.5f);
        
        onComplete?.Invoke();
    }

    public void SetConfirmTangkisInteractable(bool isInteractable)
    {
        if (confirmTangkisButton != null)
        {
            confirmTangkisButton.interactable = isInteractable;
        }
    }
}