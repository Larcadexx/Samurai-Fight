using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Setup")]
    public Image[] scoreManusiaImages;
    public Image[] scoreAiImages;
    public SistemKartu[] cardSlots;

    [Header("Posisi Panel Kartu")]
    public Transform posisiPanelDefault;
    public Transform posisiPanelKanan;
    public Transform posisiPanelBawah;

    [Header("Text System")]
    public TextMeshProUGUI nilaiSerangText;
    public TextMeshProUGUI systemText;
    public TextMeshProUGUI sisaKartuDeckText;

    [Header("Tampilan Ronde & Kemenangan (Sprite)")]
    public Image roundImageDisplay; 
    public Sprite[] roundNumberSprites;
    public Sprite pemainMenangSprite; 
    public Sprite aiMenangSprite; 

    [Header("Panels")]
    public GameObject KartuManusiaPanel;
    public GameObject InfoPanel;
    public GameObject OpsiAwalPanel;
    public GameObject AksiMelangkahPanel;
    public GameObject AksiTangkisPanel;
    public GameObject AksiSergapPanel;
    public GameObject PerkuatSeranganPanel;
    public GameObject RoundPanel;
    public GameObject PausePanel; 

    [Header("Action Buttons")]
    public Button MelangkahButton;
    public Button SerangButton;
    public Button MajuButton;
    public Button MundurButton;
    public Button KonfirmasiTangkisButton;
    private Coroutine messageCoroutine;
    private CanvasGroup roundPanelCanvasGroup;

    [Header("Pause Panel Elements")]
    public Button volumeButton;
    public Sprite volumeOnSprite;
    public Sprite volumeOffSprite;

    void Awake()
    {
    instance = this;
    if (nilaiSerangText != null) nilaiSerangText.gameObject.SetActive(false);
    if (systemText != null) systemText.gameObject.SetActive(false);
    if (InfoPanel != null) InfoPanel.SetActive(false);
    if (KonfirmasiTangkisButton != null) KonfirmasiTangkisButton.gameObject.SetActive(false);

    if (RoundPanel != null)
    {
        roundPanelCanvasGroup = RoundPanel.GetComponent<CanvasGroup>();
        if (roundPanelCanvasGroup == null)
        {
            Debug.LogError("RoundPanel tidak memiliki komponen CanvasGroup! Harap tambahkan di Inspector.");
        }
        RoundPanel.SetActive(false);
    }

    if (PausePanel != null) PausePanel.SetActive(false); 
    if (volumeButton != null)
    {
        volumeButton.onClick.AddListener(OnVolumeButtonPressed);
    }
    }

    public void UpdatePlayerHandUI(Player player)
    {
        foreach (var slot in cardSlots) slot.HideSlot();
        for (int i = 0; i < player.hand.Count; i++)
        {
            if (i < cardSlots.Length) cardSlots[i].Initialize(player.hand[i]);
        }
    }

    public void SetPlayerHandInteractable(bool isInteractable)
    {
        foreach (var slot in cardSlots)
        {
            Button button = slot.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = slot.gameObject.activeSelf && isInteractable;
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

    public void UpdateMainDeckUI(int cardCount)
    {
        if (sisaKartuDeckText == null) return;
        sisaKartuDeckText.text = cardCount.ToString();
    }

    public void ShowMessage(string message, float duration = 2f)
    {
        if (systemText == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    public void ShowTangkisanGagalMessage(float duration = 3f)
    {
        string message = "Tangkisan GAGAL!! Nilai tidak sesuai";
        ShowMessage(message, duration);
    }

    public void HideMessage()
    {
        if (systemText == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        systemText.gameObject.SetActive(false);
    }

    public void ShowInfo_AITurn()
    {
        if (InfoPanel != null) InfoPanel.SetActive(true);
        ShowMessage("Giliran AI...", 0f);
    }

    public void ShowAttackStrength(int strength)
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(true);
        nilaiSerangText.text = $"KEKUATAN SERANGAN: {strength}";
    }

    public void UpdateSelectedTangkisTotal(int total, int requiredStrength)
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(true);
        nilaiSerangText.text = $"TANGKISAN ANDA: {total} / {requiredStrength}";
    }

    public void HideAttackStrength()
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(false);
    }

    public void MoveCardPanel(Transform targetPosisi)
    {
        if (KartuManusiaPanel != null && targetPosisi != null)
        {
            KartuManusiaPanel.transform.position = targetPosisi.position;
        }
    }

    public void RestoreDefaultLayout()
    {
        HideAllPlayerPanels();
        HideAttackStrength();
        KartuManusiaPanel.SetActive(false);
    }

    public void ShowPausePanel()
{
    if (PausePanel != null)
    {
        PausePanel.SetActive(true);

        if (MusicManager.Instance != null)
        {
            UpdateVolumeButtonSprite(MusicManager.Instance.IsMuted());
        }
    }
}

    public void HidePausePanel()
    {
        if (PausePanel != null)
        {
            PausePanel.SetActive(false);
        }
    }

    public void HideConfirmTangkisbtn()
    {
        if (KonfirmasiTangkisButton != null) KonfirmasiTangkisButton.gameObject.SetActive(false);
    }

    public void HideCardPanel()
    {
        if (KartuManusiaPanel != null) KartuManusiaPanel.SetActive(false);
    }

    public void HideAllUIsForRoundEnd()
    {
        HideAllPlayerPanels();
        HideAttackStrength();
        HideMessage();
    }

    public void HideAllPlayerPanels()
    {
        OpsiAwalPanel.SetActive(false);
        AksiMelangkahPanel.SetActive(false);
        AksiTangkisPanel.SetActive(false);
        AksiSergapPanel.SetActive(false);
        PerkuatSeranganPanel.SetActive(false);
        if (InfoPanel != null) InfoPanel.SetActive(false);
        KonfirmasiTangkisButton.gameObject.SetActive(false);
        KartuManusiaPanel.SetActive(false);
    }

    public void ShowOpsiAwal(bool bisaMelangkah, bool bisaMenyerang)
    {
        RestoreDefaultLayout();
        OpsiAwalPanel.SetActive(true);

        if (InfoPanel != null) InfoPanel.SetActive(true);

        MoveCardPanel(posisiPanelDefault);
        KartuManusiaPanel.SetActive(true);
        MelangkahButton.interactable = bisaMelangkah;
        SerangButton.interactable = bisaMenyerang;
        SetPlayerHandInteractable(false);
    }

    public void ShowPilihKartuAksi(string jenisAksi, string detailAksi = "")
    {
        HideAllPlayerPanels();
        string message = "";
        Transform targetPosisi = posisiPanelDefault;

        switch (jenisAksi)
        {
            case "melangkah":
                message = "Pilih satu kartu, untuk melangkah seberapa jauh!!";
                targetPosisi = posisiPanelKanan;
                break;
            case "serang":
                message = "Pilih kartu yang nilainya sama dengan jarak Anda ke lawan.";
                targetPosisi = posisiPanelBawah;
                break;
            case "perkuat":
                message = "Pilih satu kartu tambahan untuk memperkuat serangan.";
                targetPosisi = posisiPanelKanan;
                break;
            case "sergap":
                message = "Pilih kartu untuk melakukan Sergap.";
                targetPosisi = posisiPanelBawah;
                break;
            case "serangbalik":
                HideAttackStrength();
                message = "Pilih satu kartu untuk melakukan Serang Balik!!";
                targetPosisi = posisiPanelBawah;
                break;
        }

        ShowMessage(message, 0f);
        MoveCardPanel(targetPosisi);
        KartuManusiaPanel.SetActive(true);
        SetPlayerHandInteractable(true);
    }

    public void ShowPerkuatSerangan()
    {
        PerkuatSeranganPanel.SetActive(true);
        ShowMessage("Serangan berhasil!! ingin perkuat serangan??", 0f);
    }

    public void ShowPilihKartuTangkis(int kekuatanSerangan)
    {
        AksiTangkisPanel.SetActive(false);
        KonfirmasiTangkisButton.gameObject.SetActive(true);
        ShowMessage($"Pilih Kartu Dengan Nilai {kekuatanSerangan}, Lalu Tekan Tangkis!!", 0f);
        MoveCardPanel(posisiPanelKanan);
        KartuManusiaPanel.SetActive(true);
        SetPlayerHandInteractable(true);
        UpdateSelectedTangkisTotal(0, kekuatanSerangan);
    }

    public IEnumerator ShowRoundStartPanel(int roundNumber, float totalDuration)
    {
        HideAllUIsForRoundEnd();

        float fadeDuration = 0.5f;
        float holdDuration = totalDuration - (fadeDuration * 2);

        if (RoundPanel != null && roundImageDisplay != null && roundPanelCanvasGroup != null && roundNumberSprites.Length > 0)
        {
            if (roundNumber >= 1 && roundNumber <= roundNumberSprites.Length)
            {
                roundImageDisplay.sprite = roundNumberSprites[roundNumber - 1];
                roundImageDisplay.gameObject.SetActive(true);
                RoundPanel.SetActive(true);
                roundPanelCanvasGroup.alpha = 0f; 

                yield return StartCoroutine(FadeCanvasGroup(roundPanelCanvasGroup, 0f, 1f, fadeDuration));

                if (holdDuration > 0)
                {
                    yield return new WaitForSeconds( 1.2f);
                }

                yield return StartCoroutine(FadeCanvasGroup(roundPanelCanvasGroup, 1f, 0f, fadeDuration));

                RoundPanel.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"Tidak ada sprite untuk Ronde {roundNumber}.");
                RoundPanel.SetActive(false);
            }
        }
        else
        {
            yield return new WaitForSeconds(totalDuration);
        }
    }

    public IEnumerator ShowGameWinRoundText(string winnerName, float totalDuration)
    {
        float fadeDuration = 0.5f;
        float holdDuration = totalDuration - (fadeDuration * 2);

        if (RoundPanel != null && roundImageDisplay != null && roundPanelCanvasGroup != null)
        {
            if (winnerName == "Pemain" && pemainMenangSprite != null)
            {
                roundImageDisplay.sprite = pemainMenangSprite;
            }
            else if (winnerName == "AI" && aiMenangSprite != null)
            {
                roundImageDisplay.sprite = aiMenangSprite;
            }
            else
            {
                Debug.LogWarning("Sprite kemenangan belum di-assign di UIManager Inspector.");
                yield break;
            }

            roundImageDisplay.gameObject.SetActive(true);
            RoundPanel.SetActive(true);
            roundPanelCanvasGroup.alpha = 0f; 

            yield return StartCoroutine(FadeCanvasGroup(roundPanelCanvasGroup, 0f, 1f, fadeDuration));

            if (holdDuration > 0)
            {
                yield return new WaitForSeconds(0.8f);
            }

            yield return StartCoroutine(FadeCanvasGroup(roundPanelCanvasGroup, 1f, 0f, fadeDuration));

            RoundPanel.SetActive(false);
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

    public IEnumerator ShowArahLangkah(bool bisaMaju, bool bisaMundur)
    {
        HideAllPlayerPanels();
        yield return new WaitForSeconds(1.5f);
        ShowMessage("Pilih arah untuk melangkah!!", 0f);
        AksiMelangkahPanel.SetActive(true);
        MajuButton.interactable = bisaMaju;
        MundurButton.interactable = bisaMundur;
    }

    public IEnumerator ShowSergap()
    {
        yield return new WaitForSeconds(2.0f);
        ShowMessage("Anda berada di jangkauan sergap, Lakukan Sergap??", 0f);
        AksiSergapPanel.SetActive(true);
    }

    public IEnumerator ShowTangkis(int kekuatan, bool isSerangBalik)
    {
        HideAllPlayerPanels();
        yield return new WaitForSeconds(1.5f);
        HideAttackStrength();
        string message = isSerangBalik ? "AI melakukan serang balik. Tangkis Serangan Balik??" : $"Anda diserang! Tangkis serangan ini?";
        ShowMessage(message, 0f);
        AksiTangkisPanel.SetActive(true);
    }
    void OnVolumeButtonPressed()
    {
        if (MusicManager.Instance == null) return;

        // Panggil fungsi di MusicManager
        bool isNowMuted = MusicManager.Instance.ToggleMute();

        // Update sprite tombol
        UpdateVolumeButtonSprite(isNowMuted);
    }

    void UpdateVolumeButtonSprite(bool isMuted)
    {
        if (volumeButton == null) return;

        if (isMuted)
        {
            volumeButton.image.sprite = volumeOffSprite;
        }
        else
        {
            volumeButton.image.sprite = volumeOnSprite;
        }
    }
}